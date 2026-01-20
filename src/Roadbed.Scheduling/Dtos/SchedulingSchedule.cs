/*
 * The namespace Roadbed.Scheduling.Entities was removed on purpose and replaced with Roadbed.Crud so that no additional using statements are required.
 */
namespace Roadbed.Scheduling;

using System;

/// <summary>
/// Configuration for scheduling a job's execution.
/// </summary>
/// <remarks>
/// Provides flexible scheduling options including cron expressions, simple intervals, and calendar-based schedules.
/// This class replaces the need for Quartz.NET attributes by allowing schedule configuration through constructor injection.
/// </remarks>
public sealed class SchedulingSchedule
{
    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="SchedulingSchedule"/> class with a cron expression.
    /// </summary>
    /// <param name="cronExpression">Cron expression defining when the job should execute.</param>
    /// <exception cref="ArgumentException">Thrown when cronExpression is null or whitespace.</exception>
    /// <example>
    /// <code>
    /// // Run every day at 2:30 AM
    /// var schedule = new SchedulingSchedule("0 30 2 * * ?");
    /// </code>
    /// </example>
    public SchedulingSchedule(string cronExpression)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cronExpression);
        this.CronExpression = cronExpression;
        this.ScheduleType = SchedulingScheduleType.Cron;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SchedulingSchedule"/> class with a simple interval.
    /// </summary>
    /// <param name="interval">Time interval between job executions.</param>
    /// <param name="startDelay">Optional delay before the first execution. Defaults to zero.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when interval is zero or negative, or when startDelay is negative.</exception>
    /// <example>
    /// <code>
    /// // Run every 5 minutes, starting immediately
    /// var schedule = new SchedulingSchedule(TimeSpan.FromMinutes(5));
    ///
    /// // Run every hour, starting after 10 minutes
    /// var schedule = new SchedulingSchedule(TimeSpan.FromHours(1), TimeSpan.FromMinutes(10));
    /// </code>
    /// </example>
    public SchedulingSchedule(TimeSpan interval, TimeSpan? startDelay = null)
    {
        if (interval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(interval), "Interval must be greater than zero.");
        }

        if (startDelay.HasValue && startDelay.Value < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(startDelay), "Start delay cannot be negative.");
        }

        this.Interval = interval;
        this.StartDelay = startDelay ?? TimeSpan.Zero;
        this.ScheduleType = SchedulingScheduleType.SimpleInterval;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SchedulingSchedule"/> class with a specific start time and optional interval.
    /// </summary>
    /// <param name="startAt">The specific date and time when the job should start.</param>
    /// <param name="interval">Optional interval for repeating executions. If null, job runs once.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when interval is zero or negative.</exception>
    /// <example>
    /// <code>
    /// // Run once at a specific time
    /// var schedule = new SchedulingSchedule(new DateTime(2026, 1, 20, 14, 30, 0));
    ///
    /// // Run every 30 minutes starting at a specific time
    /// var schedule = new SchedulingSchedule(
    ///     new DateTime(2026, 1, 20, 14, 30, 0),
    ///     TimeSpan.FromMinutes(30));
    /// </code>
    /// </example>
    public SchedulingSchedule(DateTime startAt, TimeSpan? interval = null)
    {
        if (interval.HasValue && interval.Value <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(interval), "Interval must be greater than zero.");
        }

        this.StartAt = startAt;
        this.Interval = interval;
        this.ScheduleType = interval.HasValue
            ? SchedulingScheduleType.SpecificTimeWithInterval
            : SchedulingScheduleType.SpecificTimeOnce;
    }

    #endregion Public Constructors

    #region Public Properties

    /// <summary>
    /// Gets the cron expression for the schedule.
    /// </summary>
    /// <remarks>
    /// Only populated when ScheduleType is Cron.
    /// </remarks>
    public string? CronExpression { get; }

    /// <summary>
    /// Gets the interval between job executions.
    /// </summary>
    /// <remarks>
    /// Populated for SimpleInterval, SpecificTimeWithInterval schedule types.
    /// </remarks>
    public TimeSpan? Interval { get; }

    /// <summary>
    /// Gets or sets the maximum number of times the job should execute.
    /// </summary>
    /// <remarks>
    /// If null, the job repeats indefinitely. If set to a positive number, the job stops after that many executions.
    /// Only applicable to repeating schedules (SimpleInterval, SpecificTimeWithInterval, Cron).
    /// </remarks>
    public int? MaxExecutionCount { get; set; }

    /// <summary>
    /// Gets or sets the priority of the job.
    /// </summary>
    /// <remarks>
    /// Higher priority jobs are executed before lower priority jobs when multiple jobs are ready to run.
    /// Default is Normal.
    /// </remarks>
    public SchedulingJobPriority Priority { get; set; } = SchedulingJobPriority.Normal;

    /// <summary>
    /// Gets the type of schedule being used.
    /// </summary>
    public SchedulingScheduleType ScheduleType { get; }

    /// <summary>
    /// Gets the specific date and time when the job should start.
    /// </summary>
    /// <remarks>
    /// Populated for SpecificTimeOnce and SpecificTimeWithInterval schedule types.
    /// </remarks>
    public DateTime? StartAt { get; }

    /// <summary>
    /// Gets the delay before the first execution when using simple intervals.
    /// </summary>
    /// <remarks>
    /// Only populated when ScheduleType is SimpleInterval.
    /// </remarks>
    public TimeSpan? StartDelay { get; }

    /// <summary>
    /// Gets or sets the time zone for the schedule.
    /// </summary>
    /// <remarks>
    /// Defaults to UTC. Particularly important for cron expressions and specific start times.
    /// </remarks>
    public TimeZoneInfo TimeZone { get; set; } = TimeZoneInfo.Utc;

    /// <summary>
    /// Gets the group name for the schedule.
    /// </summary>
    public string GroupName { get; init; } = "Default";

    /// <summary>
    /// Gets or sets a value indicating whether misfire handling is enabled.
    /// </summary>
    public bool MisfireHandlingEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the group name for the schedule.
    /// </summary>
    public SchedulingMisfireStrategy MisfireStrategy { get; set; } = SchedulingMisfireStrategy.Default;

    #endregion Public Properties
}