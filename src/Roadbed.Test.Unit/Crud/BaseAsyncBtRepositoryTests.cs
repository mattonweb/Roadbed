namespace Roadbed.Test.Unit.Crud;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Crud;
using Roadbed.Crud.Operations.Async;
using Roadbed.Crud.Repositories.Async;

/// <summary>
/// Contains unit tests for verifying the behavior of the
/// <see cref="BaseAsyncBtRepository{TEntity, TId}"/> abstract class.
/// </summary>
[TestClass]
public class BaseAsyncBtRepositoryTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that parameterless constructor creates an instance successfully.
    /// </summary>
    [TestMethod]
    public void Constructor_NoParameters_CreatesInstance()
    {
        // Arrange (Given)

        // Act (When)
        var instance = new StubAsyncBtRepository();

        // Assert (Then)
        Assert.IsNotNull(
            instance,
            "Instance should be created successfully with the parameterless constructor.");
    }

    /// <summary>
    /// Unit test to verify that logger constructor creates an instance successfully.
    /// </summary>
    [TestMethod]
    public void Constructor_WithLogger_CreatesInstance()
    {
        // Arrange (Given)
        ILogger logger = NullLogger.Instance;

        // Act (When)
        var instance = new StubAsyncBtRepository(logger);

        // Assert (Then)
        Assert.IsNotNull(
            instance,
            "Instance should be created successfully with the logger constructor.");
    }

    /// <summary>
    /// Unit test to verify that instance implements IAsyncBtRepository interface.
    /// </summary>
    [TestMethod]
    public void Instance_TypeCheck_ImplementsIAsyncBtRepository()
    {
        // Arrange (Given)

        // Act (When)
        var instance = new StubAsyncBtRepository();

        // Assert (Then)
        Assert.IsInstanceOfType<IAsyncBtRepository<TestEntityRecord, long>>(
            instance,
            "Instance should implement IAsyncBtRepository<TestEntityRecord, long>.");
    }

    /// <summary>
    /// Unit test to verify that instance also implements IAsyncBulkOnlyRepository
    /// through inheritance.
    /// </summary>
    [TestMethod]
    public void Instance_TypeCheck_ImplementsIAsyncBulkOnlyRepository()
    {
        // Arrange (Given)

        // Act (When)
        var instance = new StubAsyncBtRepository();

        // Assert (Then)
        Assert.IsInstanceOfType<IAsyncBulkOnlyRepository<TestEntityRecord, long>>(
            instance,
            "Bt repository should also implement IAsyncBulkOnlyRepository via inheritance.");
    }

    /// <summary>
    /// Unit test to verify that instance implements IAsyncTruncateOperation.
    /// </summary>
    [TestMethod]
    public void Instance_TypeCheck_ImplementsIAsyncTruncateOperation()
    {
        // Arrange (Given)

        // Act (When)
        var instance = new StubAsyncBtRepository();

        // Assert (Then)
        Assert.IsInstanceOfType<IAsyncTruncateOperation<TestEntityRecord, long>>(
            instance,
            "Instance should implement IAsyncTruncateOperation<TestEntityRecord, long>.");
    }

    /// <summary>
    /// Unit test to verify that instance implements IRepository marker interface.
    /// </summary>
    [TestMethod]
    public void Instance_TypeCheck_ImplementsIRepository()
    {
        // Arrange (Given)

        // Act (When)
        var instance = new StubAsyncBtRepository();

        // Assert (Then)
        Assert.IsInstanceOfType<IRepository<TestEntityRecord, long>>(
            instance,
            "Instance should implement IRepository<TestEntityRecord, long>.");
    }

    /// <summary>
    /// Unit test to verify that instance inherits from BaseClassWithLogging.
    /// </summary>
    [TestMethod]
    public void Instance_TypeCheck_InheritsBaseClassWithLogging()
    {
        // Arrange (Given)

        // Act (When)
        var instance = new StubAsyncBtRepository();

        // Assert (Then)
        Assert.IsInstanceOfType<BaseClassWithLogging>(
            instance,
            "Instance should inherit from BaseClassWithLogging.");
    }

    /// <summary>
    /// Unit test to verify that BulkInsertAsync returns expected row count.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task BulkInsertAsync_Called_ReturnsExpectedRowCount()
    {
        // Arrange (Given)
        var rows = new List<TestEntityRecord>
        {
            new TestEntityRecord { Id = 1, Name = "First" },
            new TestEntityRecord { Id = 2, Name = "Second" },
            new TestEntityRecord { Id = 3, Name = "Third" },
        };

        var instance = new StubAsyncBtRepository
        {
            RowsToReturn = rows.Count,
        };

        // Act (When)
        long result = await instance.BulkInsertAsync("activity-1", rows);

        // Assert (Then)
        Assert.AreEqual(
            (long)rows.Count,
            result,
            "BulkInsertAsync should return the row count supplied by the concrete implementation.");
    }

    /// <summary>
    /// Unit test to verify that BulkInsertAsync passes its arguments to the implementation.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task BulkInsertAsync_Called_PassesArgumentsToImplementation()
    {
        // Arrange (Given)
        const string expectedActivityId = "activity-42";
        var expectedRows = new List<TestEntityRecord>
        {
            new TestEntityRecord { Id = 1, Name = "Row 1" },
        };
        using var cts = new CancellationTokenSource();
        CancellationToken expectedToken = cts.Token;

        var instance = new StubAsyncBtRepository();

        // Act (When)
        await instance.BulkInsertAsync(expectedActivityId, expectedRows, expectedToken);

        // Assert (Then)
        Assert.AreEqual(
            expectedActivityId,
            instance.LastBulkInsertActivityId,
            "BulkInsertAsync should pass the activity id to the concrete implementation.");
        Assert.AreSame(
            expectedRows,
            instance.LastBulkInsertRows,
            "BulkInsertAsync should pass the rows collection to the concrete implementation.");
        Assert.AreEqual(
            expectedToken,
            instance.LastBulkInsertCancellationToken,
            "BulkInsertAsync should pass the CancellationToken to the concrete implementation.");
    }

    /// <summary>
    /// Unit test to verify that TruncateAsync invokes the implementation.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task TruncateAsync_Called_InvokesImplementation()
    {
        // Arrange (Given)
        var instance = new StubAsyncBtRepository();

        // Act (When)
        await instance.TruncateAsync();

        // Assert (Then)
        Assert.AreEqual(
            1,
            instance.TruncateInvocationCount,
            "TruncateAsync should call the concrete implementation exactly once.");
    }

    /// <summary>
    /// Unit test to verify that TruncateAsync passes the CancellationToken to the implementation.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task TruncateAsync_WithCancellationToken_PassesTokenToImplementation()
    {
        // Arrange (Given)
        using var cts = new CancellationTokenSource();
        CancellationToken expectedToken = cts.Token;

        var instance = new StubAsyncBtRepository();

        // Act (When)
        await instance.TruncateAsync(expectedToken);

        // Assert (Then)
        Assert.AreEqual(
            expectedToken,
            instance.LastTruncateCancellationToken,
            "TruncateAsync should pass the CancellationToken to the concrete implementation.");
    }

    #endregion Public Methods

    #region Private Classes

    /// <summary>
    /// Concrete stub for testing the abstract BaseAsyncBtRepository.
    /// </summary>
    private sealed class StubAsyncBtRepository
        : BaseAsyncBtRepository<TestEntityRecord, long>
    {
        public long RowsToReturn { get; set; }

        public string? LastBulkInsertActivityId { get; private set; }

        public IList<TestEntityRecord>? LastBulkInsertRows { get; private set; }

        public CancellationToken LastBulkInsertCancellationToken { get; private set; }

        public int TruncateInvocationCount { get; private set; }

        public CancellationToken LastTruncateCancellationToken { get; private set; }

        public StubAsyncBtRepository()
            : base()
        {
        }

        public StubAsyncBtRepository(ILogger logger)
            : base(logger)
        {
        }

        public override Task<long> BulkInsertAsync(
            string activityId,
            IList<TestEntityRecord> rows,
            CancellationToken cancellationToken = default)
        {
            this.LastBulkInsertActivityId = activityId;
            this.LastBulkInsertRows = rows;
            this.LastBulkInsertCancellationToken = cancellationToken;
            return Task.FromResult(this.RowsToReturn);
        }

        public override Task TruncateAsync(CancellationToken cancellationToken = default)
        {
            this.TruncateInvocationCount++;
            this.LastTruncateCancellationToken = cancellationToken;
            return Task.CompletedTask;
        }
    }

    #endregion Private Classes
}
