namespace Roadbed.Logging;

using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Data-access contract for the <c>job_checkpoint</c> control-state table.
/// </summary>
/// <remarks>
/// Every member throws on any database failure. There is deliberately no
/// delete: checkpoint rows are never removed by Roadbed.
/// </remarks>
internal interface ILoggingJobCheckpointRepository
{
    #region Public Methods

    /// <summary>
    /// Reads one checkpoint row.
    /// </summary>
    /// <param name="key">The row's primary key.</param>
    /// <param name="cancellationToken">Token to notify when the operation should be canceled.</param>
    /// <returns>The row, or <c>null</c> when no row exists for <paramref name="key"/>.</returns>
    Task<LoggingJobCheckpoint?> ReadAsync(
        LoggingJobCheckpointKey key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts an empty checkpoint row (version 0, no position) when none exists;
    /// leaves an existing row untouched.
    /// </summary>
    /// <param name="key">The row's primary key.</param>
    /// <param name="nowUtc">UTC stamp for <c>created_on</c> / <c>last_modified_on</c> of a newly inserted row.</param>
    /// <param name="cancellationToken">Token to notify when the operation should be canceled.</param>
    /// <returns>A task that completes when the row is known to exist.</returns>
    Task EnsureExistsAsync(
        LoggingJobCheckpointKey key,
        DateTime nowUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets <c>last_run_started_on</c>, inserting the row first when none exists.
    /// Never touches the position, <c>version</c> or <c>advanced_on</c>.
    /// </summary>
    /// <param name="key">The row's primary key.</param>
    /// <param name="nowUtc">UTC stamp written to <c>last_run_started_on</c> and <c>last_modified_on</c>.</param>
    /// <param name="cancellationToken">Token to notify when the operation should be canceled.</param>
    /// <returns>A task that completes when the start has been recorded.</returns>
    Task MarkRunStartedAsync(
        LoggingJobCheckpointKey key,
        DateTime nowUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves the position from the version the caller read to the next version
    /// (compare-and-set). The row must already exist.
    /// </summary>
    /// <param name="key">The row's primary key.</param>
    /// <param name="expectedVersion">The <c>version</c> the caller read; the UPDATE matches only that version.</param>
    /// <param name="position">The new position. All three cursor values are replaced.</param>
    /// <param name="fireInstanceId">Quartz fire instance id of the advancing run, or <c>null</c>.</param>
    /// <param name="nowUtc">UTC stamp written to <c>advanced_on</c> and <c>last_modified_on</c>.</param>
    /// <param name="cancellationToken">Token to notify when the operation should be canceled.</param>
    /// <returns>The number of rows the UPDATE changed: 1 when the advance won, 0 when the version had moved.</returns>
    Task<int> AdvanceAsync(
        LoggingJobCheckpointKey key,
        long expectedVersion,
        LoggingJobCheckpointPosition position,
        string? fireInstanceId,
        DateTime nowUtc,
        CancellationToken cancellationToken = default);

    #endregion Public Methods
}
