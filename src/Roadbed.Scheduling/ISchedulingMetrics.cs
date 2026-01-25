namespace Roadbed.Scheduling;

using System;

/// <summary>
/// Contract for tracking scheduled job execution metrics.
/// </summary>
/// <remarks>
/// Implementations of this interface receive notifications about job lifecycle events
/// and can record metrics, log to external systems, or perform other monitoring tasks.
/// All methods are called synchronously by the metrics listener. If async operations
/// are needed, implementations should queue work or use fire-and-forget patterns.
/// Implementations must be thread-safe as they may be called concurrently.
/// Any exceptions thrown by implementations are caught and logged as warnings,
/// ensuring metrics failures never break job execution.
/// </remarks>
public interface ISchedulingMetrics
{
    #region Public Methods

    /// <summary>
    /// Called when a job completes successfully.
    /// </summary>
    /// <param name="info">Job execution information including ResultMessage if set by the job.</param>
    /// <param name="duration">Execution duration measured from job start to completion.</param>
    /// <remarks>
    /// The info parameter includes the ResultMessage property which contains the value
    /// set by the job via Context.Result (if any). This allows jobs to provide execution
    /// summaries such as "Processed 1,234 records" or "Deleted 56 files, freed 2.3 GB".
    /// </remarks>
    void JobCompleted(JobExecutionInfo info, TimeSpan duration);

    /// <summary>
    /// Called when a job fails with an exception.
    /// </summary>
    /// <param name="info">Job execution information including ResultMessage if set by the job before failure.</param>
    /// <param name="exception">The exception that caused the job to fail.</param>
    /// <param name="duration">Execution duration measured from job start to failure.</param>
    /// <remarks>
    /// The info parameter may include a ResultMessage if the job set Context.Result
    /// before the exception was thrown. This allows capturing partial results such as
    /// "Processed 500 records before failure" for diagnostic purposes.
    /// </remarks>
    void JobFailed(JobExecutionInfo info, Exception exception, TimeSpan duration);

    /// <summary>
    /// Called when a job trigger misfires.
    /// </summary>
    /// <param name="info">Job execution information for the misfired trigger.</param>
    /// <remarks>
    /// A misfire occurs when a scheduled job execution is missed, typically due to:
    /// - System downtime during scheduled execution time
    /// - Thread pool exhaustion preventing job start
    /// - Job still running from previous execution
    /// The info parameter will have null values for timing properties (FireTimeUtc, etc.)
    /// since the job did not actually execute.
    /// </remarks>
    void JobMisfired(JobExecutionInfo info);

    /// <summary>
    /// Called when a job starts executing.
    /// </summary>
    /// <param name="info">Job execution information at the start of execution.</param>
    /// <remarks>
    /// This is called immediately before the job's ExecuteAsync method is invoked.
    /// The info parameter will not include ResultMessage as the job has not yet executed.
    /// Use this to track job start times, concurrent executions, or trigger metrics collection.
    /// </remarks>
    void JobStarted(JobExecutionInfo info);

    #endregion Public Methods
}