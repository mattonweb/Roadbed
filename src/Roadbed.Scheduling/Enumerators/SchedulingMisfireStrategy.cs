/*
 * The namespace Roadbed.Scheduling.Enumerators was removed on purpose and replaced with Roadbed.Crud so that no additional using statements are required.
 */
namespace Roadbed.Scheduling;

/// <summary>
/// Defines strategies for handling misfired job executions.
/// </summary>
public enum SchedulingMisfireStrategy
{
    /// <summary>
    /// Use Quartz.NET default misfire handling.
    /// </summary>
    Default = 0,

    /// <summary>
    /// Do nothing - missed executions are skipped.
    /// </summary>
    DoNothing = 1,

    /// <summary>
    /// Fire once and proceed with the schedule (cron only).
    /// </summary>
    FireAndProceed = 2,

    /// <summary>
    /// Ignore misfires and fire immediately.
    /// </summary>
    IgnoreMisfires = 3,

    /// <summary>
    /// Fire immediately (simple schedules).
    /// </summary>
    FireNow = 4,

    /// <summary>
    /// Reschedule to next fire time with existing repeat count (simple schedules).
    /// </summary>
    NextWithExistingCount = 5,

    /// <summary>
    /// Reschedule to next fire time with remaining repeat count (simple schedules).
    /// </summary>
    NextWithRemainingCount = 6,

    /// <summary>
    /// Fire now with existing repeat count (simple schedules).
    /// </summary>
    NowWithExistingCount = 7,

    /// <summary>
    /// Fire now with remaining repeat count (simple schedules).
    /// </summary>
    NowWithRemainingCount = 8,
}