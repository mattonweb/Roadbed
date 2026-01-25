/*
 * The namespace Roadbed.Scheduling.Services was removed on purpose
 * and replaced with Roadbed.Scheduling so that no additional
 * using statements are required.
 */
namespace Roadbed.Scheduling;

using System;

/// <summary>
/// Default no-op implementation of ISchedulingMetrics that does nothing.
/// </summary>
/// <remarks>
/// This implementation is used as the default when no metrics implementation is registered
/// by the consumer. All methods are empty, resulting in zero overhead when metrics are not needed.
/// The JIT compiler optimizes away the empty method calls, ensuring no performance impact.
/// This class is internal and exposed only through the singleton Instance property.
/// Thread-safe as it is stateless and immutable.
/// </remarks>
internal sealed class NullSchedulingMetrics : ISchedulingMetrics
{
    #region Public Fields

    /// <summary>
    /// Gets the singleton instance of NullSchedulingMetrics.
    /// </summary>
    /// <remarks>
    /// This singleton is registered as the default ISchedulingMetrics implementation
    /// in the DI container when no custom implementation is provided by the consumer.
    /// Reusing the same instance avoids unnecessary allocations.
    /// </remarks>
    public static readonly NullSchedulingMetrics Instance = new NullSchedulingMetrics();

    #endregion Public Fields

    #region Private Constructors

    private NullSchedulingMetrics()
    {
    }

    #endregion Private Constructors

    #region Public Methods

    /// <inheritdoc/>
    public void JobCompleted(JobExecutionInfo info, TimeSpan duration)
    {
        // No-op: Intentionally empty for zero overhead
    }

    /// <inheritdoc/>
    public void JobFailed(JobExecutionInfo info, Exception exception, TimeSpan duration)
    {
        // No-op: Intentionally empty for zero overhead
    }

    /// <inheritdoc/>
    public void JobMisfired(JobExecutionInfo info)
    {
        // No-op: Intentionally empty for zero overhead
    }

    /// <inheritdoc/>
    public void JobStarted(JobExecutionInfo info)
    {
        // No-op: Intentionally empty for zero overhead
    }

    #endregion Public Methods
}