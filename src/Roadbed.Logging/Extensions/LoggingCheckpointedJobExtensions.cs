namespace Roadbed.Logging;

using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// The checkpoint operations a job that implements
/// <see cref="ILoggingCheckpointedJob"/> calls on itself.
/// </summary>
/// <remarks>
/// <para>
/// Each method resolves <see cref="ILoggingJobCheckpointService"/> from
/// <see cref="ServiceLocator"/> at call time, never at job construction:
/// Roadbed.Scheduling constructs jobs while DI is still being configured,
/// possibly before Roadbed.Logging's installer has run. Call these only from
/// inside the job's <c>ExecuteAsync</c>.
/// </para>
/// <para>
/// A typical run reads the checkpoint, records the start, does and saves its
/// work, then advances from the version it read:
/// </para>
/// <code>
/// LoggingJobCheckpoint? checkpoint = await this.ReadCheckpointAsync("criteria-rematch", cancellationToken);
/// await this.MarkCheckpointRunStartedAsync("criteria-rematch", cancellationToken);
/// // ... do the work and save it ...
/// bool won = await this.TryAdvanceCheckpointAsync(
///     "criteria-rematch",
///     checkpoint?.Version ?? 0,
///     new LoggingJobCheckpointPosition { Id = maxAuditId },
///     this.Context.FireInstanceId,
///     cancellationToken);
/// </code>
/// </remarks>
public static class LoggingCheckpointedJobExtensions
{
    #region Public Methods

    /// <summary>
    /// Reads this job's checkpoint.
    /// </summary>
    /// <param name="job">The job that owns the checkpoint.</param>
    /// <param name="checkpointKey">The checkpoint's name within the job. Use <see cref="string.Empty"/> for a job's single checkpoint.</param>
    /// <param name="cancellationToken">Token to notify when the operation should be canceled.</param>
    /// <returns>The checkpoint, or <c>null</c> when the job has never recorded one in this application and environment.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="job"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no <see cref="ILoggingJobCheckpointService"/> is registered.</exception>
    /// <remarks>See <see cref="ILoggingJobCheckpointService.ReadAsync"/>.</remarks>
    public static Task<LoggingJobCheckpoint?> ReadCheckpointAsync(
        this ILoggingCheckpointedJob job,
        string checkpointKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);

        return ResolveService().ReadAsync(job, checkpointKey, cancellationToken);
    }

    /// <summary>
    /// Records that this run started, without moving the position.
    /// </summary>
    /// <param name="job">The job that owns the checkpoint.</param>
    /// <param name="checkpointKey">The checkpoint's name within the job. Use <see cref="string.Empty"/> for a job's single checkpoint.</param>
    /// <param name="cancellationToken">Token to notify when the operation should be canceled.</param>
    /// <returns>A task that completes when the start has been recorded.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="job"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no <see cref="ILoggingJobCheckpointService"/> is registered.</exception>
    /// <remarks>See <see cref="ILoggingJobCheckpointService.MarkRunStartedAsync"/>.</remarks>
    public static Task MarkCheckpointRunStartedAsync(
        this ILoggingCheckpointedJob job,
        string checkpointKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);

        return ResolveService().MarkRunStartedAsync(job, checkpointKey, cancellationToken);
    }

    /// <summary>
    /// Advances this job's checkpoint from the version it read (compare-and-set).
    /// Call only after the work the new position covers has been saved.
    /// </summary>
    /// <param name="job">The job that owns the checkpoint.</param>
    /// <param name="checkpointKey">The checkpoint's name within the job. Use <see cref="string.Empty"/> for a job's single checkpoint.</param>
    /// <param name="expectedVersion">The <see cref="LoggingJobCheckpoint.Version"/> the job read, or 0 when the read returned <c>null</c>.</param>
    /// <param name="position">The new position.</param>
    /// <param name="fireInstanceId">The run's Quartz fire instance id (<c>this.Context.FireInstanceId</c>), or <c>null</c>.</param>
    /// <param name="cancellationToken">Token to notify when the operation should be canceled.</param>
    /// <returns><c>true</c> when this call moved the position; <c>false</c> when another run had already advanced it.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="job"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no <see cref="ILoggingJobCheckpointService"/> is registered.</exception>
    /// <remarks>See <see cref="ILoggingJobCheckpointService.TryAdvanceAsync"/>.</remarks>
    public static Task<bool> TryAdvanceCheckpointAsync(
        this ILoggingCheckpointedJob job,
        string checkpointKey,
        long expectedVersion,
        LoggingJobCheckpointPosition position,
        string? fireInstanceId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);

        return ResolveService().TryAdvanceAsync(
            job,
            checkpointKey,
            expectedVersion,
            position,
            fireInstanceId,
            cancellationToken);
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Resolves the checkpoint service from <see cref="ServiceLocator"/>.
    /// </summary>
    /// <returns>The registered checkpoint service.</returns>
    /// <exception cref="InvalidOperationException">Thrown when none is registered.</exception>
    private static ILoggingJobCheckpointService ResolveService()
    {
        return ServiceLocator.TryGetService<ILoggingJobCheckpointService>()
            ?? throw new InvalidOperationException(
                $"No {nameof(ILoggingJobCheckpointService)} is registered. Job checkpoints need Roadbed.Logging wired " +
                "through a provider package (Roadbed.Logging.MySql or Roadbed.Logging.Sqlite), the job_checkpoint table " +
                "installed, and a LoggingOptions with a non-blank Application and Environment.");
    }

    #endregion Private Methods
}
