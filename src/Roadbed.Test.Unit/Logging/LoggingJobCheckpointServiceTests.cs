namespace Roadbed.Test.Unit.Logging;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Roadbed.Data;
using Roadbed.Data.Sqlite;
using Roadbed.Logging;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="LoggingJobCheckpointService"/> class.
/// </summary>
/// <remarks>
/// <para>
/// Most tests run the real service and repository against an in-memory SQLite
/// database built from the shipped <c>job_checkpoint/install_sqlite.txt</c>, so
/// the compare-and-set, the run-start upsert and the key isolation are proven
/// against a real engine. The tests that need a database failure or a lost
/// acknowledgement use a strict Moq repository instead.
/// </para>
/// <para>
/// MariaDB-only behavior (the <c>ON DUPLICATE KEY UPDATE</c> form and the
/// left-to-right <c>SET</c> order) is covered by SQL-shape tests in
/// <c>LoggingJobCheckpointRepositoryTests</c>.
/// </para>
/// </remarks>
[TestClass]
public sealed class LoggingJobCheckpointServiceTests
{
    #region Private Fields

    private const string CheckpointKey = "criteria-rematch";

    private static readonly DateTimeOffset StartInstant =
        new DateTimeOffset(2026, 10, 4, 12, 0, 0, TimeSpan.Zero);

    private SqliteConnection? _keepAlive;
    private SqliteCheckpointDatabaseFactory? _factory;
    private FakeTimeProvider? _timeProvider;

    #endregion Private Fields

    #region Public Properties

    /// <summary>
    /// Gets or sets the MSTest context for the running test.
    /// </summary>
    public TestContext? TestContext { get; set; }

    #endregion Public Properties

    #region Private Properties

    /// <summary>
    /// Gets the cancellation token of the running test.
    /// </summary>
    private CancellationToken TestCancellationToken => this.TestContext?.CancellationToken ?? CancellationToken.None;

    #endregion Private Properties

    #region Public Methods

    /// <summary>
    /// Creates a private in-memory SQLite database holding the shipped
    /// <c>job_checkpoint</c> table, kept alive for the duration of one test.
    /// </summary>
    [TestInitialize]
    public void TestInitialize()
    {
        DapperMapping.Configure(typeof(LoggingJobCheckpoint));

        var connectionString = new DataConnecionString(DataConnectionStringType.SQLiteInMemory)
        {
            DatabaseSource = $"JobCheckpoint_{Guid.NewGuid():N}",
        };

        this._factory = new SqliteCheckpointDatabaseFactory(connectionString);
        this._keepAlive = (SqliteConnection)this._factory.CreateOpenConnection();
        this._timeProvider = new FakeTimeProvider(StartInstant);

        string ddl = File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory,
            "Assets",
            "Tables",
            "job_checkpoint",
            "install_sqlite.txt"));

        using SqliteCommand command = this._keepAlive.CreateCommand();
        command.CommandText = ddl;
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Closes the keep-alive connection, which destroys the in-memory database.
    /// </summary>
    [TestCleanup]
    public void TestCleanup()
    {
        this._keepAlive?.Dispose();
    }

    /// <summary>
    /// Unit test to verify that reading a checkpoint that has never been written
    /// returns null, the normal "never run" answer, rather than throwing.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task ReadAsync_NeverRun_ReturnsNull()
    {
        // Arrange (Given)
        LoggingJobCheckpointService service = this.CreateSqliteService();

        // Act (When)
        LoggingJobCheckpoint? checkpoint = await service.ReadAsync(
            CreateJob(),
            CheckpointKey,
            this.TestCancellationToken);

        // Assert (Then)
        Assert.IsNull(
            checkpoint,
            "A checkpoint that was never written must read as null (never run).");
    }

    /// <summary>
    /// Unit test to verify that a read has no side effects: it never creates a
    /// row, so reading alone can never look like progress.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task ReadAsync_NeverRun_DoesNotCreateRow()
    {
        // Arrange (Given)
        LoggingJobCheckpointService service = this.CreateSqliteService();

        // Act (When)
        await service.ReadAsync(CreateJob(), CheckpointKey, this.TestCancellationToken);

        // Assert (Then)
        Assert.AreEqual(
            0L,
            this.CountRows(),
            "Reading a checkpoint must not insert a row.");
    }

    /// <summary>
    /// Unit test to verify that reading an existing checkpoint leaves it exactly
    /// as it was, so a run that reads and then fails before advancing changes
    /// nothing.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task ReadAsync_ExistingCheckpoint_LeavesRowUnchanged()
    {
        // Arrange (Given)
        LoggingJobCheckpointService service = this.CreateSqliteService();
        ILoggingCheckpointedJob job = CreateJob();
        await service.TryAdvanceAsync(job, CheckpointKey, 0, new LoggingJobCheckpointPosition { Id = 10 }, "fire-1", this.TestCancellationToken);
        LoggingJobCheckpoint? before = await service.ReadAsync(job, CheckpointKey, this.TestCancellationToken);
        this._timeProvider!.Advance(TimeSpan.FromHours(1));

        // Act (When)
        LoggingJobCheckpoint? after = await service.ReadAsync(job, CheckpointKey, this.TestCancellationToken);

        // Assert (Then)
        Assert.IsNotNull(
            before,
            "The arranged advance must have created the checkpoint.");
        Assert.IsNotNull(
            after,
            "The checkpoint must still exist after a read.");
        Assert.AreEqual(
            before.Version,
            after.Version,
            "A read must not change the version.");
        Assert.AreEqual(
            before.LastModifiedOn,
            after.LastModifiedOn,
            "A read must not touch last_modified_on.");
        Assert.AreEqual(
            before.LastRunStartedOn,
            after.LastRunStartedOn,
            "A read must not record a run start.");
    }

    /// <summary>
    /// Unit test to verify that a database failure during a read propagates
    /// instead of being reported as "never run", which would trigger a baseline
    /// or a full backfill.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task ReadAsync_RepositoryThrows_PropagatesException()
    {
        // Arrange (Given)
        var repository = new Mock<ILoggingJobCheckpointRepository>(MockBehavior.Strict);
        repository
            .Setup(r => r.ReadAsync(It.IsAny<LoggingJobCheckpointKey>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("simulated database failure"));
        LoggingJobCheckpointService service = this.CreateService(repository.Object, CreateOptions());
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            await service.ReadAsync(CreateJob(), CheckpointKey, this.TestCancellationToken);
        }
        catch (TimeoutException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "A failed read must throw, never return null.");
    }

    /// <summary>
    /// Unit test to verify that a blank environment is rejected, so a
    /// misconfigured dev host and prod host can never share a checkpoint.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task ReadAsync_BlankEnvironment_ThrowsInvalidOperationException()
    {
        // Arrange (Given)
        var options = new LoggingOptions { Application = "Pebble.Window.Service", Environment = "  " };
        LoggingJobCheckpointService service = this.CreateService(
            new Mock<ILoggingJobCheckpointRepository>(MockBehavior.Strict).Object,
            options);
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            await service.ReadAsync(CreateJob(), CheckpointKey, this.TestCancellationToken);
        }
        catch (InvalidOperationException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "A blank LoggingOptions.Environment must be rejected before any database call.");
    }

    /// <summary>
    /// Unit test to verify that an unset environment (the LoggingOptions
    /// default) is rejected.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task ReadAsync_NullEnvironment_ThrowsInvalidOperationException()
    {
        // Arrange (Given)
        var options = new LoggingOptions { Application = "Pebble.Window.Service" };
        LoggingJobCheckpointService service = this.CreateService(
            new Mock<ILoggingJobCheckpointRepository>(MockBehavior.Strict).Object,
            options);
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            await service.ReadAsync(CreateJob(), CheckpointKey, this.TestCancellationToken);
        }
        catch (InvalidOperationException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "An unset LoggingOptions.Environment must be rejected before any database call.");
    }

    /// <summary>
    /// Unit test to verify that a blank application is rejected, because the
    /// application is part of the checkpoint's key.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task ReadAsync_BlankApplication_ThrowsInvalidOperationException()
    {
        // Arrange (Given)
        var options = new LoggingOptions { Application = string.Empty, Environment = "Production" };
        LoggingJobCheckpointService service = this.CreateService(
            new Mock<ILoggingJobCheckpointRepository>(MockBehavior.Strict).Object,
            options);
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            await service.ReadAsync(CreateJob(), CheckpointKey, this.TestCancellationToken);
        }
        catch (InvalidOperationException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "A blank LoggingOptions.Application must be rejected before any database call.");
    }

    /// <summary>
    /// Unit test to verify that the same job in two environments keeps two
    /// independent checkpoints, as intended even when environments share one
    /// database.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task ReadAsync_SameJobOtherEnvironment_DoesNotSeeCheckpoint()
    {
        // Arrange (Given)
        LoggingJobCheckpointService development = this.CreateSqliteService(
            new LoggingOptions { Application = "Pebble.Window.Service", Environment = "Development" });
        LoggingJobCheckpointService production = this.CreateSqliteService(
            new LoggingOptions { Application = "Pebble.Window.Service", Environment = "Production" });
        await development.TryAdvanceAsync(CreateJob(), CheckpointKey, 0, new LoggingJobCheckpointPosition { Id = 10 }, "fire-1", this.TestCancellationToken);

        // Act (When)
        LoggingJobCheckpoint? seenByProduction = await production.ReadAsync(CreateJob(), CheckpointKey, this.TestCancellationToken);

        // Assert (Then)
        Assert.IsNull(
            seenByProduction,
            "A checkpoint written in Development must not be visible to Production.");
    }

    /// <summary>
    /// Unit test to verify that two applications with a job of the same group
    /// and name keep independent checkpoints, because the application is part
    /// of the key.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task ReadAsync_SameJobOtherApplication_DoesNotSeeCheckpoint()
    {
        // Arrange (Given)
        LoggingJobCheckpointService first = this.CreateSqliteService(
            new LoggingOptions { Application = "Pebble.Window.Service", Environment = "Production" });
        LoggingJobCheckpointService second = this.CreateSqliteService(
            new LoggingOptions { Application = "Pebble.Loader.Place", Environment = "Production" });
        await first.TryAdvanceAsync(CreateJob(), CheckpointKey, 0, new LoggingJobCheckpointPosition { Id = 10 }, "fire-1", this.TestCancellationToken);

        // Act (When)
        LoggingJobCheckpoint? seenBySecond = await second.ReadAsync(CreateJob(), CheckpointKey, this.TestCancellationToken);

        // Assert (Then)
        Assert.IsNull(
            seenBySecond,
            "A checkpoint written by one application must not be visible to another.");
    }

    /// <summary>
    /// Unit test to verify that one job's checkpoint keys are independent of
    /// each other.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task ReadAsync_OtherCheckpointKey_DoesNotSeeCheckpoint()
    {
        // Arrange (Given)
        LoggingJobCheckpointService service = this.CreateSqliteService();
        await service.TryAdvanceAsync(CreateJob(), "board-1", 0, new LoggingJobCheckpointPosition { Id = 10 }, "fire-1", this.TestCancellationToken);

        // Act (When)
        LoggingJobCheckpoint? other = await service.ReadAsync(CreateJob(), "board-2", this.TestCancellationToken);

        // Assert (Then)
        Assert.IsNull(
            other,
            "Advancing board-1 must not create or move board-2.");
    }

    /// <summary>
    /// Unit test to verify that a checkpoint key with trailing whitespace is
    /// rejected, because MariaDB's PAD SPACE collation would treat it as equal to
    /// the trimmed key.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task ReadAsync_CheckpointKeyWithTrailingSpace_ThrowsArgumentException()
    {
        // Arrange (Given)
        LoggingJobCheckpointService service = this.CreateSqliteService();
        ArgumentException? caughtException = null;

        // Act (When)
        try
        {
            await service.ReadAsync(CreateJob(), "board-1 ", this.TestCancellationToken);
        }
        catch (ArgumentException ex)
        {
            caughtException = ex;
        }

        // Assert (Then)
        Assert.IsNotNull(
            caughtException,
            "A checkpoint key with trailing whitespace must be rejected.");
        Assert.AreEqual(
            "checkpointKey",
            caughtException.ParamName,
            "The exception must name the checkpointKey parameter.");
    }

    /// <summary>
    /// Unit test to verify that a checkpoint key longer than the column is
    /// rejected before the database, which on SQLite would otherwise store it.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task ReadAsync_CheckpointKeyTooLong_ThrowsArgumentException()
    {
        // Arrange (Given)
        LoggingJobCheckpointService service = this.CreateSqliteService();
        string tooLong = new string('k', 101);
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            await service.ReadAsync(CreateJob(), tooLong, this.TestCancellationToken);
        }
        catch (ArgumentException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "A checkpoint key longer than 100 characters must be rejected.");
    }

    /// <summary>
    /// Unit test to verify that a job with a blank group is rejected, because
    /// the group is half of the Quartz JobKey the checkpoint is stored under.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task ReadAsync_BlankJobGroup_ThrowsArgumentException()
    {
        // Arrange (Given)
        LoggingJobCheckpointService service = this.CreateSqliteService();
        var job = new TestCheckpointedJob { Name = "HelpWantedMatch", JobGroup = string.Empty };
        ArgumentException? caughtException = null;

        // Act (When)
        try
        {
            await service.ReadAsync(job, CheckpointKey, this.TestCancellationToken);
        }
        catch (ArgumentException ex)
        {
            caughtException = ex;
        }

        // Assert (Then)
        Assert.IsNotNull(
            caughtException,
            "A job with a blank JobGroup must be rejected.");
        Assert.AreEqual(
            "job",
            caughtException.ParamName,
            "The exception must name the job parameter.");
    }

    /// <summary>
    /// Unit test to verify that the first advance of a never-run checkpoint wins
    /// and stores the position, the next version, the advancing run and the
    /// advance time.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task TryAdvanceAsync_NeverRun_WinsAndStoresPosition()
    {
        // Arrange (Given)
        LoggingJobCheckpointService service = this.CreateSqliteService();
        ILoggingCheckpointedJob job = CreateJob();

        // Act (When)
        bool won = await service.TryAdvanceAsync(job, CheckpointKey, 0, new LoggingJobCheckpointPosition { Id = 42 }, "fire-1", this.TestCancellationToken);
        LoggingJobCheckpoint? checkpoint = await service.ReadAsync(job, CheckpointKey, this.TestCancellationToken);

        // Assert (Then)
        Assert.IsTrue(
            won,
            "The first advance from version 0 must win.");
        Assert.IsNotNull(
            checkpoint,
            "The advance must create the checkpoint row.");
        Assert.AreEqual(
            42L,
            checkpoint.PositionId,
            "The stored position must be the one advanced to.");
        Assert.AreEqual(
            1L,
            checkpoint.Version,
            "The first advance must move the version from 0 to 1.");
        Assert.AreEqual(
            "fire-1",
            checkpoint.AdvancedByFireInstanceId,
            "The advancing run's fire instance id must be recorded.");
        Assert.AreEqual(
            StartInstant.UtcDateTime,
            checkpoint.AdvancedOn,
            "advanced_on must be stamped from the injected clock.");
        Assert.AreEqual(
            DateTimeKind.Utc,
            checkpoint.AdvancedOn!.Value.Kind,
            "advanced_on must be returned as UTC.");
        Assert.IsNull(
            checkpoint.PreviousPositionId,
            "The first advance has no previous position.");
    }

    /// <summary>
    /// Unit test to verify the compare-and-set: two advances from the same read
    /// can never both win, so two runs never both advance from one starting point.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task TryAdvanceAsync_TwoAdvancesFromSameRead_ExactlyOneWins()
    {
        // Arrange (Given)
        LoggingJobCheckpointService service = this.CreateSqliteService();
        ILoggingCheckpointedJob job = CreateJob();
        LoggingJobCheckpoint? read = await service.ReadAsync(job, CheckpointKey, this.TestCancellationToken);
        long expectedVersion = read?.Version ?? 0;

        // Act (When)
        bool firstWon = await service.TryAdvanceAsync(job, CheckpointKey, expectedVersion, new LoggingJobCheckpointPosition { Id = 10 }, "fire-A", this.TestCancellationToken);
        bool secondWon = await service.TryAdvanceAsync(job, CheckpointKey, expectedVersion, new LoggingJobCheckpointPosition { Id = 20 }, "fire-B", this.TestCancellationToken);
        LoggingJobCheckpoint? final = await service.ReadAsync(job, CheckpointKey, this.TestCancellationToken);

        // Assert (Then)
        Assert.IsTrue(
            firstWon,
            "The first advance from the read version must win.");
        Assert.IsFalse(
            secondWon,
            "A second advance from the same read version must lose.");
        Assert.IsNotNull(
            final,
            "The checkpoint must exist after the winning advance.");
        Assert.AreEqual(
            10L,
            final.PositionId,
            "The losing advance must not overwrite the winner's position.");
        Assert.AreEqual(
            1L,
            final.Version,
            "Only the winning advance may move the version.");
    }

    /// <summary>
    /// Unit test to verify that an advance records where it started from, so a
    /// console can show the range the last run covered.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task TryAdvanceAsync_SecondAdvance_RecordsPreviousPosition()
    {
        // Arrange (Given)
        LoggingJobCheckpointService service = this.CreateSqliteService();
        ILoggingCheckpointedJob job = CreateJob();
        DateTime firstOn = new DateTime(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);
        DateTime secondOn = new DateTime(2026, 10, 2, 8, 0, 0, DateTimeKind.Utc);
        await service.TryAdvanceAsync(job, CheckpointKey, 0, new LoggingJobCheckpointPosition { Id = 10, On = firstOn, Text = "cursor-a" }, "fire-1", this.TestCancellationToken);

        // Act (When)
        bool won = await service.TryAdvanceAsync(job, CheckpointKey, 1, new LoggingJobCheckpointPosition { Id = 20, On = secondOn, Text = "cursor-b" }, "fire-2", this.TestCancellationToken);
        LoggingJobCheckpoint? checkpoint = await service.ReadAsync(job, CheckpointKey, this.TestCancellationToken);

        // Assert (Then)
        Assert.IsTrue(
            won,
            "Advancing from the current version must win.");
        Assert.IsNotNull(
            checkpoint,
            "The checkpoint must exist.");
        Assert.AreEqual(
            10L,
            checkpoint.PreviousPositionId,
            "previous_position_id must hold the id the advance started from.");
        Assert.AreEqual(
            firstOn,
            checkpoint.PreviousPositionOn,
            "previous_position_on must hold the timestamp the advance started from.");
        Assert.AreEqual(
            "cursor-a",
            checkpoint.PreviousPositionText,
            "previous_position_text must hold the text cursor the advance started from.");
        Assert.AreEqual(
            20L,
            checkpoint.PositionId,
            "position_id must hold the new id.");
        Assert.AreEqual(
            "cursor-b",
            checkpoint.PositionText,
            "position_text must hold the new text cursor.");
        Assert.AreEqual(
            2L,
            checkpoint.Version,
            "Each winning advance must move the version by one.");
    }

    /// <summary>
    /// Unit test to verify that a timestamp cursor is stored at microsecond
    /// precision, the same on SQLite as on MariaDB's DATETIME(6).
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task TryAdvanceAsync_PositionOnWithSubMicrosecondTicks_StoresMicrosecondValue()
    {
        // Arrange (Given)
        LoggingJobCheckpointService service = this.CreateSqliteService();
        ILoggingCheckpointedJob job = CreateJob();
        DateTime microsecond = new DateTime(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc).AddTicks(1_234_560);
        DateTime withExtraTicks = microsecond.AddTicks(7);

        // Act (When)
        await service.TryAdvanceAsync(job, CheckpointKey, 0, new LoggingJobCheckpointPosition { On = withExtraTicks }, "fire-1", this.TestCancellationToken);
        LoggingJobCheckpoint? checkpoint = await service.ReadAsync(job, CheckpointKey, this.TestCancellationToken);

        // Assert (Then)
        Assert.IsNotNull(
            checkpoint,
            "The checkpoint must exist.");
        Assert.AreEqual(
            microsecond,
            checkpoint.PositionOn,
            "position_on must be truncated to microsecond precision.");
    }

    /// <summary>
    /// Unit test to verify that an advance is refused inside an ambient
    /// transaction, because it would commit on its own connection ahead of the
    /// job's work, and that nothing is written.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task TryAdvanceAsync_InsideTransactionScope_ThrowsAndWritesNothing()
    {
        // Arrange (Given)
        LoggingJobCheckpointService service = this.CreateSqliteService();
        bool exceptionThrown = false;

        // Act (When)
        using (new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            try
            {
                await service.TryAdvanceAsync(CreateJob(), CheckpointKey, 0, new LoggingJobCheckpointPosition { Id = 10 }, "fire-1", this.TestCancellationToken);
            }
            catch (InvalidOperationException)
            {
                exceptionThrown = true;
            }
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "Advancing inside a TransactionScope must throw.");
        Assert.AreEqual(
            0L,
            this.CountRows(),
            "A refused advance must not write a row.");
    }

    /// <summary>
    /// Unit test to verify that recording a run start is refused inside an
    /// ambient transaction.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task MarkRunStartedAsync_InsideTransactionScope_ThrowsInvalidOperationException()
    {
        // Arrange (Given)
        LoggingJobCheckpointService service = this.CreateSqliteService();
        bool exceptionThrown = false;

        // Act (When)
        using (new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            try
            {
                await service.MarkRunStartedAsync(CreateJob(), CheckpointKey, this.TestCancellationToken);
            }
            catch (InvalidOperationException)
            {
                exceptionThrown = true;
            }
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "Recording a run start inside a TransactionScope must throw.");
    }

    /// <summary>
    /// Unit test to verify that an advance with no cursor value is rejected as a
    /// likely bug rather than stored as an empty position.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task TryAdvanceAsync_EmptyPosition_ThrowsArgumentException()
    {
        // Arrange (Given)
        LoggingJobCheckpointService service = this.CreateSqliteService();
        ArgumentException? caughtException = null;

        // Act (When)
        try
        {
            await service.TryAdvanceAsync(CreateJob(), CheckpointKey, 0, new LoggingJobCheckpointPosition(), "fire-1", this.TestCancellationToken);
        }
        catch (ArgumentException ex)
        {
            caughtException = ex;
        }

        // Assert (Then)
        Assert.IsNotNull(
            caughtException,
            "A position with no Id, On or Text must be rejected.");
        Assert.AreEqual(
            "position",
            caughtException.ParamName,
            "The exception must name the position parameter.");
    }

    /// <summary>
    /// Unit test to verify that a negative expected version is rejected.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task TryAdvanceAsync_NegativeExpectedVersion_ThrowsArgumentOutOfRangeException()
    {
        // Arrange (Given)
        LoggingJobCheckpointService service = this.CreateSqliteService();
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            await service.TryAdvanceAsync(CreateJob(), CheckpointKey, -1, new LoggingJobCheckpointPosition { Id = 1 }, "fire-1", this.TestCancellationToken);
        }
        catch (ArgumentOutOfRangeException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "A negative expected version must be rejected.");
    }

    /// <summary>
    /// Unit test to verify that a fire instance id at Quartz's own 140-character
    /// ENTRY_ID limit is accepted and stored.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task TryAdvanceAsync_FireInstanceIdAt140Characters_IsStored()
    {
        // Arrange (Given)
        LoggingJobCheckpointService service = this.CreateSqliteService();
        string longest = new string('f', 140);

        // Act (When)
        bool won = await service.TryAdvanceAsync(CreateJob(), CheckpointKey, 0, new LoggingJobCheckpointPosition { Id = 1 }, longest, this.TestCancellationToken);
        LoggingJobCheckpoint? checkpoint = await service.ReadAsync(CreateJob(), CheckpointKey, this.TestCancellationToken);

        // Assert (Then)
        Assert.IsTrue(
            won,
            "An advance with a 140-character fire instance id must win.");
        Assert.AreEqual(
            longest,
            checkpoint?.AdvancedByFireInstanceId,
            "The full 140-character fire instance id must be stored.");
    }

    /// <summary>
    /// Unit test to verify that a fire instance id longer than the column is
    /// rejected before the database.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task TryAdvanceAsync_FireInstanceIdOver140Characters_ThrowsArgumentException()
    {
        // Arrange (Given)
        LoggingJobCheckpointService service = this.CreateSqliteService();
        ArgumentException? caughtException = null;

        // Act (When)
        try
        {
            await service.TryAdvanceAsync(CreateJob(), CheckpointKey, 0, new LoggingJobCheckpointPosition { Id = 1 }, new string('f', 141), this.TestCancellationToken);
        }
        catch (ArgumentException ex)
        {
            caughtException = ex;
        }

        // Assert (Then)
        Assert.IsNotNull(
            caughtException,
            "A fire instance id longer than 140 characters must be rejected.");
        Assert.AreEqual(
            "fireInstanceId",
            caughtException.ParamName,
            "The exception must name the fireInstanceId parameter.");
    }

    /// <summary>
    /// Unit test to verify that when the UPDATE reports no change but the row
    /// shows this call's own advance (its commit landed before the
    /// acknowledgement was lost and the executor retried), the advance is
    /// reported as won.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task TryAdvanceAsync_ZeroRowsButOwnCommitLanded_ReturnsTrue()
    {
        // Arrange (Given)
        var repository = CreateRepositoryReportingZeroRows(new LoggingJobCheckpoint
        {
            Version = 5,
            PositionId = 99,
            AdvancedByFireInstanceId = "fire-1",
        });
        LoggingJobCheckpointService service = this.CreateService(repository.Object, CreateOptions());

        // Act (When)
        bool won = await service.TryAdvanceAsync(CreateJob(), CheckpointKey, 4, new LoggingJobCheckpointPosition { Id = 99 }, "fire-1", this.TestCancellationToken);

        // Assert (Then)
        Assert.IsTrue(
            won,
            "A row showing this run's own advance to the next version must count as a win.");
    }

    /// <summary>
    /// Unit test to verify that when the UPDATE reports no change and the row
    /// shows another run's advance, the advance is reported as lost.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task TryAdvanceAsync_ZeroRowsAndOtherRunAdvanced_ReturnsFalse()
    {
        // Arrange (Given)
        var repository = CreateRepositoryReportingZeroRows(new LoggingJobCheckpoint
        {
            Version = 5,
            PositionId = 99,
            AdvancedByFireInstanceId = "fire-2",
        });
        LoggingJobCheckpointService service = this.CreateService(repository.Object, CreateOptions());

        // Act (When)
        bool won = await service.TryAdvanceAsync(CreateJob(), CheckpointKey, 4, new LoggingJobCheckpointPosition { Id = 99 }, "fire-1", this.TestCancellationToken);

        // Assert (Then)
        Assert.IsFalse(
            won,
            "A row advanced by a different run must count as a loss.");
    }

    /// <summary>
    /// Unit test to verify that the same run advancing twice from one version
    /// with different positions does not get a second win from the re-read.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task TryAdvanceAsync_ZeroRowsSameRunOtherPosition_ReturnsFalse()
    {
        // Arrange (Given)
        var repository = CreateRepositoryReportingZeroRows(new LoggingJobCheckpoint
        {
            Version = 5,
            PositionId = 50,
            AdvancedByFireInstanceId = "fire-1",
        });
        LoggingJobCheckpointService service = this.CreateService(repository.Object, CreateOptions());

        // Act (When)
        bool won = await service.TryAdvanceAsync(CreateJob(), CheckpointKey, 4, new LoggingJobCheckpointPosition { Id = 99 }, "fire-1", this.TestCancellationToken);

        // Assert (Then)
        Assert.IsFalse(
            won,
            "A row holding a different position than this call wrote must count as a loss.");
    }

    /// <summary>
    /// Unit test to verify that without a fire instance id a zero-row UPDATE is
    /// a loss and the service does not re-read (the strict mock has no read set up).
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task TryAdvanceAsync_ZeroRowsWithoutFireInstanceId_ReturnsFalse()
    {
        // Arrange (Given)
        var repository = new Mock<ILoggingJobCheckpointRepository>(MockBehavior.Strict);
        repository
            .Setup(r => r.EnsureExistsAsync(It.IsAny<LoggingJobCheckpointKey>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository
            .Setup(r => r.AdvanceAsync(It.IsAny<LoggingJobCheckpointKey>(), It.IsAny<long>(), It.IsAny<LoggingJobCheckpointPosition>(), It.IsAny<string?>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        LoggingJobCheckpointService service = this.CreateService(repository.Object, CreateOptions());

        // Act (When)
        bool won = await service.TryAdvanceAsync(CreateJob(), CheckpointKey, 4, new LoggingJobCheckpointPosition { Id = 99 }, null, this.TestCancellationToken);

        // Assert (Then)
        Assert.IsFalse(
            won,
            "Without a fire instance id a zero-row advance cannot be proven and must count as a loss.");
    }

    /// <summary>
    /// Unit test to verify that an UPDATE changing more than one row, which only a
    /// table missing its primary key could produce, fails loudly.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task TryAdvanceAsync_MoreThanOneRowChanged_ThrowsInvalidOperationException()
    {
        // Arrange (Given)
        var repository = new Mock<ILoggingJobCheckpointRepository>(MockBehavior.Strict);
        repository
            .Setup(r => r.EnsureExistsAsync(It.IsAny<LoggingJobCheckpointKey>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository
            .Setup(r => r.AdvanceAsync(It.IsAny<LoggingJobCheckpointKey>(), It.IsAny<long>(), It.IsAny<LoggingJobCheckpointPosition>(), It.IsAny<string?>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        LoggingJobCheckpointService service = this.CreateService(repository.Object, CreateOptions());
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            await service.TryAdvanceAsync(CreateJob(), CheckpointKey, 0, new LoggingJobCheckpointPosition { Id = 1 }, "fire-1", this.TestCancellationToken);
        }
        catch (InvalidOperationException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "An advance that changed more than one row must throw.");
    }

    /// <summary>
    /// Unit test to verify that a database failure during an advance propagates
    /// instead of being reported as a lost race.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task TryAdvanceAsync_RepositoryThrows_PropagatesException()
    {
        // Arrange (Given)
        var repository = new Mock<ILoggingJobCheckpointRepository>(MockBehavior.Strict);
        repository
            .Setup(r => r.EnsureExistsAsync(It.IsAny<LoggingJobCheckpointKey>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository
            .Setup(r => r.AdvanceAsync(It.IsAny<LoggingJobCheckpointKey>(), It.IsAny<long>(), It.IsAny<LoggingJobCheckpointPosition>(), It.IsAny<string?>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("simulated database failure"));
        LoggingJobCheckpointService service = this.CreateService(repository.Object, CreateOptions());
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            await service.TryAdvanceAsync(CreateJob(), CheckpointKey, 0, new LoggingJobCheckpointPosition { Id = 1 }, "fire-1", this.TestCancellationToken);
        }
        catch (TimeoutException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "A failed advance must throw, never return false.");
    }

    /// <summary>
    /// Unit test to verify that recording a run start on a never-run checkpoint
    /// creates the row at version 0 with no position and no advance time.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task MarkRunStartedAsync_NeverRun_CreatesRowWithoutPosition()
    {
        // Arrange (Given)
        LoggingJobCheckpointService service = this.CreateSqliteService();
        ILoggingCheckpointedJob job = CreateJob();

        // Act (When)
        await service.MarkRunStartedAsync(job, CheckpointKey, this.TestCancellationToken);
        LoggingJobCheckpoint? checkpoint = await service.ReadAsync(job, CheckpointKey, this.TestCancellationToken);

        // Assert (Then)
        Assert.IsNotNull(
            checkpoint,
            "Recording a run start must create the row.");
        Assert.AreEqual(
            0L,
            checkpoint.Version,
            "Recording a run start must leave the version at 0.");
        Assert.IsNull(
            checkpoint.PositionId,
            "Recording a run start must not set a position.");
        Assert.IsNull(
            checkpoint.AdvancedOn,
            "Recording a run start must not set advanced_on.");
        Assert.AreEqual(
            StartInstant.UtcDateTime,
            checkpoint.LastRunStartedOn,
            "last_run_started_on must be stamped from the injected clock.");
    }

    /// <summary>
    /// Unit test to verify that recording a later run start moves only
    /// last_run_started_on: a deferred pass must not reset the gate that reads
    /// advanced_on, nor move the position or the version.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task MarkRunStartedAsync_AfterAdvance_LeavesPositionVersionAndAdvancedOnUnchanged()
    {
        // Arrange (Given)
        LoggingJobCheckpointService service = this.CreateSqliteService();
        ILoggingCheckpointedJob job = CreateJob();
        await service.TryAdvanceAsync(job, CheckpointKey, 0, new LoggingJobCheckpointPosition { Id = 10 }, "fire-1", this.TestCancellationToken);
        this._timeProvider!.Advance(TimeSpan.FromHours(2));
        DateTime deferredPassStart = this._timeProvider.GetUtcNow().UtcDateTime;

        // Act (When)
        await service.MarkRunStartedAsync(job, CheckpointKey, this.TestCancellationToken);
        LoggingJobCheckpoint? checkpoint = await service.ReadAsync(job, CheckpointKey, this.TestCancellationToken);

        // Assert (Then)
        Assert.IsNotNull(
            checkpoint,
            "The checkpoint must exist.");
        Assert.AreEqual(
            deferredPassStart,
            checkpoint.LastRunStartedOn,
            "last_run_started_on must move to the deferred pass's start.");
        Assert.AreEqual(
            StartInstant.UtcDateTime,
            checkpoint.AdvancedOn,
            "advanced_on must stay at the last real advance.");
        Assert.AreEqual(
            1L,
            checkpoint.Version,
            "Recording a run start must not change the version.");
        Assert.AreEqual(
            10L,
            checkpoint.PositionId,
            "Recording a run start must not change the position.");
    }

    /// <summary>
    /// Unit test to verify that a run which records its start and then advances
    /// from the version it read (0 for never run) still wins its own
    /// compare-and-set.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task TryAdvanceAsync_AfterMarkRunStarted_WinsFromVersionZero()
    {
        // Arrange (Given)
        LoggingJobCheckpointService service = this.CreateSqliteService();
        ILoggingCheckpointedJob job = CreateJob();
        LoggingJobCheckpoint? read = await service.ReadAsync(job, CheckpointKey, this.TestCancellationToken);
        await service.MarkRunStartedAsync(job, CheckpointKey, this.TestCancellationToken);

        // Act (When)
        bool won = await service.TryAdvanceAsync(job, CheckpointKey, read?.Version ?? 0, new LoggingJobCheckpointPosition { Id = 7 }, "fire-1", this.TestCancellationToken);

        // Assert (Then)
        Assert.IsTrue(
            won,
            "Recording a run start must not make the run lose its own advance.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates the checkpointed job every test uses.
    /// </summary>
    /// <returns>A job in group <c>helpwanted</c> named <c>HelpWantedMatch</c>.</returns>
    private static ILoggingCheckpointedJob CreateJob()
    {
        return new TestCheckpointedJob { Name = "HelpWantedMatch", JobGroup = "helpwanted" };
    }

    /// <summary>
    /// Creates valid logging options.
    /// </summary>
    /// <returns>Options with a non-blank application and environment.</returns>
    private static LoggingOptions CreateOptions()
    {
        return new LoggingOptions { Application = "Pebble.Window.Service", Environment = "Production" };
    }

    /// <summary>
    /// Creates a strict repository whose advance reports zero changed rows and
    /// whose re-read returns <paramref name="reread"/>.
    /// </summary>
    /// <param name="reread">The row the service sees when it re-reads.</param>
    /// <returns>The configured mock.</returns>
    private static Mock<ILoggingJobCheckpointRepository> CreateRepositoryReportingZeroRows(LoggingJobCheckpoint reread)
    {
        var repository = new Mock<ILoggingJobCheckpointRepository>(MockBehavior.Strict);
        repository
            .Setup(r => r.EnsureExistsAsync(It.IsAny<LoggingJobCheckpointKey>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository
            .Setup(r => r.AdvanceAsync(It.IsAny<LoggingJobCheckpointKey>(), It.IsAny<long>(), It.IsAny<LoggingJobCheckpointPosition>(), It.IsAny<string?>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        repository
            .Setup(r => r.ReadAsync(It.IsAny<LoggingJobCheckpointKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(reread);
        return repository;
    }

    /// <summary>
    /// Counts the rows in the test database's <c>job_checkpoint</c> table.
    /// </summary>
    /// <returns>The row count.</returns>
    private long CountRows()
    {
        using SqliteCommand command = this._keepAlive!.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM job_checkpoint;";
        return Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Creates a service over the given repository with the test clock.
    /// </summary>
    /// <param name="repository">The repository the service uses.</param>
    /// <param name="options">The logging options the service uses.</param>
    /// <returns>The service under test.</returns>
    private LoggingJobCheckpointService CreateService(
        ILoggingJobCheckpointRepository repository,
        LoggingOptions options)
    {
        return new LoggingJobCheckpointService(
            repository,
            options,
            this._timeProvider!,
            NullLogger<LoggingJobCheckpointService>.Instance);
    }

    /// <summary>
    /// Creates a service over the real repository and the test's SQLite database.
    /// </summary>
    /// <param name="options">The logging options; defaults to <see cref="CreateOptions"/>.</param>
    /// <returns>The service under test.</returns>
    private LoggingJobCheckpointService CreateSqliteService(LoggingOptions? options = null)
    {
        LoggingOptions effective = options ?? CreateOptions();
        var repository = new LoggingJobCheckpointRepository(
            new SqliteTestExecutor(),
            this._factory!,
            effective,
            NullLogger<LoggingJobCheckpointRepository>.Instance);

        return this.CreateService(repository, effective);
    }

    #endregion Private Methods

    #region Private Types

    /// <summary>
    /// A minimal checkpointed job identity.
    /// </summary>
    private sealed class TestCheckpointedJob : ILoggingCheckpointedJob
    {
        /// <inheritdoc/>
        public string Name { get; init; } = string.Empty;

        /// <inheritdoc/>
        public string JobGroup { get; init; } = string.Empty;
    }

    /// <summary>
    /// SQLite connection factory that also carries the Roadbed.Logging marker.
    /// </summary>
    private sealed class SqliteCheckpointDatabaseFactory : SqliteConnectionFactory, ILoggingDatabaseFactory
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SqliteCheckpointDatabaseFactory"/> class.
        /// </summary>
        /// <param name="connectionString">The in-memory database connection string.</param>
        public SqliteCheckpointDatabaseFactory(DataConnecionString connectionString)
            : base(connectionString)
        {
        }
    }

    /// <summary>
    /// The same thin adapter over <see cref="SqliteExecutor"/> that
    /// Roadbed.Logging.Sqlite ships (its own type is internal to that assembly).
    /// </summary>
    private sealed class SqliteTestExecutor : ILoggingDataExecutor
    {
        /// <inheritdoc/>
        public Task<int> ExecuteAsync(
            DataExecutorRequest request,
            ILoggingDatabaseFactory factory,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            return SqliteExecutor.ExecuteAsync(request, factory, logger, cancellationToken);
        }

        /// <inheritdoc/>
        public Task<IEnumerable<T>> QueryAsync<T>(
            DataExecutorRequest request,
            ILoggingDatabaseFactory factory,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            return SqliteExecutor.QueryAsync<T>(request, factory, logger, cancellationToken);
        }
    }

    #endregion Private Types
}
