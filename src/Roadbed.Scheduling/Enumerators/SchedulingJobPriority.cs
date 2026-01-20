/*
 * The namespace Roadbed.Scheduling.Enumerators was removed on purpose and replaced with Roadbed.Crud so that no additional using statements are required.
 */
namespace Roadbed.Scheduling;

/// <summary>
/// Defines the priority for jobs in Quartz.NET.
/// </summary>
public enum SchedulingJobPriority
{
    /// <summary>
    /// Lowest job priority.
    /// </summary>
    Lowest = 0,

    /// <summary>
    /// Very Low job priority.
    /// </summary>
    VeryLow = 2,

    /// <summary>
    /// Low job priority.
    /// </summary>
    Low = 4,

    /// <summary>
    /// Normal job priority.
    /// </summary>
    Normal = 5,

    /// <summary>
    /// High job priority.
    /// </summary>
    High = 7,

    /// <summary>
    /// Very High job priority.
    /// </summary>
    VeryHigh = 9,

    /// <summary>
    /// Highest job priority.
    /// </summary>
    Highest = 10,
}