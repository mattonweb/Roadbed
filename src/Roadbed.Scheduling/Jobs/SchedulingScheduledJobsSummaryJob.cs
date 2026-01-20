namespace Roadbed.Scheduling.Jobs;

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Quartz;
using Roadbed.Scheduling.Services;

/// <summary>
/// Scheduled job that logs information about all actively scheduled jobs.
/// </summary>
public class SchedulingScheduledJobsSummaryJob
    : BaseSchedulingJob<SchedulingScheduledJobsSummaryJob>
{
    #region Private Fields

    private readonly IScheduler _scheduler;

    #endregion Private Fields

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="SchedulingScheduledJobsSummaryJob"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <param name="scheduler">Quartz scheduler to query for active jobs.</param>
    /// <exception cref="ArgumentNullException">Thrown when logger or scheduler is null.</exception>
    public SchedulingScheduledJobsSummaryJob(
        ILogger<SchedulingScheduledJobsSummaryJob> logger,
        IScheduler scheduler)
        : base(
            name: "ScheduledJobsSummary",
            description: "Logs a daily summary of all actively scheduled jobs in the system",
            schedule: new SchedulingSchedule("0 0 8 * * ?") // Daily at 8:00 AM
            {
                TimeZone = TimeZoneInfo.Local,
                GroupName = "System",
                Priority = SchedulingJobPriority.Lowest,
            },
            logger: logger)
    {
        ArgumentNullException.ThrowIfNull(scheduler);
        this._scheduler = scheduler;
    }

    #endregion Public Constructors

    #region Public Methods

    /// <inheritdoc/>
    public override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        this.LogDebug("Starting scheduled jobs summary");

        StringBuilder summary = new StringBuilder();

        int totalJobs = await SchedulingJobSummaryService.CreateJobScheduleSummaryAsync(
            summary,
            this._scheduler,
            cancellationToken);

        this.LogInformation("{JobsSummary}", summary.ToString());
        this.LogDebug("Completed scheduled jobs summary. Total jobs: {TotalJobs}", totalJobs);
    }

    #endregion Public Methods
}