namespace Roadbed.Logging;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

/// <summary>
/// Default implementation of <see cref="ILoggingActivityService"/>.
/// </summary>
/// <remarks>
/// Snapshots the host name and process identifier at instance construction
/// so that every <c>BeginAsync</c> call records consistent provenance
/// without making a syscall on every invocation.
/// </remarks>
public sealed class LoggingActivityService
    : BaseClassWithLogging,
      ILoggingActivityService
{
    #region Private Fields

    /// <summary>
    /// Name of the diagnostic <see cref="Activity"/> opened for each run.
    /// </summary>
    private const string ActivityOperationName = "roadbed.logging.activity";

    /// <summary>
    /// Tag key under which the activity identifier is exposed on the
    /// diagnostic <see cref="Activity"/>.
    /// </summary>
    private const string ActivityIdTagKey = "roadbed.activity_id";

    /// <summary>
    /// Scope-state key under which the activity identifier is exposed to the
    /// MEL logging pipeline.
    /// </summary>
    private const string ActivityIdScopeKey = "activity_id";

    private readonly ILoggingActivityRepository _activityRepository;
    private readonly ILoggingActivityInputRepository _activityInputRepository;
    private readonly LoggingOptions _options;
    private readonly string _hostName;
    private readonly int _processId;

    #endregion Private Fields

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingActivityService"/> class.
    /// </summary>
    /// <param name="logger">Represents a type used to perform logging.</param>
    public LoggingActivityService(
        ILogger<LoggingActivityService> logger)
        : this(
            ServiceLocator.GetService<ILoggingActivityRepository>(),
            ServiceLocator.GetService<ILoggingActivityInputRepository>(),
            ServiceLocator.GetService<LoggingOptions>(),
            logger)
    {
    }

    #endregion Public Constructors

    #region Internal Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingActivityService"/> class.
    /// </summary>
    /// <param name="activityRepository">Repository for the <c>activity</c> table.</param>
    /// <param name="activityInputRepository">Repository for the <c>activity_input</c> lineage table.</param>
    /// <param name="options">Host-supplied logging options.</param>
    /// <param name="logger">Represents a type used to perform logging.</param>
    internal LoggingActivityService(
        ILoggingActivityRepository activityRepository,
        ILoggingActivityInputRepository activityInputRepository,
        LoggingOptions options,
        ILogger<LoggingActivityService> logger)
        : base(logger)
    {
        ArgumentNullException.ThrowIfNull(activityRepository);
        ArgumentNullException.ThrowIfNull(activityInputRepository);
        ArgumentNullException.ThrowIfNull(options);

        this._activityRepository = activityRepository;
        this._activityInputRepository = activityInputRepository;
        this._options = options;
        this._hostName = Environment.MachineName;
        this._processId = Environment.ProcessId;
    }

    #endregion Internal Constructors

    #region Public Methods

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// This method is deliberately <strong>not</strong> <c>async</c>. The
    /// diagnostic <see cref="Activity"/> and the MEL logging scope must be
    /// established in the <em>caller's</em> execution context so that the
    /// caller's subsequent <c>ILogger</c> calls inherit the ambient
    /// <c>activity_id</c> (which is how <c>RoadbedDbLogRecordExporter</c>
    /// stamps the <c>log_entries.activity_id</c> column).
    /// </para>
    /// <para>
    /// Both <c>Activity.Current</c> and the MEL scope are backed by
    /// <see cref="System.Threading.AsyncLocal{T}"/>. The C# async-method
    /// builder snapshots the <c>ExecutionContext</c> before running an async
    /// method's synchronous prologue and restores it when control yields
    /// back to the caller, so any AsyncLocal mutation made <em>inside</em> an
    /// <c>async</c> method — prologue or continuation — is discarded from the
    /// caller's view. Pushing the ambient here, in a plain method that shares
    /// the caller's <c>ExecutionContext</c>, lets the mutation flow back to
    /// the caller; the awaitable INSERT is delegated to the private
    /// <see cref="InsertAndWrapAsync"/> tail.
    /// </para>
    /// </remarks>
    public Task<LoggingActivityScope> BeginAsync(
        LoggingActivityBeginRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Id);

        // Start a diagnostic Activity *before* the INSERT so that the row
        // can carry trace_id / span_id sampled from a Current that already
        // covers the run. The Activity defaults to ActivityIdFormat.W3C
        // when no parent is present, producing W3C trace/span ids
        // automatically. Started here (not in the async tail) so
        // Activity.Current flows to the caller — see the method remarks.
        Activity? activity = new Activity(ActivityOperationName).Start();
        activity.SetTag(ActivityIdTagKey, request.Id);

        // Push the MEL scope so subsequent ILogger usage inherits the
        // activity_id without callers having to thread it through. Pushed
        // here, in the caller's execution context, so the scope is active on
        // the caller's logs after `await BeginAsync(...)` returns.
        IDisposable? logScope = this.Logger.BeginScope(
            new Dictionary<string, object>
            {
                [ActivityIdScopeKey] = request.Id,
            });

        // Stamp created_on explicitly (not server DEFAULT) so we know the
        // exact value at scope time. UPDATE statements include it in their
        // WHERE clause to enable MySQL partition pruning to one partition.
        DateTime nowUtc = DateTime.UtcNow;

        var entity = new LoggingActivity
        {
            Id = request.Id,
            ParentActivityId = request.ParentActivityId,
            RootActivityId = request.RootActivityId ?? request.Id,
            TraceId = activity.TraceId.ToHexString(),
            SpanId = activity.SpanId.ToHexString(),
            ActivityKey = request.ActivityKey,
            Application = request.Application ?? this._options.Application,
            Environment = request.Environment ?? this._options.Environment,
            ActivityType = request.ActivityType ?? LoggingActivityType.Unknown.ToString().ToLowerInvariant(),
            Target = request.Target,
            Status = LoggingActivityStatus.Running,
            StartedOn = nowUtc,
            LastHeartbeatOn = nowUtc,
            ParametersJson = request.ParametersJson,
            SchedulerInstanceId = request.SchedulerInstanceId,
            FireInstanceId = request.FireInstanceId,
            QuartzJobName = request.QuartzJobName,
            QuartzJobGroup = request.QuartzJobGroup,
            QuartzTriggerName = request.QuartzTriggerName,
            QuartzTriggerGroup = request.QuartzTriggerGroup,
            Host = this._hostName,
            ProcessId = this._processId,
            CreatedBy = request.CreatedBy,
            CreatedOn = nowUtc,
            LastModifiedOn = nowUtc,
        };

        return this.InsertAndWrapAsync(entity, nowUtc, activity, logScope, cancellationToken);
    }

    /// <inheritdoc/>
    public Task HeartbeatAsync(
        LoggingActivityScope scope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scope);

        return this._activityRepository.RecordHeartbeatAsync(
            scope.ActivityId,
            scope.CreatedOn,
            DateTime.UtcNow,
            cancellationToken);
    }

    /// <inheritdoc/>
    public Task HeartbeatAsync(
        string activityId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(activityId);

        return this._activityRepository.RecordHeartbeatAsync(
            activityId,
            createdOn: null,
            DateTime.UtcNow,
            cancellationToken);
    }

    /// <inheritdoc/>
    public Task UpdateAsync(
        LoggingActivityUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ActivityId);

        return this._activityRepository.UpdateCurrentStateAsync(request, cancellationToken);
    }

    /// <inheritdoc/>
    public Task CompleteAsync(
        LoggingActivityScope scope,
        LoggingActivityStatus status,
        long? recordsImpacted = null,
        string? metricsJson = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scope);

        return this.CompleteCoreAsync(
            scope.ActivityId,
            scope.CreatedOn,
            status,
            recordsImpacted,
            metricsJson,
            cancellationToken);
    }

    /// <inheritdoc/>
    public Task CompleteAsync(
        string activityId,
        LoggingActivityStatus status,
        long? recordsImpacted = null,
        string? metricsJson = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(activityId);

        return this.CompleteCoreAsync(
            activityId,
            createdOn: null,
            status,
            recordsImpacted,
            metricsJson,
            cancellationToken);
    }

    /// <inheritdoc/>
    public Task FailAsync(
        LoggingActivityScope scope,
        Exception error,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(error);

        return this._activityRepository.FailAsync(
            scope.ActivityId,
            scope.CreatedOn,
            DateTime.UtcNow,
            error.Message,
            error.GetType().FullName ?? error.GetType().Name,
            cancellationToken);
    }

    /// <inheritdoc/>
    public Task FailAsync(
        string activityId,
        Exception error,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(activityId);
        ArgumentNullException.ThrowIfNull(error);

        return this._activityRepository.FailAsync(
            activityId,
            createdOn: null,
            DateTime.UtcNow,
            error.Message,
            error.GetType().FullName ?? error.GetType().Name,
            cancellationToken);
    }

    /// <inheritdoc/>
    public Task AddInputAsync(
        string activityId,
        string inputActivityId,
        string? inputRole = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(activityId);
        ArgumentException.ThrowIfNullOrWhiteSpace(inputActivityId);

        var entity = new LoggingActivityInput
        {
            ActivityId = activityId,
            InputActivityId = inputActivityId,
            InputRole = inputRole,
        };

        return this._activityInputRepository.InsertAsync(entity, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> ReapStaleActivitiesAsync(
        TimeSpan staleAfter,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(staleAfter, TimeSpan.Zero);

        DateTime nowUtc = DateTime.UtcNow;
        DateTime staleBefore = nowUtc - staleAfter;
        string? environment = this.ScopedEnvironment();

        IReadOnlyList<string> stale = await this._activityRepository
            .FindStaleAsync(this._options.Application, environment, staleBefore, cancellationToken)
            .ConfigureAwait(false);

        if (stale.Count == 0)
        {
            return stale;
        }

        string metricsJson = BuildReapMetricsJson(reason, staleAfter);

        // Always reaped as Canceled — never Succeeded/Failed. The reason lives
        // in metrics; error/error_type stay reserved for real exceptions.
        await this._activityRepository
            .ReapAsync(
                stale,
                this._options.Application,
                environment,
                LoggingActivityStatus.Canceled,
                nowUtc,
                metricsJson,
                cancellationToken)
            .ConfigureAwait(false);

        return stale;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<string>> FindStaleActivitiesAsync(
        TimeSpan staleAfter,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(staleAfter, TimeSpan.Zero);

        DateTime staleBefore = DateTime.UtcNow - staleAfter;

        return this._activityRepository.FindStaleAsync(
            this._options.Application,
            this.ScopedEnvironment(),
            staleBefore,
            cancellationToken);
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Serializes the reap marker written to a reaped row's <c>metrics</c>
    /// column.
    /// </summary>
    /// <param name="reason">Optional caller-supplied reason.</param>
    /// <param name="staleAfter">The staleness threshold the sweep used.</param>
    /// <returns>A compact JSON object of the form <c>{"reaped":true,"reason":...,"stale_after_seconds":N}</c>.</returns>
    private static string BuildReapMetricsJson(string? reason, TimeSpan staleAfter)
    {
        return JsonConvert.SerializeObject(new
        {
            reaped = true,
            reason,
            stale_after_seconds = (long)staleAfter.TotalSeconds,
        });
    }

    /// <summary>
    /// Returns the configured environment normalized to <c>null</c> when blank,
    /// so the repository only filters on environment when one is actually set.
    /// </summary>
    /// <returns>The non-blank environment, or <c>null</c>.</returns>
    private string? ScopedEnvironment()
    {
        return string.IsNullOrWhiteSpace(this._options.Environment)
            ? null
            : this._options.Environment;
    }

    /// <summary>
    /// Awaitable tail of <see cref="BeginAsync"/>: performs the activity-row
    /// INSERT and wraps the already-pushed ambient state in a
    /// <see cref="LoggingActivityScope"/>.
    /// </summary>
    /// <param name="entity">The fully-populated activity row to insert.</param>
    /// <param name="createdOn">The UTC <c>created_on</c> stamp shared with the scope for partition-pruned updates.</param>
    /// <param name="activity">The diagnostic <see cref="Activity"/> already started in the caller's execution context.</param>
    /// <param name="logScope">The MEL scope handle already opened in the caller's execution context.</param>
    /// <param name="cancellationToken">Token to notify when the operation should be canceled.</param>
    /// <returns>The scope bundling the activity id, timestamp, and ambient handles.</returns>
    /// <remarks>
    /// On insert failure the ambient handles are disposed best-effort so the
    /// run does not leave a live scope pointing at a row that was never
    /// committed. The disposal runs in this async frame, so the
    /// <c>Activity.Current</c> pointer the caller already inherited reverts
    /// only when the caller's own logical async scope unwinds; the practical
    /// effect is that the stopped activity is never reused because
    /// <c>BeginAsync</c> rethrows and the caller's <c>using</c> binding is
    /// never assigned.
    /// </remarks>
    private async Task<LoggingActivityScope> InsertAndWrapAsync(
        LoggingActivity entity,
        DateTime createdOn,
        Activity activity,
        IDisposable? logScope,
        CancellationToken cancellationToken)
    {
        try
        {
            await this._activityRepository
                .InsertAsync(entity, cancellationToken)
                .ConfigureAwait(false);
        }
        catch
        {
            logScope?.Dispose();
            activity.Dispose();
            throw;
        }

        return new LoggingActivityScope(entity.Id, createdOn, activity, logScope);
    }

    /// <summary>
    /// Shared body for the two <c>CompleteAsync</c> overloads. Rejects
    /// the <see cref="LoggingActivityStatus.Failed"/> status so callers
    /// must route through <c>FailAsync</c> for exception-driven failures.
    /// </summary>
    /// <param name="activityId">Identifier of the activity row to finalize.</param>
    /// <param name="createdOn">When supplied, the repository UPDATE includes <c>AND created_on = @CreatedOn</c> for partition pruning.</param>
    /// <param name="status">Terminal status to record.</param>
    /// <param name="recordsImpacted">Optional headline count of records produced or affected during the run.</param>
    /// <param name="metricsJson">Optional metrics JSON to persist alongside the terminal status.</param>
    /// <param name="cancellationToken">Token to notify when the operation should be canceled.</param>
    /// <returns>A task that completes when the row has been finalized.</returns>
    private Task CompleteCoreAsync(
        string activityId,
        DateTime? createdOn,
        LoggingActivityStatus status,
        long? recordsImpacted,
        string? metricsJson,
        CancellationToken cancellationToken)
    {
        if (status == LoggingActivityStatus.Failed)
        {
            throw new ArgumentException(
                $"Use {nameof(this.FailAsync)} to record an exception-driven failure.",
                nameof(status));
        }

        return this._activityRepository.CompleteAsync(
            activityId,
            createdOn,
            status,
            DateTime.UtcNow,
            recordsImpacted,
            metricsJson,
            cancellationToken);
    }

    #endregion Private Methods
}
