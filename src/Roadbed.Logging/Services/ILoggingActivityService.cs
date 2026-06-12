namespace Roadbed.Logging;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Public surface for managing the lifecycle of a row in the <c>activity</c>
/// table.
/// </summary>
/// <remarks>
/// Implementations open a diagnostic <c>Activity</c> and an MEL logger scope
/// for every <see cref="BeginAsync"/> call, so subsequent <c>ILogger</c>
/// usage automatically inherits the activity's <c>activity_id</c>,
/// <c>trace_id</c>, and <c>span_id</c> on every emitted log row.
/// </remarks>
internal interface ILoggingActivityService
{
    #region Public Methods

    /// <summary>
    /// Inserts a new <c>activity</c> row in the <see cref="LoggingActivityStatus.Running"/>
    /// state and opens an ambient scope that subsequent log lines inherit.
    /// </summary>
    /// <param name="request">Initial values for the new activity row. The caller supplies the ULID identifier.</param>
    /// <param name="cancellationToken">Token to notify when the operation should be canceled.</param>
    /// <returns>A disposable handle whose lifetime defines the ambient scope. Dispose to pop the scope; call <c>CompleteAsync</c> or <c>FailAsync</c> to mark the row terminal.</returns>
    Task<LoggingActivityScope> BeginAsync(
        LoggingActivityBeginRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stamps <c>UtcNow</c> into the <c>last_heartbeat_on</c> column of the
    /// activity row identified by <paramref name="scope"/>.
    /// </summary>
    /// <param name="scope">Scope returned by <see cref="BeginAsync"/>. Carries the row's <c>created_on</c> so the UPDATE prunes to one MySQL partition.</param>
    /// <param name="cancellationToken">Token to notify when the operation should be canceled.</param>
    /// <returns>A task that completes when the heartbeat has been recorded.</returns>
    Task HeartbeatAsync(
        LoggingActivityScope scope,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stamps <c>UtcNow</c> into the <c>last_heartbeat_on</c> column of an
    /// existing activity row, identified by id only.
    /// </summary>
    /// <param name="activityId">Identifier of the activity row to update.</param>
    /// <param name="cancellationToken">Token to notify when the operation should be canceled.</param>
    /// <returns>A task that completes when the heartbeat has been recorded.</returns>
    /// <remarks>
    /// Prefer the <see cref="HeartbeatAsync(LoggingActivityScope, CancellationToken)"/>
    /// overload when the caller still holds the scope — this id-only path
    /// causes MySQL to probe every defined monthly partition (about 120)
    /// rather than pruning to the single partition that owns the row.
    /// </remarks>
    Task HeartbeatAsync(
        string activityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Patches the supplied non-<c>null</c> "current state" fields onto an
    /// existing activity row.
    /// </summary>
    /// <param name="request">The patch request. Null properties preserve their existing values. Setting <see cref="LoggingActivityUpdateRequest.CreatedOn"/> enables partition pruning.</param>
    /// <param name="cancellationToken">Token to notify when the operation should be canceled.</param>
    /// <returns>A task that completes when the patch has been applied.</returns>
    Task UpdateAsync(
        LoggingActivityUpdateRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the activity row terminal with the supplied
    /// non-<see cref="LoggingActivityStatus.Failed"/> status.
    /// </summary>
    /// <param name="scope">Scope returned by <see cref="BeginAsync"/>. Carries the row's <c>created_on</c> so the UPDATE prunes to one MySQL partition.</param>
    /// <param name="status">Terminal status to record. Use <see cref="FailAsync(LoggingActivityScope, Exception, CancellationToken)"/> for exception-driven failures.</param>
    /// <param name="recordsImpacted">Optional headline count of records produced or affected during the run.</param>
    /// <param name="metricsJson">Optional metrics JSON to persist alongside the terminal status.</param>
    /// <param name="cancellationToken">Token to notify when the operation should be canceled.</param>
    /// <returns>A task that completes when the row has been finalized.</returns>
    Task CompleteAsync(
        LoggingActivityScope scope,
        LoggingActivityStatus status,
        long? recordsImpacted = null,
        string? metricsJson = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the activity row terminal, identified by id only.
    /// </summary>
    /// <param name="activityId">Identifier of the activity row to finalize.</param>
    /// <param name="status">Terminal status to record.</param>
    /// <param name="recordsImpacted">Optional headline count of records produced or affected during the run.</param>
    /// <param name="metricsJson">Optional metrics JSON to persist alongside the terminal status.</param>
    /// <param name="cancellationToken">Token to notify when the operation should be canceled.</param>
    /// <returns>A task that completes when the row has been finalized.</returns>
    /// <remarks>
    /// Prefer the <see cref="CompleteAsync(LoggingActivityScope, LoggingActivityStatus, long?, string?, CancellationToken)"/>
    /// overload when the scope is available — this id-only path probes
    /// every monthly partition on MySQL.
    /// </remarks>
    Task CompleteAsync(
        string activityId,
        LoggingActivityStatus status,
        long? recordsImpacted = null,
        string? metricsJson = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the activity row as <see cref="LoggingActivityStatus.Failed"/>
    /// and records the captured exception.
    /// </summary>
    /// <param name="scope">Scope returned by <see cref="BeginAsync"/>. Carries the row's <c>created_on</c> so the UPDATE prunes to one MySQL partition.</param>
    /// <param name="error">The exception that ended the activity.</param>
    /// <param name="cancellationToken">Token to notify when the operation should be canceled.</param>
    /// <returns>A task that completes when the row has been finalized.</returns>
    Task FailAsync(
        LoggingActivityScope scope,
        Exception error,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the activity row as <see cref="LoggingActivityStatus.Failed"/>,
    /// identified by id only.
    /// </summary>
    /// <param name="activityId">Identifier of the activity row to finalize.</param>
    /// <param name="error">The exception that ended the activity.</param>
    /// <param name="cancellationToken">Token to notify when the operation should be canceled.</param>
    /// <returns>A task that completes when the row has been finalized.</returns>
    /// <remarks>
    /// Prefer the <see cref="FailAsync(LoggingActivityScope, Exception, CancellationToken)"/>
    /// overload when the scope is available — this id-only path probes
    /// every monthly partition on MySQL.
    /// </remarks>
    Task FailAsync(
        string activityId,
        Exception error,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts a lineage edge into <c>activity_input</c> linking the
    /// supplied activity to one of its upstream inputs.
    /// </summary>
    /// <param name="activityId">The consuming activity's identifier.</param>
    /// <param name="inputActivityId">The upstream input activity's identifier.</param>
    /// <param name="inputRole">Optional free-form role describing the consumed input (e.g. <c>"places"</c>, <c>"cousubs"</c>).</param>
    /// <param name="cancellationToken">Token to notify when the operation should be canceled.</param>
    /// <returns>A task that completes when the edge has been inserted.</returns>
    Task AddInputAsync(
        string activityId,
        string inputActivityId,
        string? inputRole = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reaps this application's stale <c>running</c> activities — rows orphaned
    /// because the owning process died without writing a terminal status —
    /// transitioning each to <see cref="LoggingActivityStatus.Canceled"/>.
    /// </summary>
    /// <param name="staleAfter">A run is stale when its last sign of life (<c>COALESCE(last_heartbeat_on, started_on, created_on)</c>) is older than this span.</param>
    /// <param name="reason">Optional free-text reason recorded in the row's <c>metrics</c> JSON so reaped rows are distinguishable from app-initiated cancellations.</param>
    /// <param name="cancellationToken">Token to notify when the operation should be canceled.</param>
    /// <returns>The ids of the activities that were reaped (empty when none were stale), so the caller can log them.</returns>
    /// <remarks>
    /// Strictly scoped to the configured <see cref="LoggingOptions.Application"/>
    /// (and <see cref="LoggingOptions.Environment"/> when set). It never reads
    /// or writes another application's activities, and never finalizes a run
    /// as <see cref="LoggingActivityStatus.Succeeded"/> or
    /// <see cref="LoggingActivityStatus.Failed"/>. Liveness is judged by
    /// heartbeat staleness only, so an instance dead on any host is reapable;
    /// invocation cadence (startup sweep, scheduled job) is the caller's choice.
    /// </remarks>
    Task<IReadOnlyList<string>> ReapStaleActivitiesAsync(
        TimeSpan staleAfter,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Read-only counterpart to
    /// <see cref="ReapStaleActivitiesAsync"/>: returns the ids this
    /// application <em>would</em> reap, without modifying any row.
    /// </summary>
    /// <param name="staleAfter">Same staleness definition as the reaper.</param>
    /// <param name="cancellationToken">Token to notify when the operation should be canceled.</param>
    /// <returns>The ids of this application's stale <c>running</c> activities.</returns>
    Task<IReadOnlyList<string>> FindStaleActivitiesAsync(
        TimeSpan staleAfter,
        CancellationToken cancellationToken = default);

    #endregion Public Methods
}
