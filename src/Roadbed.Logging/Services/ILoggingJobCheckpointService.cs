namespace Roadbed.Logging;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Public surface for reading and advancing a checkpointed job's durable
/// position in the <c>job_checkpoint</c> table.
/// </summary>
/// <remarks>
/// <para>
/// Jobs normally reach this service through the extension methods in
/// <see cref="LoggingCheckpointedJobExtensions"/>. The interface is public so a
/// consumer's unit tests can register a fake in <c>ServiceLocator</c> and drive a
/// job through "never run", "won" and "lost" without a database.
/// </para>
/// <para>
/// The checkpoint is keyed by <c>LoggingOptions.Application</c>,
/// <c>LoggingOptions.Environment</c>, the job's <see cref="ILoggingCheckpointedJob.JobGroup"/>
/// and <see cref="ILoggingCheckpointedJob.Name"/>, and a checkpoint key.
/// </para>
/// <para>
/// The checkpoint shares no transaction with the job's own work: the logging
/// database is reached on its own connection. A job's work must therefore be
/// safe to repeat, because a crash between saving the work and advancing the
/// checkpoint repeats that work on the next run.
/// </para>
/// </remarks>
public interface ILoggingJobCheckpointService
{
    #region Public Methods

    /// <summary>
    /// Reads a job's checkpoint.
    /// </summary>
    /// <param name="job">The job that owns the checkpoint.</param>
    /// <param name="checkpointKey">The checkpoint's name within the job. Use <see cref="string.Empty"/> for a job's single checkpoint.</param>
    /// <param name="cancellationToken">Token to notify when the operation should be canceled.</param>
    /// <returns>The checkpoint, or <c>null</c> when none exists: the job has never run (or never recorded anything) in this application and environment.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the job identity or <paramref name="checkpointKey"/> is invalid.</exception>
    /// <exception cref="System.InvalidOperationException">Thrown when <c>LoggingOptions.Application</c> or <c>LoggingOptions.Environment</c> is blank or too long.</exception>
    /// <remarks>Any database failure propagates. A failed read is never reported as <c>null</c>.</remarks>
    Task<LoggingJobCheckpoint?> ReadAsync(
        ILoggingCheckpointedJob job,
        string checkpointKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records that a run started, without moving the position.
    /// </summary>
    /// <param name="job">The job that owns the checkpoint.</param>
    /// <param name="checkpointKey">The checkpoint's name within the job. Use <see cref="string.Empty"/> for a job's single checkpoint.</param>
    /// <param name="cancellationToken">Token to notify when the operation should be canceled.</param>
    /// <returns>A task that completes when the start has been recorded.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the job identity or <paramref name="checkpointKey"/> is invalid.</exception>
    /// <exception cref="System.InvalidOperationException">Thrown when called inside an ambient <c>System.Transactions</c> transaction, or when <c>LoggingOptions.Application</c> or <c>LoggingOptions.Environment</c> is blank or too long.</exception>
    /// <remarks>
    /// Sets <c>last_run_started_on</c> only, creating the row when none exists.
    /// It never changes the position, <c>version</c> or <c>advanced_on</c>, so a
    /// deferred pass can record itself without resetting a gate that reads
    /// <c>advanced_on</c>, and the run's own later advance still succeeds.
    /// </remarks>
    Task MarkRunStartedAsync(
        ILoggingCheckpointedJob job,
        string checkpointKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Advances a job's checkpoint from the version the caller read to a new
    /// position (compare-and-set).
    /// </summary>
    /// <param name="job">The job that owns the checkpoint.</param>
    /// <param name="checkpointKey">The checkpoint's name within the job. Use <see cref="string.Empty"/> for a job's single checkpoint.</param>
    /// <param name="expectedVersion">The <see cref="LoggingJobCheckpoint.Version"/> the caller read, or 0 when the read returned <c>null</c>.</param>
    /// <param name="position">The new position. All three cursor values are replaced; a value left <c>null</c> is stored as <c>null</c>.</param>
    /// <param name="fireInstanceId">The advancing run's Quartz fire instance id (<c>this.Context.FireInstanceId</c>), or <c>null</c>. At most 140 characters.</param>
    /// <param name="cancellationToken">Token to notify when the operation should be canceled.</param>
    /// <returns><c>true</c> when this call moved the position; <c>false</c> when another run had already advanced from <paramref name="expectedVersion"/>.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the job identity, <paramref name="checkpointKey"/>, <paramref name="position"/> or <paramref name="fireInstanceId"/> is invalid.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <paramref name="expectedVersion"/> is negative.</exception>
    /// <exception cref="System.InvalidOperationException">Thrown when called inside an ambient <c>System.Transactions</c> transaction, or when <c>LoggingOptions.Application</c> or <c>LoggingOptions.Environment</c> is blank or too long.</exception>
    /// <remarks>
    /// <para>
    /// Call this only after the work the new position covers has been saved.
    /// A crash then costs a repeat, never a gap. Never call it inside the
    /// transaction that saves the work: the advance would commit on its own
    /// connection ahead of that work, so it throws there instead.
    /// </para>
    /// <para>
    /// On <c>false</c>, do not retry with the same expected version: read the
    /// checkpoint again and decide from the new position.
    /// </para>
    /// <para>
    /// When the database reports no change, the service re-reads the row. If it
    /// shows the next version, this <paramref name="fireInstanceId"/> and this
    /// position, an earlier attempt's commit landed before its acknowledgement
    /// was lost, and the call returns <c>true</c>. Without a fire instance id
    /// that case returns <c>false</c>.
    /// </para>
    /// </remarks>
    Task<bool> TryAdvanceAsync(
        ILoggingCheckpointedJob job,
        string checkpointKey,
        long expectedVersion,
        LoggingJobCheckpointPosition position,
        string? fireInstanceId,
        CancellationToken cancellationToken = default);

    #endregion Public Methods
}
