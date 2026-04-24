namespace Roadbed.Scheduling;

using Quartz;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Base contract for all scheduled jobs.
/// </summary>
/// <remarks>
/// This interface extends Quartz.IJob to enable proper integration with the Quartz.NET scheduler.
/// Jobs implementing this interface can be automatically discovered and registered by the
/// scheduling framework. The Execute method from IJob is implemented explicitly in BaseSchedulingJob,
/// while ExecuteAsync provides the user-facing method signature for job implementations.
/// </remarks>
public interface ISchedulingJob : IJob
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

    /// <summary>
    /// Gets a value indicating whether the job should be registered with Quartz.
    /// </summary>
    /// <remarks>
    /// When <see langword="false"/>, <see cref="Installers.InstallScheduling"/>
    /// skips both the job registration and its trigger — the job is entirely
    /// absent from the scheduler. Defaults to <see langword="true"/>.
    /// </remarks>
    bool IsEnabled => true;

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