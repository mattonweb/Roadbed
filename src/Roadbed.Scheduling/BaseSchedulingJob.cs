namespace Roadbed.Scheduling;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Quartz;

/// <summary>
/// Abstract base class for scheduled jobs providing common functionality.
/// </summary>
/// <typeparam name="T">Class inheriting from BaseSchedulingJob.</typeparam>
/// <remarks>
/// This base class provides two initialization patterns:
/// 1. Constructor injection: Pass name, description, and schedule to base constructor
/// 2. Property override: Override Name, Description, and Schedule properties in derived class
///
/// The Context property is available during job execution and can be used to set result messages
/// via Context.Result, which will be captured by the metrics system.
/// </remarks>
public abstract class BaseSchedulingJob<T>
    : BaseClassWithLoggingFactory<T>, ISchedulingJob
{
    #region Private Fields

    private readonly string? _description;
    private readonly string? _name;
    private readonly SchedulingSchedule? _schedule;
    private readonly bool _isEnabled = true;
    private IJobExecutionContext? _currentContext;

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

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseSchedulingJob{T}"/> class for manual-only execution.
    /// </summary>
    /// <param name="name">The unique name for this job.</param>
    /// <param name="description">A description of what this job does.</param>
    /// <param name="logger">Represents a type used to perform logging.</param>
    /// <remarks>
    /// Jobs created with this constructor have no automatic schedule and must be triggered
    /// programmatically via <c>ISchedulerFactory</c>. They are registered as durable in Quartz
    /// (persisted without a trigger). Use <c>scheduler.TriggerJob(new JobKey(name, groupName))</c>
    /// to execute on demand.
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when name or description is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
    protected BaseSchedulingJob(
        string name,
        string description,
        ILogger logger)
        : base(logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        this._name = name;
        this._description = description;
        this._schedule = new SchedulingSchedule();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseSchedulingJob{T}"/> class with a default
    /// schedule that may be overridden or disabled by <see cref="SchedulingJobOptions"/>.
    /// </summary>
    /// <param name="name">The unique name for this job. Used as the lookup key into <see cref="SchedulingJobOptions.Features"/>.</param>
    /// <param name="description">A description of what this job does.</param>
    /// <param name="defaultSchedule">The fallback schedule used when no matching options entry is present or the entry does not override the cron expression.</param>
    /// <param name="options">Options POCO supplied by the hosting application. Must be registered in DI as a singleton.</param>
    /// <param name="logger">Represents a type used to perform logging.</param>
    /// <remarks>
    /// Resolution rules:
    /// <list type="bullet">
    /// <item><description>Missing entry in <see cref="SchedulingJobOptions.Features"/> → use <paramref name="defaultSchedule"/>, enabled.</description></item>
    /// <item><description>Entry with <see cref="SchedulingJobFeature.Enabled"/> = <see langword="false"/> → job is disabled and not registered.</description></item>
    /// <item><description>Entry with a non-empty <see cref="SchedulingJobFeature.CronExpression"/> → use that cron expression.</description></item>
    /// <item><description>Entry with <see cref="SchedulingJobFeature.Enabled"/> = <see langword="true"/> and no cron → use <paramref name="defaultSchedule"/>.</description></item>
    /// </list>
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when name or description is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when defaultSchedule, options, or logger is null.</exception>
    protected BaseSchedulingJob(
        string name,
        string description,
        SchedulingSchedule defaultSchedule,
        SchedulingJobOptions options,
        ILogger logger)
        : base(logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentNullException.ThrowIfNull(defaultSchedule);
        ArgumentNullException.ThrowIfNull(options);

        this._name = name;
        this._description = description;
        (this._isEnabled, this._schedule) = ResolveFromOptions(name, options, defaultSchedule);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseSchedulingJob{T}"/> class whose schedule
    /// must come from <see cref="SchedulingJobOptions"/>. No default schedule is supplied.
    /// </summary>
    /// <param name="name">The unique name for this job. Used as the lookup key into <see cref="SchedulingJobOptions.Features"/>.</param>
    /// <param name="description">A description of what this job does.</param>
    /// <param name="options">Options POCO supplied by the hosting application. Must contain an entry for <paramref name="name"/>.</param>
    /// <param name="logger">Represents a type used to perform logging.</param>
    /// <remarks>
    /// Use this overload when the schedule is deployment-specific (e.g., differs per security zone)
    /// and there is no sensible universal default. Resolution rules:
    /// <list type="bullet">
    /// <item><description>Missing entry in <see cref="SchedulingJobOptions.Features"/> → throws <see cref="InvalidOperationException"/>.</description></item>
    /// <item><description>Entry with <see cref="SchedulingJobFeature.Enabled"/> = <see langword="false"/> → job is disabled and not registered.</description></item>
    /// <item><description>Entry with <see cref="SchedulingJobFeature.Enabled"/> = <see langword="true"/> and no cron → throws <see cref="InvalidOperationException"/>.</description></item>
    /// <item><description>Entry with a non-empty <see cref="SchedulingJobFeature.CronExpression"/> → use that cron expression.</description></item>
    /// </list>
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when name or description is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when options or logger is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the options entry is missing or enabled without a cron expression.</exception>
    protected BaseSchedulingJob(
        string name,
        string description,
        SchedulingJobOptions options,
        ILogger logger)
        : base(logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentNullException.ThrowIfNull(options);

        this._name = name;
        this._description = description;
        (this._isEnabled, this._schedule) = ResolveFromOptionsStrict(name, options);
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

    /// <inheritdoc/>
    public virtual bool IsEnabled => this._isEnabled;

    #endregion Public Properties

    #region Protected Properties

    /// <summary>
    /// Gets the current job execution context.
    /// </summary>
    /// <remarks>
    /// Available during job execution. Use to set result message or access execution metadata.
    /// Example: this.Context.Result = "Processed 1,234 records".
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when accessed outside of ExecuteAsync method.</exception>
    protected IJobExecutionContext Context
    {
        get
        {
            if (this._currentContext == null)
            {
                throw new InvalidOperationException(
                    "Context is only available during job execution (within ExecuteAsync method).");
            }

            return this._currentContext;
        }
    }

    #endregion Protected Properties

    #region Public Methods

    /// <summary>
    /// Quartz.NET job execution entry point.
    /// </summary>
    /// <param name="context">Quartz job execution context.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    /// <remarks>
    /// This method implements IJob.Execute explicitly to bridge between Quartz.NET and the
    /// user-facing ExecuteAsync method. It manages the Context property lifecycle, ensuring
    /// it is available during ExecuteAsync execution and cleared afterwards.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when context is null.</exception>
    async Task IJob.Execute(IJobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Store context for duration of execution
        this._currentContext = context;

        try
        {
            // Call the user-implemented ExecuteAsync
            await this.ExecuteAsync(context.CancellationToken);
        }
        finally
        {
            // Clear context after execution (prevents access outside ExecuteAsync)
            this._currentContext = null;
        }
    }

    /// <inheritdoc/>
    public abstract Task ExecuteAsync(CancellationToken cancellationToken);

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Resolves the <see cref="IsEnabled"/> and <see cref="Schedule"/> values from
    /// <paramref name="options"/>, falling back to <paramref name="defaultSchedule"/> when no
    /// override is supplied.
    /// </summary>
    private static (bool isEnabled, SchedulingSchedule schedule) ResolveFromOptions(
        string name,
        SchedulingJobOptions options,
        SchedulingSchedule defaultSchedule)
    {
        var features = options.Features ?? new Dictionary<string, SchedulingJobFeature>();

        if (!features.TryGetValue(name, out var feature) || feature is null)
        {
            return (true, defaultSchedule);
        }

        if (!feature.Enabled)
        {
            return (false, new SchedulingSchedule());
        }

        if (!string.IsNullOrWhiteSpace(feature.CronExpression))
        {
            return (true, new SchedulingSchedule(feature.CronExpression));
        }

        return (true, defaultSchedule);
    }

    /// <summary>
    /// Resolves the <see cref="IsEnabled"/> and <see cref="Schedule"/> values from
    /// <paramref name="options"/>. Throws when the options entry is missing or does not
    /// supply a cron expression for an enabled job.
    /// </summary>
    private static (bool isEnabled, SchedulingSchedule schedule) ResolveFromOptionsStrict(
        string name,
        SchedulingJobOptions options)
    {
        var features = options.Features ?? new Dictionary<string, SchedulingJobFeature>();

        if (!features.TryGetValue(name, out var feature) || feature is null)
        {
            throw new InvalidOperationException(
                $"Job '{name}' requires SchedulingJobOptions.Features[\"{name}\"] because no default schedule was supplied.");
        }

        if (!feature.Enabled)
        {
            return (false, new SchedulingSchedule());
        }

        if (string.IsNullOrWhiteSpace(feature.CronExpression))
        {
            throw new InvalidOperationException(
                $"Job '{name}' requires SchedulingJobOptions.Features[\"{name}\"].CronExpression because no default schedule was supplied.");
        }

        return (true, new SchedulingSchedule(feature.CronExpression));
    }

    #endregion Private Methods
}