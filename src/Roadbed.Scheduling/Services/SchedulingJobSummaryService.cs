namespace Roadbed.Scheduling.Services;

using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Quartz;

/// <summary>
/// Service to create a summary of all scheduled jobs.
/// </summary>
internal static class SchedulingJobSummaryService
{
    #region Public Methods

    /// <summary>
    /// Creates a summary of all jobs that are scheduled.
    /// </summary>
    /// <param name="summary">Container for the summary.</param>
    /// <param name="scheduler">Quartz scheduler to query for active jobs.</param>
    /// <param name="cancellationToken">Token to notify when an operation should be canceled.</param>
    /// <returns>Total number of jobs found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when summary or scheduler is null.</exception>
    public static async Task<int> CreateJobScheduleSummaryAsync(
        StringBuilder summary,
        IScheduler scheduler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(summary);
        ArgumentNullException.ThrowIfNull(scheduler);

        summary.AppendLine("=== Scheduled Jobs Summary ===");

        // Get all job groups
        var jobGroupNames = await scheduler.GetJobGroupNames(cancellationToken);
        int totalJobs = 0;

        foreach (var groupName in jobGroupNames.OrderBy(g => g))
        {
            var jobKeys = await scheduler.GetJobKeys(
                Quartz.Impl.Matchers.GroupMatcher<JobKey>.GroupEquals(groupName),
                cancellationToken);

            foreach (var jobKey in jobKeys.OrderBy(k => k.Name))
            {
                totalJobs++;
                var jobDetail = await scheduler.GetJobDetail(jobKey, cancellationToken);
                var triggers = await scheduler.GetTriggersOfJob(jobKey, cancellationToken);

                summary.AppendLine($"[{totalJobs}] {jobKey.Name} (Group: {jobKey.Group})");

                if (!string.IsNullOrWhiteSpace(jobDetail?.Description))
                {
                    summary.AppendLine($"    Description: {jobDetail.Description}");
                }

                foreach (var trigger in triggers)
                {
                    var nextFireTime = trigger.GetNextFireTimeUtc();
                    var previousFireTime = trigger.GetPreviousFireTimeUtc();

                    summary.AppendLine($"    Trigger: {trigger.Key.Name}");
                    summary.AppendLine($"    State: {await scheduler.GetTriggerState(trigger.Key, cancellationToken)}");

                    if (nextFireTime.HasValue)
                    {
                        summary.AppendLine($"    Next Run: {nextFireTime.Value.ToLocalTime():yyyy-MM-dd HH:mm:ss}");
                    }

                    if (previousFireTime.HasValue)
                    {
                        summary.AppendLine($"    Last Run: {previousFireTime.Value.ToLocalTime():yyyy-MM-dd HH:mm:ss}");
                    }

                    summary.AppendLine($"    Priority: {trigger.Priority}");
                }

                summary.AppendLine();
            }
        }

        summary.AppendLine($"Total Jobs: {totalJobs}");
        summary.AppendLine("==============================");

        return totalJobs;
    }

    #endregion Public Methods
}