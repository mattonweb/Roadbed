namespace Roadbed.Logging;

using System;
using System.Diagnostics;

/// <summary>
/// Disposable handle bundling the diagnostic <see cref="Activity"/> and the
/// <c>Microsoft.Extensions.Logging</c> scope that a
/// <c>LoggingActivityService.BeginAsync</c> call opened.
/// </summary>
/// <remarks>
/// <para>
/// Disposing the scope stops the <see cref="Activity"/> and pops the MEL
/// scope frame. It deliberately does <strong>not</strong> mark the activity
/// row terminal in the database — callers must explicitly invoke
/// <c>CompleteAsync</c> or <c>FailAsync</c> to record the outcome, because
/// the row's terminal status is information dispose-time cannot recover
/// (Succeeded vs. Canceled vs. Skipped).
/// </para>
/// <para>
/// Holds no managed resources other than the two disposables passed in; both
/// are nullable to make the type usable in test paths that do not exercise
/// the full activity pipeline.
/// </para>
/// </remarks>
public sealed class LoggingActivityScope : IDisposable
{
    #region Private Fields

    private readonly Activity? _activity;
    private readonly IDisposable? _logScope;
    private bool _disposed;

    #endregion Private Fields

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingActivityScope"/> class.
    /// </summary>
    /// <param name="activityId">Caller-supplied ULID of the underlying activity row.</param>
    /// <param name="createdOn">UTC timestamp persisted in the row's <c>created_on</c> column. Captured here so subsequent update calls can include it in the WHERE clause and let MySQL prune to the one monthly partition that owns the row.</param>
    /// <param name="activity">The diagnostic <see cref="Activity"/> the service started, when one was created.</param>
    /// <param name="logScope">The MEL scope handle the service opened, when one was created.</param>
    public LoggingActivityScope(
        string activityId,
        DateTime createdOn,
        Activity? activity,
        IDisposable? logScope)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(activityId);

        this.ActivityId = activityId;
        this.CreatedOn = createdOn;
        this._activity = activity;
        this._logScope = logScope;
    }

    #endregion Public Constructors

    #region Public Properties

    /// <summary>
    /// Gets the caller-supplied ULID of the underlying activity row.
    /// </summary>
    public string ActivityId { get; }

    /// <summary>
    /// Gets the UTC timestamp persisted in the row's <c>created_on</c>
    /// column.
    /// </summary>
    /// <remarks>
    /// The activity table is RANGE-partitioned monthly on
    /// <c>TO_DAYS(created_on)</c>. Update calls that include
    /// <c>created_on</c> in the WHERE clause prune to the single
    /// partition that holds the row; calls that pass only the
    /// <see cref="ActivityId"/> probe every partition.
    /// </remarks>
    public DateTime CreatedOn { get; }

    /// <summary>
    /// Gets the OpenTelemetry-compatible trace identifier active inside the scope,
    /// or <c>null</c> when no <see cref="Activity"/> was started.
    /// </summary>
    public string? TraceId => this._activity?.TraceId.ToHexString();

    /// <summary>
    /// Gets the OpenTelemetry-compatible span identifier active inside the scope,
    /// or <c>null</c> when no <see cref="Activity"/> was started.
    /// </summary>
    public string? SpanId => this._activity?.SpanId.ToHexString();

    #endregion Public Properties

    #region Public Methods

    /// <inheritdoc/>
    public void Dispose()
    {
        if (this._disposed)
        {
            return;
        }

        this._disposed = true;

        // Pop the MEL scope first so subsequent log lines do not get the activity_id stamped,
        // then stop the System.Diagnostics.Activity so Activity.Current reverts.
        this._logScope?.Dispose();
        this._activity?.Dispose();
    }

    #endregion Public Methods
}
