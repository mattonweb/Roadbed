/*
 * The namespace Roadbed.Scheduling.Enumerators was removed on purpose and replaced with Roadbed.Crud so that no additional using statements are required.
 */
namespace Roadbed.Scheduling;

/// <summary>
/// Defines the type of scheduling mechanism being used.
/// </summary>
public enum SchedulingScheduleType
{
    /// <summary>
    /// Schedule is defined by a cron expression.
    /// </summary>
    Cron = 0,

    /// <summary>
    /// Schedule repeats at a simple fixed interval.
    /// </summary>
    SimpleInterval = 1,

    /// <summary>
    /// Schedule runs once at a specific date and time.
    /// </summary>
    SpecificTimeOnce = 2,

    /// <summary>
    /// Schedule starts at a specific date and time, then repeats at an interval.
    /// </summary>
    SpecificTimeWithInterval = 3,

    /// <summary>
    /// Job has no automatic schedule and must be triggered programmatically.
    /// </summary>
    /// <remarks>
    /// Jobs with this schedule type are registered as durable in Quartz (persisted without a trigger).
    /// They can be executed on demand via <c>ISchedulerFactory</c> by calling
    /// <c>scheduler.TriggerJob(new JobKey(name, groupName))</c>.
    /// </remarks>
    ManualOnly = 4,
}