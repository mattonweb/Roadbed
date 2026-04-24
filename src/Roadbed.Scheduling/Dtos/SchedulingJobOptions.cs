/*
 * The namespace Roadbed.Scheduling.Dtos was removed on purpose and replaced with Roadbed.Scheduling so that no additional using statements are required.
 */
namespace Roadbed.Scheduling;

using System.Collections.Generic;

/// <summary>
/// Options POCO for gating and configuring scheduled jobs at application startup.
/// </summary>
/// <remarks>
/// This type is populated by the hosting application (typically from its own
/// <c>appsettings.json</c>) and registered as a singleton in DI. Jobs opt in to
/// gating by taking <see cref="SchedulingJobOptions"/> in their constructor and
/// passing it to the matching <see cref="BaseSchedulingJob{T}"/> base constructor.
///
/// <para>
/// Roadbed.Scheduling never reads configuration directly — the application owns
/// the mapping between its own configuration shape and this POCO.
/// </para>
///
/// <para>
/// The key used to look up an entry in <see cref="Features"/> is the job's
/// <see cref="ISchedulingJob.Name"/>. Missing entries mean "use the job's
/// hardcoded defaults." Presence of an entry can override the schedule or
/// disable the job entirely.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var options = new SchedulingJobOptions
/// {
///     Features = new Dictionary&lt;string, SchedulingJobFeature&gt;
///     {
///         ["ForecastRefresh"] = new SchedulingJobFeature
///         {
///             Enabled = true,
///             CronExpression = "0 */5 * * * ?",
///             Arguments = new Dictionary&lt;string, string&gt; { ["zone"] = "public" },
///         },
///         ["TelemetryScrubber"] = new SchedulingJobFeature { Enabled = false },
///     },
/// };
///
/// services.AddSingleton(options);
/// </code>
/// </example>
public sealed class SchedulingJobOptions
{
    #region Public Properties

    /// <summary>
    /// Gets the per-job feature entries, keyed by <see cref="ISchedulingJob.Name"/>.
    /// </summary>
    /// <remarks>
    /// Jobs whose <see cref="ISchedulingJob.Name"/> is not present in this dictionary
    /// use their hardcoded defaults. Defaults to an empty dictionary.
    /// </remarks>
    public IReadOnlyDictionary<string, SchedulingJobFeature> Features { get; init; }
        = new Dictionary<string, SchedulingJobFeature>();

    #endregion Public Properties
}
