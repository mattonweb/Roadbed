namespace Roadbed.Logging;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Data-access contract for the <c>activity</c> table.
/// </summary>
/// <remarks>
/// Each method maps to a single SQL statement against the activity row
/// addressed by its identifier. There is intentionally no read or list
/// surface here — Roadbed.Logging only writes activity rows; analysts read
/// them directly out of the database.
/// </remarks>
internal interface ILoggingActivityRepository
{
    #region Public Methods

    /// <summary>
    /// Inserts the supplied activity row in the <c>running</c> state.
    /// </summary>
    /// <param name="entity">The activity row to insert.</param>
    /// <param name="cancellationToken">Token to notify when the operation should be canceled.</param>
    /// <returns>A task that completes when the row has been inserted.</returns>
    Task InsertAsync(
        LoggingActivity entity,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Patches the supplied non-<c>null</c> fields onto an existing activity row.
    /// </summary>
    /// <param name="request">The current-state update request.</param>
    /// <param name="cancellationToken">Token to notify when the operation should be canceled.</param>
    /// <returns>A task that completes when the row has been patched.</returns>
    /// <remarks>
    /// Properties left at <c>null</c> on <paramref name="request"/> preserve
    /// their existing values via a <c>COALESCE</c>-driven UPDATE. When
    /// <see cref="LoggingActivityUpdateRequest.CreatedOn"/> is set, the
    /// generated SQL includes <c>AND created_on = @CreatedOn</c> in the
    /// WHERE clause so MySQL prunes to the one monthly partition that
    /// owns the row instead of probing every partition.
    /// </remarks>
    Task UpdateCurrentStateAsync(
        LoggingActivityUpdateRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stamps the supplied timestamp into the <c>last_heartbeat_on</c> column.
    /// </summary>
    /// <param name="activityId">Identifier of the activity row to update.</param>
    /// <param name="createdOn">When supplied, the SQL adds <c>AND created_on = @CreatedOn</c> so MySQL prunes the UPDATE to a single monthly partition. Pass <c>null</c> to fall back to the legacy single-column WHERE (probes every partition).</param>
    /// <param name="heartbeatOn">UTC moment of the heartbeat.</param>
    /// <param name="cancellationToken">Token to notify when the operation should be canceled.</param>
    /// <returns>A task that completes when the heartbeat has been recorded.</returns>
    Task RecordHeartbeatAsync(
        string activityId,
        DateTime? createdOn,
        DateTime heartbeatOn,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the activity row as terminal with the supplied status.
    /// </summary>
    /// <param name="activityId">Identifier of the activity row to update.</param>
    /// <param name="createdOn">When supplied, the SQL adds <c>AND created_on = @CreatedOn</c> so MySQL prunes the UPDATE to a single monthly partition. Pass <c>null</c> to fall back to the legacy single-column WHERE.</param>
    /// <param name="status">Terminal status to record.</param>
    /// <param name="completedOn">UTC moment the activity completed.</param>
    /// <param name="recordsImpacted">Optional headline count of records produced or affected.</param>
    /// <param name="metricsJson">Optional updated metrics JSON to persist alongside the terminal status.</param>
    /// <param name="cancellationToken">Token to notify when the operation should be canceled.</param>
    /// <returns>A task that completes when the row has been finalized.</returns>
    Task CompleteAsync(
        string activityId,
        DateTime? createdOn,
        LoggingActivityStatus status,
        DateTime completedOn,
        long? recordsImpacted,
        string? metricsJson,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the activity row as <see cref="LoggingActivityStatus.Failed"/>
    /// and records the captured exception.
    /// </summary>
    /// <param name="activityId">Identifier of the activity row to update.</param>
    /// <param name="createdOn">When supplied, the SQL adds <c>AND created_on = @CreatedOn</c> so MySQL prunes the UPDATE to a single monthly partition. Pass <c>null</c> to fall back to the legacy single-column WHERE.</param>
    /// <param name="completedOn">UTC moment the failure was recorded.</param>
    /// <param name="error">Rendered exception message.</param>
    /// <param name="errorType">Fully-qualified type name of the captured exception.</param>
    /// <param name="cancellationToken">Token to notify when the operation should be canceled.</param>
    /// <returns>A task that completes when the row has been finalized.</returns>
    Task FailAsync(
        string activityId,
        DateTime? createdOn,
        DateTime completedOn,
        string error,
        string errorType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the ids of <c>running</c> activity rows for one application that
    /// show no sign of life since <paramref name="staleBeforeUtc"/>.
    /// </summary>
    /// <param name="application">The owning application; every candidate row must match it exactly. Never null/blank.</param>
    /// <param name="environment">When non-blank, candidates must also match this environment exactly; when blank, the environment column is not filtered.</param>
    /// <param name="staleBeforeUtc">Cutoff: a row is stale when <c>COALESCE(last_heartbeat_on, started_on, created_on)</c> is strictly older than this UTC moment.</param>
    /// <param name="cancellationToken">Token to notify when the operation should be canceled.</param>
    /// <returns>The matching activity ids; empty when none are stale.</returns>
    /// <remarks>
    /// Read-only. The <c>COALESCE</c> chain protects a just-begun run that has
    /// not emitted its first heartbeat from being reported stale on the
    /// strength of an old — but absent — heartbeat. The query is always
    /// scoped to a single <paramref name="application"/> (and
    /// <paramref name="environment"/> when set); there is no cross-application
    /// surface.
    /// </remarks>
    Task<IReadOnlyList<string>> FindStaleAsync(
        string application,
        string? environment,
        DateTime staleBeforeUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Transitions the supplied <c>running</c> activity rows to a terminal
    /// status in a single set-based UPDATE, scoped to one application.
    /// </summary>
    /// <param name="activityIds">Ids to reap (typically the output of <see cref="FindStaleAsync"/>). A no-op when empty.</param>
    /// <param name="application">The owning application; included in the WHERE clause as a defense-in-depth guard so the UPDATE can never touch another application's row.</param>
    /// <param name="environment">When non-blank, also required to match in the WHERE clause.</param>
    /// <param name="terminalStatus">Terminal status to record (the reaper uses <see cref="LoggingActivityStatus.Canceled"/>).</param>
    /// <param name="reapedOnUtc">Explicit UTC value stamped into both <c>completed_on</c> and <c>last_modified_on</c> (the framework's UTC-override contract; never relies on the <c>ON UPDATE</c> trigger).</param>
    /// <param name="metricsJson">JSON written to the <c>metrics</c> column to mark the row as reaped; <c>error</c>/<c>error_type</c> are intentionally left untouched.</param>
    /// <param name="cancellationToken">Token to notify when the operation should be canceled.</param>
    /// <returns>A task that completes when the UPDATE has run.</returns>
    /// <remarks>
    /// The UPDATE re-asserts <c>status = 'running'</c> so a row that reached a
    /// terminal status between the find and the reap is left alone. The
    /// <c>activity</c> table is RANGE-partitioned on <c>created_on</c> and this
    /// sweep does not prune partitions; it relies on the
    /// <c>(application, status, created_on)</c> index and is expected to run
    /// infrequently.
    /// </remarks>
    Task ReapAsync(
        IReadOnlyList<string> activityIds,
        string application,
        string? environment,
        LoggingActivityStatus terminalStatus,
        DateTime reapedOnUtc,
        string? metricsJson,
        CancellationToken cancellationToken = default);

    #endregion Public Methods
}
