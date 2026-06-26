namespace Roadbed.Test.Unit.DbQueue;

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed;
using Roadbed.Data;
using Roadbed.DbQueue;
using Roadbed.DbQueue.Internal;

/// <summary>
/// Unit tests for <see cref="QueueProcessor{T}"/>.
/// </summary>
[TestClass]
public class QueueProcessorTests
{
    #region Private Fields

    /// <summary>
    /// Compiled UUIDv7 "D" form regex: version nibble is 7, variant high
    /// nibble is 8/9/A/B.
    /// </summary>
    private static readonly Regex UuidV7Pattern = new (
        "^[0-9a-f]{8}-[0-9a-f]{4}-7[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    #endregion Private Fields

    #region Public Methods

    /// <summary>
    /// Verifies that a null payload is rejected before any SQL runs.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task EnqueueAsync_NullPayload_ThrowsArgumentNullException()
    {
        // Arrange (Given)
        QueueProcessor<TestPayload> processor = BuildProcessor(out CapturingExecutor _);
        bool thrown = false;

        // Act (When)
        try
        {
            await processor.EnqueueAsync(payload: null!);
        }
        catch (ArgumentNullException)
        {
            thrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(thrown, "Null payload must throw ArgumentNullException.");
    }

    /// <summary>
    /// Verifies that <see cref="QueueProcessor{T}.EnqueueAsync"/> mints a
    /// 36-character UUIDv7 "D" form external id and returns it.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task EnqueueAsync_ValidPayload_ReturnsUuidV7DFormatExternalId()
    {
        // Arrange (Given)
        QueueProcessor<TestPayload> processor = BuildProcessor(out CapturingExecutor _);

        // Act (When)
        string externalId = await processor.EnqueueAsync(new TestPayload { Email = "a@b" });

        // Assert (Then)
        Assert.AreEqual(36, externalId.Length, "External id must be 36 characters (UUIDv7 'D' form).");
        StringAssert.Matches(
            externalId,
            UuidV7Pattern,
            "External id must be a canonical lowercase hyphenated UUIDv7.");
    }

    /// <summary>
    /// Verifies the enqueue SQL targets the backtick-wrapped, validated
    /// message table name — defense-in-depth that an invalid name cannot
    /// reach the SQL composer.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task EnqueueAsync_ValidPayload_InsertsIntoBacktickWrappedMessageTable()
    {
        // Arrange (Given)
        QueueProcessor<TestPayload> processor = BuildProcessor(out CapturingExecutor executor);

        // Act (When)
        _ = await processor.EnqueueAsync(new TestPayload { Email = "a@b" });

        // Assert (Then)
        Assert.HasCount(1, executor.Executes, "EnqueueAsync should execute exactly one INSERT.");
        string sql = executor.Executes[0].Query;
        StringAssert.Contains(sql, "INSERT INTO `queue_message_foo_unsubscribe`", "Enqueue must INSERT into the backtick-wrapped message table.");
        StringAssert.Contains(sql, "@ExternalId", "external_id must be passed as a real query parameter.");
        StringAssert.Contains(sql, "@Payload", "payload must be passed as a real query parameter.");
        Assert.IsFalse(
            sql.Contains("created_on", StringComparison.Ordinal),
            "Enqueue must NOT supply created_on — the DB default UTC_TIMESTAMP(6) owns the partition key.");
    }

    /// <summary>
    /// Verifies the payload is serialized through
    /// <see cref="Roadbed.RoadbedJson.Options"/>: the round-trip through the
    /// same options returns an equivalent object.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task EnqueueAsync_ValidPayload_SerializesWithRoadbedJsonOptions()
    {
        // Arrange (Given)
        QueueProcessor<TestPayload> processor = BuildProcessor(out CapturingExecutor executor);
        var original = new TestPayload { Email = "a@example.com", ListId = 42 };

        // Act (When)
        _ = await processor.EnqueueAsync(original);

        // Assert (Then)
        object? payloadParam = GetParam(executor.Executes[0].Parameters, "Payload");
        string json = (string)payloadParam!;
        TestPayload? roundTripped = JsonSerializer.Deserialize<TestPayload>(json, RoadbedJson.Options);
        Assert.IsNotNull(roundTripped, "Payload must deserialize back through the shared RoadbedJson options.");
        Assert.AreEqual(original.Email, roundTripped!.Email);
        Assert.AreEqual(original.ListId, roundTripped.ListId);
    }

    /// <summary>
    /// Verifies that <c>batchSize</c> &lt;= 0 throws before any SQL runs.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task ProcessBatchAsync_ZeroBatchSize_ThrowsArgumentOutOfRangeException()
    {
        // Arrange (Given)
        QueueProcessor<TestPayload> processor = BuildProcessor(out CapturingExecutor _);
        bool thrown = false;

        // Act (When)
        try
        {
            await processor.ProcessBatchAsync(0, (msg, ct) => Task.CompletedTask);
        }
        catch (ArgumentOutOfRangeException)
        {
            thrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(thrown, "ProcessBatchAsync must reject a non-positive batch size.");
    }

    /// <summary>
    /// Verifies that a null handler is rejected.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task ProcessBatchAsync_NullHandler_ThrowsArgumentNullException()
    {
        // Arrange (Given)
        QueueProcessor<TestPayload> processor = BuildProcessor(out CapturingExecutor _);
        bool thrown = false;

        // Act (When)
        try
        {
            await processor.ProcessBatchAsync(10, handler: null!);
        }
        catch (ArgumentNullException)
        {
            thrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(thrown, "ProcessBatchAsync must reject a null handler.");
    }

    /// <summary>
    /// Verifies that an empty queue yields zero-count result and never
    /// records a processed row.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task ProcessBatchAsync_EmptyQueue_ReturnsZeroCountsAndRecordsNothing()
    {
        // Arrange (Given)
        QueueProcessor<TestPayload> processor = BuildProcessor(out CapturingExecutor executor);
        executor.ClaimedRows = Array.Empty<ClaimedMessageRow>();
        bool handlerInvoked = false;

        // Act (When)
        QueueProcessResult result = await processor.ProcessBatchAsync(
            5,
            (msg, ct) =>
            {
                handlerInvoked = true;
                return Task.CompletedTask;
            });

        // Assert (Then)
        Assert.AreEqual(0, result.Attempted);
        Assert.AreEqual(0, result.Succeeded);
        Assert.AreEqual(0, result.Failed);
        Assert.IsFalse(handlerInvoked, "Handler must not be invoked when the claim is empty.");
        Assert.HasCount(0, executor.Executes, "Empty claim must not write any processed rows.");
    }

    /// <summary>
    /// Verifies the claim SQL is the anti-join shape — backtick-wrapped
    /// tables, LEFT JOIN against the processed table, IS NULL guard, FIFO
    /// ascending by id, parameterized LIMIT.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task ProcessBatchAsync_ClaimSql_AntiJoinFifoOrderParameterizedLimit()
    {
        // Arrange (Given)
        QueueProcessor<TestPayload> processor = BuildProcessor(out CapturingExecutor executor);

        // Act (When)
        _ = await processor.ProcessBatchAsync(7, (msg, ct) => Task.CompletedTask);

        // Assert (Then)
        Assert.HasCount(1, executor.Queries);
        string sql = executor.Queries[0].Query;

        StringAssert.Contains(sql, "FROM `queue_message_foo_unsubscribe` AS m", "Claim must target the backtick-wrapped message table.");
        StringAssert.Contains(sql, "LEFT JOIN `queue_processed_foo_unsubscribe` AS p", "Claim must LEFT JOIN the backtick-wrapped processed table.");
        StringAssert.Contains(sql, "ON p.fk_queue_id = m.id", "Claim must join on the logical id.");
        StringAssert.Contains(sql, "WHERE p.fk_queue_id IS NULL", "Anti-join must filter unprocessed rows only.");
        StringAssert.Contains(sql, "ORDER BY m.id ASC", "FIFO ordering must be ascending by id.");
        StringAssert.Contains(sql, "LIMIT @BatchSize", "Batch cap must be passed as a real query parameter.");

        object? batchSize = GetParam(executor.Queries[0].Parameters, "BatchSize");
        Assert.AreEqual(7, batchSize, "Batch size must be plumbed verbatim to the @BatchSize parameter.");
    }

    /// <summary>
    /// Verifies that a successful handler is dispatched once per claimed
    /// message and results in one processed-row INSERT with
    /// is_processed_successfully = 1.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task ProcessBatchAsync_SuccessfulHandler_RecordsFlagOnePerMessage()
    {
        // Arrange (Given)
        QueueProcessor<TestPayload> processor = BuildProcessor(out CapturingExecutor executor);
        executor.ClaimedRows = new[]
        {
            BuildRow(1, payload: new TestPayload { Email = "a@b", ListId = 1 }),
            BuildRow(2, payload: new TestPayload { Email = "c@d", ListId = 2 }),
        };

        var seenIds = new List<long>();

        // Act (When)
        QueueProcessResult result = await processor.ProcessBatchAsync(
            5,
            (msg, ct) =>
            {
                seenIds.Add(msg.Id);
                return Task.CompletedTask;
            });

        // Assert (Then)
        Assert.AreEqual(2, result.Attempted);
        Assert.AreEqual(2, result.Succeeded);
        Assert.AreEqual(0, result.Failed);
        CollectionAssert.AreEqual(new[] { 1L, 2L }, seenIds, "FIFO order from the claim must be preserved through dispatch.");

        Assert.HasCount(2, executor.Executes, "One processed-row INSERT per attempted message.");
        foreach (DataExecutorRequest req in executor.Executes)
        {
            StringAssert.Contains(req.Query, "INSERT INTO `queue_processed_foo_unsubscribe`", "Processed rows go to the backtick-wrapped processed table.");
            Assert.AreEqual(1, GetParam(req.Parameters, "IsProcessedSuccessfully"), "Successful handler must record is_processed_successfully = 1.");
        }

        Assert.AreEqual(1L, GetParam(executor.Executes[0].Parameters, "FkQueueId"));
        Assert.AreEqual(2L, GetParam(executor.Executes[1].Parameters, "FkQueueId"));
    }

    /// <summary>
    /// Verifies that a throwing handler:
    ///   (1) does not stop the batch,
    ///   (2) records is_processed_successfully = 0 for the failed message,
    ///   (3) lets the surrounding successful handlers complete normally.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task ProcessBatchAsync_ThrowingHandler_RecordsFlagZeroAndContinuesBatch()
    {
        // Arrange (Given)
        var logger = new RecordingLogger();
        QueueProcessor<TestPayload> processor = BuildProcessor(out CapturingExecutor executor, logger);
        executor.ClaimedRows = new[]
        {
            BuildRow(10, payload: new TestPayload { Email = "ok-1@x" }),
            BuildRow(20, payload: new TestPayload { Email = "boom@x" }),
            BuildRow(30, payload: new TestPayload { Email = "ok-2@x" }),
        };

        var seenIds = new List<long>();

        // Act (When)
        QueueProcessResult result = await processor.ProcessBatchAsync(
            5,
            (msg, ct) =>
            {
                seenIds.Add(msg.Id);
                if (msg.Payload.Email == "boom@x")
                {
                    throw new InvalidOperationException("simulated handler failure");
                }

                return Task.CompletedTask;
            });

        // Assert (Then)
        CollectionAssert.AreEqual(new[] { 10L, 20L, 30L }, seenIds, "A throwing handler must NOT stop the batch — every claimed row must still be dispatched.");

        Assert.AreEqual(3, result.Attempted);
        Assert.AreEqual(2, result.Succeeded);
        Assert.AreEqual(1, result.Failed);

        Assert.HasCount(3, executor.Executes, "Even the failed message records exactly one processed row.");
        Assert.AreEqual(1, GetParam(executor.Executes[0].Parameters, "IsProcessedSuccessfully"));
        Assert.AreEqual(0, GetParam(executor.Executes[1].Parameters, "IsProcessedSuccessfully"));
        Assert.AreEqual(1, GetParam(executor.Executes[2].Parameters, "IsProcessedSuccessfully"));

        Assert.IsTrue(
            logger.Records.Any(r => r.Level == LogLevel.Error && r.Exception is InvalidOperationException),
            "Handler failure must be logged at Error with the captured exception.");
    }

    /// <summary>
    /// Verifies the claim row's payload is deserialized through
    /// <see cref="Roadbed.RoadbedJson.Options"/>: the handler receives the
    /// same payload that was serialized in.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task ProcessBatchAsync_PayloadRoundTrip_HandlerReceivesDeserializedTyped()
    {
        // Arrange (Given)
        QueueProcessor<TestPayload> processor = BuildProcessor(out CapturingExecutor executor);
        executor.ClaimedRows = new[]
        {
            BuildRow(1, payload: new TestPayload { Email = "X@Y", ListId = 99 }),
        };

        TestPayload? received = null;

        // Act (When)
        _ = await processor.ProcessBatchAsync(
            1,
            (msg, ct) =>
            {
                received = msg.Payload;
                return Task.CompletedTask;
            });

        // Assert (Then)
        Assert.IsNotNull(received);
        Assert.AreEqual("X@Y", received!.Email);
        Assert.AreEqual(99, received.ListId);
    }

    /// <summary>
    /// Verifies that a payload that fails to deserialize is treated as a
    /// per-message failure (one processed row with flag = 0, batch
    /// continues, log entry at Error), not a hard stop.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task ProcessBatchAsync_CorruptPayload_RecordsFlagZeroAndContinues()
    {
        // Arrange (Given)
        var logger = new RecordingLogger();
        QueueProcessor<TestPayload> processor = BuildProcessor(out CapturingExecutor executor, logger);
        executor.ClaimedRows = new[]
        {
            new ClaimedMessageRow
            {
                Id = 1,
                ExternalId = "0193e9bf-cafe-7000-8000-000000000001",
                CreatedOn = new DateTime(2026, 6, 25, 0, 0, 0, DateTimeKind.Utc),
                Payload = "{this is not valid json",
            },
            BuildRow(2, payload: new TestPayload { Email = "ok@x" }),
        };

        bool secondDispatched = false;

        // Act (When)
        QueueProcessResult result = await processor.ProcessBatchAsync(
            5,
            (msg, ct) =>
            {
                if (msg.Id == 2)
                {
                    secondDispatched = true;
                }

                return Task.CompletedTask;
            });

        // Assert (Then)
        Assert.AreEqual(2, result.Attempted, "Corrupt-payload rows are still 'attempted' — the failure is surfaced via a processed row.");
        Assert.AreEqual(1, result.Succeeded);
        Assert.AreEqual(1, result.Failed);
        Assert.IsTrue(secondDispatched, "Corrupt payload must not stop the rest of the batch.");

        Assert.HasCount(2, executor.Executes);
        Assert.AreEqual(0, GetParam(executor.Executes[0].Parameters, "IsProcessedSuccessfully"));
        Assert.AreEqual(1, GetParam(executor.Executes[1].Parameters, "IsProcessedSuccessfully"));

        Assert.IsTrue(
            logger.Records.Any(r => r.Level == LogLevel.Error),
            "Corrupt-payload failure must be logged at Error.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Constructs a <see cref="QueueProcessor{T}"/> with the capturing
    /// executor and a fixed test queue name (<c>foo_unsubscribe</c>).
    /// </summary>
    /// <param name="executor">The capturing executor the processor will write through.</param>
    /// <param name="logger">Optional recording logger; null means the null logger.</param>
    /// <returns>A test-ready processor.</returns>
    private static QueueProcessor<TestPayload> BuildProcessor(
        out CapturingExecutor executor,
        ILogger<QueueProcessor<TestPayload>>? logger = null)
    {
        executor = new CapturingExecutor();
        var definition = new QueueDefinition<TestPayload>(
            "foo_unsubscribe",
            new StubConnectionFactory());
        return new QueueProcessor<TestPayload>(
            definition,
            executor,
            logger ?? NullLogger<QueueProcessor<TestPayload>>.Instance);
    }

    /// <summary>
    /// Builds a single claim-row stub with the supplied id and the JSON
    /// representation of <paramref name="payload"/> using the shared
    /// <see cref="Roadbed.RoadbedJson.Options"/>.
    /// </summary>
    /// <param name="id">FIFO id assigned to the stub row.</param>
    /// <param name="payload">Payload that will be serialized into the row's <c>payload</c> column.</param>
    /// <returns>A stub <see cref="ClaimedMessageRow"/>.</returns>
    private static ClaimedMessageRow BuildRow(long id, TestPayload payload)
    {
        return new ClaimedMessageRow
        {
            Id = id,
            ExternalId = Guid.CreateVersion7().ToString("D"),
            CreatedOn = new DateTime(2026, 6, 25, 0, 0, 0, DateTimeKind.Utc),
            Payload = JsonSerializer.Serialize(payload, RoadbedJson.Options),
        };
    }

    /// <summary>
    /// Reads a property value off an anonymous-typed Dapper parameter bag.
    /// </summary>
    /// <param name="parameters">The anonymous object placed on <see cref="DataExecutorRequest.Parameters"/>.</param>
    /// <param name="name">Property name to read.</param>
    /// <returns>The property value, or <c>null</c> if not present.</returns>
    private static object? GetParam(object? parameters, string name)
    {
        if (parameters is null)
        {
            return null;
        }

        return parameters.GetType().GetProperty(name)?.GetValue(parameters);
    }

    #endregion Private Methods

    #region Private Types

    /// <summary>
    /// Captures every <see cref="DataExecutorRequest"/> the processor issues
    /// and returns canned claim rows on <c>QueryAsync</c>.
    /// </summary>
    private sealed class CapturingExecutor : IDbQueueDataExecutor
    {
        /// <summary>
        /// Gets the list of INSERT requests the processor issued.
        /// </summary>
        public List<DataExecutorRequest> Executes { get; } = new ();

        /// <summary>
        /// Gets the list of SELECT requests the processor issued.
        /// </summary>
        public List<DataExecutorRequest> Queries { get; } = new ();

        /// <summary>
        /// Gets or sets the canned claim rows handed back from the next
        /// QueryAsync call.
        /// </summary>
        public IReadOnlyList<ClaimedMessageRow> ClaimedRows { get; set; } = Array.Empty<ClaimedMessageRow>();

        /// <inheritdoc/>
        public Task<int> ExecuteAsync(
            DataExecutorRequest request,
            IDataConnectionFactory factory,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            this.Executes.Add(request);
            return Task.FromResult(1);
        }

        /// <inheritdoc/>
        public Task<IEnumerable<TRow>> QueryAsync<TRow>(
            DataExecutorRequest request,
            IDataConnectionFactory factory,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            this.Queries.Add(request);

            if (this.ClaimedRows is IEnumerable<TRow> typed)
            {
                return Task.FromResult(typed);
            }

            return Task.FromResult<IEnumerable<TRow>>(Array.Empty<TRow>());
        }
    }

    /// <summary>
    /// Test-only stub for <see cref="IDataConnectionFactory"/>; not invoked.
    /// </summary>
    private sealed class StubConnectionFactory : IDataConnectionFactory
    {
        /// <inheritdoc/>
        public DataConnecionString Connecion { get; } =
            new (DataConnectionStringType.SQLiteInMemory) { DatabaseSource = "QueueProcessorTests" };

        /// <inheritdoc/>
        public IDbConnection CreateOpenConnection()
        {
            throw new InvalidOperationException("Stub: not expected to be called by QueueProcessor tests.");
        }

        /// <inheritdoc/>
        public Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Stub: not expected to be called by QueueProcessor tests.");
        }
    }

    /// <summary>
    /// Captures each log record the processor emits so failure-path tests
    /// can assert the Error-level log.
    /// </summary>
    private sealed class RecordingLogger : ILogger<QueueProcessor<TestPayload>>
    {
        /// <summary>
        /// Gets the captured records (oldest first).
        /// </summary>
        public List<Record> Records { get; } = new ();

        /// <inheritdoc/>
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        /// <inheritdoc/>
        public bool IsEnabled(LogLevel logLevel) => true;

        /// <inheritdoc/>
        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);
            this.Records.Add(new Record
            {
                Level = logLevel,
                Exception = exception,
                Message = formatter(state, exception),
            });
        }

        /// <summary>
        /// One captured log record.
        /// </summary>
        public sealed class Record
        {
            /// <summary>Gets or sets the level the record was logged at.</summary>
            public LogLevel Level { get; set; }

            /// <summary>Gets or sets the exception attached to the record (if any).</summary>
            public Exception? Exception { get; set; }

            /// <summary>Gets or sets the formatted message text.</summary>
            public string Message { get; set; } = string.Empty;
        }
    }

    /// <summary>
    /// Test payload used to verify round-trip serialization.
    /// </summary>
    private sealed class TestPayload
    {
        /// <summary>Gets or sets the email address carried by this message.</summary>
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        /// <summary>Gets or sets the list identifier.</summary>
        [JsonPropertyName("list_id")]
        public int ListId { get; set; }
    }

    #endregion Private Types
}
