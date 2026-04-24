/*
 * The namespace Roadbed.Scheduling.Dtos was removed on purpose and replaced with Roadbed.Scheduling so that no additional using statements are required.
 */
namespace Roadbed.Scheduling;

using System.Collections.Generic;

/// <summary>
/// A single per-job entry in <see cref="SchedulingJobOptions.Features"/>.
/// </summary>
/// <remarks>
/// Each instance describes whether a named job is enabled, optionally overrides
/// its cron expression, and may carry a free-form <see cref="Arguments"/> bag
/// for application-specific extensibility.
/// </remarks>
public sealed class SchedulingJobFeature
{
    #region Public Properties

    /// <summary>
    /// Gets a value indicating whether the job is enabled.
    /// </summary>
    /// <remarks>
    /// When <see langword="false"/>, the job is skipped during Quartz registration —
    /// it is not added to the scheduler, has no trigger, and cannot be invoked
    /// manually. Defaults to <see langword="true"/>.
    /// </remarks>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Gets the cron expression that overrides the job's default schedule.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When set, the job's schedule is replaced with
    /// <c>new SchedulingSchedule(CronExpression)</c>. When <see langword="null"/>
    /// or whitespace, the job uses its hardcoded default schedule (if one was
    /// supplied to the base constructor).
    /// </para>
    ///
    /// <para>
    /// Jobs that take <see cref="SchedulingJobOptions"/> without a default schedule
    /// require this property to be populated whenever <see cref="Enabled"/> is
    /// <see langword="true"/>.
    /// </para>
    /// </remarks>
    public string? CronExpression { get; init; }

    /// <summary>
    /// Gets an optional extensibility bag for job-specific arguments.
    /// </summary>
    /// <remarks>
    /// The framework does not interpret these values — jobs read whichever keys
    /// they care about. Use this for per-zone or per-deployment parameters that
    /// are not represented by the other properties on this type.
    /// </remarks>
    public IReadOnlyDictionary<string, string>? Arguments { get; init; }

    #endregion Public Properties
}
