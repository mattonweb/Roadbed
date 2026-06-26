namespace Roadbed.DbQueue;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Roadbed.Data;
using Roadbed.DbQueue.Internal;

/// <summary>
/// Enqueue + drain engine for a single queue bound by
/// <see cref="QueueDefinition{T}"/>.
/// </summary>
/// <typeparam name="T">The payload type carried by this queue.</typeparam>
/// <remarks>
/// <para>
/// <strong>Single-consumer assumption.</strong> The processor assumes one
/// instance per queue is running at a time. The consuming host runs
/// <see cref="ProcessBatchAsync"/> from a single-worker, non-overlapping
/// scheduled job (the standard
/// <c>[DisallowConcurrentExecution]</c> Roadbed.Scheduling pattern). There
/// is no <c>SKIP LOCKED</c>, row claim lock, visibility timeout, or
/// in-flight state. If two processors ran the same queue concurrently both
/// anti-joins could claim the same message and the handler would run twice
/// — a single per-month DB unique on the processed table would still let
/// only one processed row land per partition, but the handler-side effect
/// would already have happened. Moving this library to multi-consumer is a
/// flagged change, not a silent race.
/// </para>
/// <para>
/// <strong>Once-only is enforced in code, not by the DB.</strong> The claim
/// uses an app-level LEFT JOIN anti-join against
/// <c>queue_processed_{name}.fk_queue_id</c> — any message that already has
/// a processed row (success <em>or</em> failure) is excluded from every
/// future batch. The per-month composite UNIQUE on the processed table is
/// only a backstop within a single partition; the anti-join is the real
/// idempotency guard. Failed messages are never auto-retried — they reach
/// the host's observability stack via <see cref="LogLevel.Error"/> and the
/// returned <see cref="QueueProcessResult.Failed"/> count, and an operator
/// replays them externally by deleting the processed row. Handlers must
/// therefore be idempotent; the library does not enforce this.
/// </para>
/// </remarks>
public sealed class QueueProcessor<T> : BaseClassWithLogging
{
    #region Private Fields

    private readonly QueueDefinition<T> _definition;
    private readonly IDbQueueDataExecutor _executor;
    private readonly string _messageTableRef;
    private readonly string _processedTableRef;

    #endregion Private Fields

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="QueueProcessor{T}"/> class
    /// using the executor registered in <see cref="ServiceLocator"/>.
    /// </summary>
    /// <param name="definition">The queue definition (name + per-queue connection factory).</param>
    /// <param name="logger">Logger used for retry/diagnostic messages on the data path.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="definition"/> is <c>null</c>.</exception>
    /// <remarks>
    /// Resolves <see cref="IDbQueueDataExecutor"/> from
    /// <see cref="ServiceLocator.GetService{T}"/>; the provider satellite's
    /// installer (e.g. <c>InstallDbQueueMySql</c>) must have run first so the
    /// executor is present in the captured snapshot.
    /// </remarks>
    public QueueProcessor(
        QueueDefinition<T> definition,
        ILogger<QueueProcessor<T>> logger)
        : this(definition, ServiceLocator.GetService<IDbQueueDataExecutor>(), logger)
    {
    }

    #endregion Public Constructors

    #region Internal Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="QueueProcessor{T}"/> class
    /// taking the executor directly. Used by unit tests reaching across
    /// <c>InternalsVisibleTo</c>.
    /// </summary>
    /// <param name="definition">The queue definition (name + per-queue connection factory).</param>
    /// <param name="executor">Provider-neutral execution port supplied by the active provider satellite.</param>
    /// <param name="logger">Logger used for retry/diagnostic messages on the data path.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="definition"/> or <paramref name="executor"/> is <c>null</c>.</exception>
    internal QueueProcessor(
        QueueDefinition<T> definition,
        IDbQueueDataExecutor executor,
        ILogger<QueueProcessor<T>> logger)
        : base(logger)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(executor);

        this._definition = definition;
        this._executor = executor;
        this._messageTableRef = $"`{definition.MessageTableName}`";
        this._processedTableRef = $"`{definition.ProcessedTableName}`";
    }

    #endregion Internal Constructors

    #region Public Methods

    /// <summary>
    /// Appends one message to this queue and returns its shareable UUIDv7
    /// external identifier.
    /// </summary>
    /// <param name="payload">The payload to enqueue. Serialized with the shared <see cref="RoadbedJson.Options"/>.</param>
    /// <param name="cancellationToken">Token observed by the call.</param>
    /// <returns>The minted UUIDv7 external id, in canonical lowercase hyphenated 36-character form. Use this as the shareable handle for confirmation URLs, support lookups, or cross-system correlation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="payload"/> is <c>null</c>.</exception>
    /// <remarks>
    /// <para>
    /// The external id is minted with <see cref="Guid.CreateVersion7()"/>. The
    /// row's <c>created_on</c> is left to the table default
    /// (<c>UTC_TIMESTAMP(6)</c>) so the partition key remains UTC regardless
    /// of the connection's session time zone. The internal auto-increment
    /// <c>id</c> is never surfaced to the caller — only the external id is.
    /// </para>
    /// </remarks>
    public async Task<string> EnqueueAsync(
        T payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);

        string externalId = Guid.CreateVersion7().ToString("D");
        string json = JsonSerializer.Serialize(payload, RoadbedJson.Options);

        string sql = $@"
            INSERT INTO {this._messageTableRef}
            (
                 external_id
                ,payload
            )
            VALUES
            (
                 @ExternalId
                ,@Payload
            )
            ;";

        var request = new DataExecutorRequest(sql)
        {
            Parameters = new
            {
                ExternalId = externalId,
                Payload = json,
            },
        };

        await this._executor
            .ExecuteAsync(request, this._definition.ConnectionFactory, this.Logger, cancellationToken)
            .ConfigureAwait(false);

        return externalId;
    }

    /// <summary>
    /// Claims up to <paramref name="batchSize"/> unprocessed messages in
    /// FIFO order, dispatches each to <paramref name="handler"/>, and records
    /// one processed row per attempt.
    /// </summary>
    /// <param name="batchSize">Upper bound on the number of messages drawn this batch. Must be greater than zero.</param>
    /// <param name="handler">Caller-supplied handler invoked once per claimed message.</param>
    /// <param name="cancellationToken">Token observed by the call and forwarded to the handler.</param>
    /// <returns>A summary with the attempted/succeeded/failed counts.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="batchSize"/> is not positive.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="handler"/> is <c>null</c>.</exception>
    /// <remarks>
    /// <para>
    /// The claim is a LEFT JOIN anti-join over
    /// <c>queue_processed_{name}.fk_queue_id</c> — any message that already
    /// has a processed row (regardless of success flag) is excluded forever.
    /// One <c>queue_processed_{name}</c> row is inserted per attempt
    /// <em>immediately</em> after the handler completes, not in a batched
    /// end-of-method commit, so a mid-batch crash still records every
    /// already-attempted message and leaves the rest eligible for the next
    /// run. A handler that throws (or a payload that fails to deserialize)
    /// is caught, recorded with <c>is_processed_successfully = 0</c>, logged
    /// at <see cref="LogLevel.Error"/> with the queue name and external id,
    /// and the batch continues.
    /// </para>
    /// </remarks>
    public async Task<QueueProcessResult> ProcessBatchAsync(
        int batchSize,
        QueueMessageHandler<T> handler,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);
        ArgumentNullException.ThrowIfNull(handler);

        IEnumerable<ClaimedMessageRow> claimed = await this
            .ClaimBatchAsync(batchSize, cancellationToken)
            .ConfigureAwait(false);

        int attempted = 0;
        int succeeded = 0;
        int failed = 0;

        foreach (ClaimedMessageRow row in claimed)
        {
            attempted++;

            bool isSuccess = await this
                .DispatchAndRecordAsync(row, handler, cancellationToken)
                .ConfigureAwait(false);

            if (isSuccess)
            {
                succeeded++;
            }
            else
            {
                failed++;
            }
        }

        return new QueueProcessResult(attempted, succeeded, failed);
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Runs the FIFO anti-join claim and returns up to
    /// <paramref name="batchSize"/> oldest unprocessed rows.
    /// </summary>
    /// <param name="batchSize">Upper bound on the number of rows returned.</param>
    /// <param name="cancellationToken">Token observed by the call.</param>
    /// <returns>The claimed rows, ordered by ascending <c>id</c>.</returns>
    private async Task<IEnumerable<ClaimedMessageRow>> ClaimBatchAsync(
        int batchSize,
        CancellationToken cancellationToken)
    {
        // The anti-join is the real idempotency guard. Column aliases match
        // the ClaimedMessageRow property names so the Dapper materializer in
        // the satellite executor binds without snake_case mapping.
        string sql = $@"
            SELECT
                 m.id          AS Id
                ,m.external_id AS ExternalId
                ,m.created_on  AS CreatedOn
                ,m.payload     AS Payload
            FROM {this._messageTableRef} AS m
            LEFT JOIN {this._processedTableRef} AS p
                ON p.fk_queue_id = m.id
            WHERE p.fk_queue_id IS NULL
            ORDER BY m.id ASC
            LIMIT @BatchSize
            ;";

        var request = new DataExecutorRequest(sql)
        {
            Parameters = new
            {
                BatchSize = batchSize,
            },
        };

        return await this._executor
            .QueryAsync<ClaimedMessageRow>(request, this._definition.ConnectionFactory, this.Logger, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Deserializes one row's payload, dispatches it to the handler, and
    /// records one processed row with the corresponding success flag. A
    /// throw at any step is caught here so the batch continues.
    /// </summary>
    /// <param name="row">The claimed row to dispatch.</param>
    /// <param name="handler">Caller-supplied handler.</param>
    /// <param name="cancellationToken">Token observed by the call and forwarded to the handler.</param>
    /// <returns><c>true</c> if the handler returned normally; <c>false</c> if the payload failed to deserialize, the handler threw, or the row failed any other per-message guard.</returns>
    private async Task<bool> DispatchAndRecordAsync(
        ClaimedMessageRow row,
        QueueMessageHandler<T> handler,
        CancellationToken cancellationToken)
    {
        bool isSuccess = false;

        try
        {
            T? payload = JsonSerializer.Deserialize<T>(row.Payload, RoadbedJson.Options);
            if (payload is null)
            {
                throw new InvalidOperationException(
                    $"Payload for queue '{this._definition.QueueName}' message '{row.ExternalId}' deserialized to null.");
            }

            var message = new QueueMessage<T>(row.Id, row.ExternalId, row.CreatedOn, payload);
            await handler(message, cancellationToken).ConfigureAwait(false);
            isSuccess = true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Cancellation requested during dispatch; the per-message record
            // below still runs so the message is not silently re-claimed on
            // the next batch, but the cancellation flag is restored before
            // we exit the method.
            this.LogWarning(
                "Queue {QueueName} message {ExternalId} canceled mid-dispatch; recorded as failed.",
                this._definition.QueueName,
                row.ExternalId);
        }
#pragma warning disable CA1031 // catch general exception: a handler-thrown failure must not stop the batch (spec §8)
        catch (Exception ex)
        {
            this.LogError(
                ex,
                "Queue {QueueName} message {ExternalId} handler failed; recording is_processed_successfully = 0.",
                this._definition.QueueName,
                row.ExternalId);
        }
#pragma warning restore CA1031

        await this.RecordProcessedAsync(row.Id, isSuccess, cancellationToken).ConfigureAwait(false);
        return isSuccess;
    }

    /// <summary>
    /// Inserts one row into the processed companion table with the supplied
    /// success flag.
    /// </summary>
    /// <param name="fkQueueId">Internal id of the source message row.</param>
    /// <param name="isProcessedSuccessfully"><c>true</c> when the handler returned; <c>false</c> when it threw or the payload could not be deserialized.</param>
    /// <param name="cancellationToken">Token observed by the call.</param>
    /// <returns>A task that completes when the row has been written.</returns>
    private async Task RecordProcessedAsync(
        long fkQueueId,
        bool isProcessedSuccessfully,
        CancellationToken cancellationToken)
    {
        string sql = $@"
            INSERT INTO {this._processedTableRef}
            (
                 fk_queue_id
                ,is_processed_successfully
            )
            VALUES
            (
                 @FkQueueId
                ,@IsProcessedSuccessfully
            )
            ;";

        var request = new DataExecutorRequest(sql)
        {
            Parameters = new
            {
                FkQueueId = fkQueueId,
                IsProcessedSuccessfully = isProcessedSuccessfully ? 1 : 0,
            },
        };

        await this._executor
            .ExecuteAsync(request, this._definition.ConnectionFactory, this.Logger, cancellationToken)
            .ConfigureAwait(false);
    }

    #endregion Private Methods
}
