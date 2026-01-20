namespace Roadbed.Scheduling;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

/// <summary>
/// Abstract base class for scheduled jobs providing common functionality.
/// </summary>
/// <typeparam name="T">Class inheriting from BaseSchedulingJob.</typeparam>
public abstract class BaseSchedulingJob<T>
    : BaseClassWithLogging<T>, ISchedulingJob
{
    #region Private Fields

    private readonly string? _name;
    private readonly string? _description;
    private readonly SchedulingSchedule? _schedule;

    #endregion Private Fields

    #region Protected Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseSchedulingJob{T}"/> class.
    /// </summary>
    /// <param name="logger">Represents a type used to perform logging.</param>
    /// <remarks>
    /// When using this constructor, derived classes must override Name, Description, and Schedule properties.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
    protected BaseSchedulingJob(ILogger logger)
        : base(logger)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseSchedulingJob{T}"/> class.
    /// </summary>
    /// <param name="name">The unique name for this job.</param>
    /// <param name="description">A description of what this job does.</param>
    /// <param name="schedule">The schedule configuration for this job.</param>
    /// <param name="logger">Represents a type used to perform logging.</param>
    /// <remarks>
    /// When using this constructor, Name, Description, and Schedule are set from constructor parameters.
    /// Derived classes should NOT override these properties.
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when name or description is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when schedule or logger is null.</exception>
    protected BaseSchedulingJob(
        string name,
        string description,
        SchedulingSchedule schedule,
        ILogger logger)
        : base(logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentNullException.ThrowIfNull(schedule);

        this._name = name;
        this._description = description;
        this._schedule = schedule;
    }

    #endregion Protected Constructors

    #region Public Properties

    /// <inheritdoc/>
    public virtual string Description
    {
        get
        {
            if (this._description != null)
            {
                return this._description;
            }

            throw new InvalidOperationException(
                $"Description must be provided either through constructor or by overriding the Description property in {this.GetType().Name}.");
        }
    }

    /// <inheritdoc/>
    public virtual string Name
    {
        get
        {
            if (this._name != null)
            {
                return this._name;
            }

            throw new InvalidOperationException(
                $"Name must be provided either through constructor or by overriding the Name property in {this.GetType().Name}.");
        }
    }

    /// <inheritdoc/>
    public virtual SchedulingSchedule Schedule
    {
        get
        {
            if (this._schedule != null)
            {
                return this._schedule;
            }

            throw new InvalidOperationException(
                $"Schedule must be provided either through constructor or by overriding the Schedule property in {this.GetType().Name}.");
        }
    }

    #endregion Public Properties

    #region Public Methods

    /// <inheritdoc/>
    public abstract Task ExecuteAsync(CancellationToken cancellationToken);

    #endregion Public Methods
}