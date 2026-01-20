namespace Roadbed.Scheduling;

/// <summary>
/// Base contract for all scheduled jobs.
/// </summary>
public interface ISchedulingJob
{
    #region Public Properties

    /// <summary>
    /// Gets the description for the schedule.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the job name for the schedule.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the schedule configuration for this job.
    /// </summary>
    SchedulingSchedule Schedule { get; }

    #endregion Public Properties

    #region Public Methods

    /// <summary>
    /// Executes the job.
    /// </summary>
    /// <param name="cancellationToken">Token to notify when an operation should be canceled.</param>
    /// <returns>A task that represents the asynchronous save operation.</returns>
    Task ExecuteAsync(CancellationToken cancellationToken);

    #endregion Public Methods
}