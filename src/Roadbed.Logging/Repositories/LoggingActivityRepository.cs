namespace Roadbed.Logging;

using System;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Roadbed.Data;

/// <summary>
/// Writes and patches rows in the <c>activity</c> table.
/// </summary>
internal sealed class LoggingActivityRepository
    : BaseClassWithLogging,
      ILoggingActivityRepository
{
    #region Private Fields

    private readonly ILoggingDatabaseFactory _factory;
    private readonly string _tableRef;

    #endregion Private Fields

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingActivityRepository"/> class.
    /// </summary>
    /// <param name="factory">Database connection factory pointing at the activity schema.</param>
    /// <param name="options">Host-supplied logging options.</param>
    /// <param name="logger">Logger used for retry diagnostics on the data path.</param>
    public LoggingActivityRepository(
        ILoggingDatabaseFactory factory,
        LoggingOptions options,
        ILogger<LoggingActivityRepository> logger)
        : base(logger)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(options);

        this._factory = factory;
        this._tableRef = string.IsNullOrWhiteSpace(options.Schema)
            ? "activity"
            : $"{options.Schema}.activity";
    }

    #endregion Public Constructors

    #region Public Methods

    /// <inheritdoc/>
    public async Task InsertAsync(
        LoggingActivity entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentException.ThrowIfNullOrWhiteSpace(entity.Id);

        // created_on / last_modified_on are stamped explicitly with UTC
        // values supplied by the caller (DateTime.UtcNow from
        // LoggingActivityService.BeginAsync) so we never rely on the
        // session time_zone of the underlying connection. The values
        // chosen here are what LoggingActivityScope.CreatedOn returns,
        // so subsequent UPDATE WHEREs can include AND created_on = ? and
        // prune to one MySQL partition.
        string sql = $@"
            INSERT INTO {this._tableRef}
            (
                 id
                ,parent_activity_id
                ,root_activity_id
                ,trace_id
                ,span_id
                ,activity_key
                ,application
                ,environment
                ,activity_type
                ,target
                ,status
                ,started_on
                ,last_heartbeat_on
                ,parameters
                ,scheduler_instance_id
                ,fire_instance_id
                ,quartz_job_name
                ,quartz_job_group
                ,quartz_trigger_name
                ,quartz_trigger_group
                ,host
                ,process_id
                ,created_by
                ,created_on
                ,last_modified_on
            )
            VALUES
            (
                 @Id
                ,@ParentActivityId
                ,@RootActivityId
                ,@TraceId
                ,@SpanId
                ,@ActivityKey
                ,@Application
                ,@Environment
                ,@ActivityType
                ,@Target
                ,@Status
                ,@StartedOn
                ,@LastHeartbeatOn
                ,@ParametersJson
                ,@SchedulerInstanceId
                ,@FireInstanceId
                ,@QuartzJobName
                ,@QuartzJobGroup
                ,@QuartzTriggerName
                ,@QuartzTriggerGroup
                ,@Host
                ,@ProcessId
                ,@CreatedBy
                ,@CreatedOn
                ,@LastModifiedOn
            )
            ;";

        var request = new DataExecutorRequest(sql)
        {
            Parameters = new
            {
                entity.Id,
                entity.ParentActivityId,
                entity.RootActivityId,
                entity.TraceId,
                entity.SpanId,
                entity.ActivityKey,
                entity.Application,
                entity.Environment,
                entity.ActivityType,
                entity.Target,
                Status = entity.Status.ToString().ToLowerInvariant(),
                entity.StartedOn,
                entity.LastHeartbeatOn,
                entity.ParametersJson,
                entity.SchedulerInstanceId,
                entity.FireInstanceId,
                entity.QuartzJobName,
                entity.QuartzJobGroup,
                entity.QuartzTriggerName,
                entity.QuartzTriggerGroup,
                entity.Host,
                entity.ProcessId,
                entity.CreatedBy,
                entity.CreatedOn,
                entity.LastModifiedOn,
            },
        };

        await LoggingSqlDispatcher
            .ExecuteAsync(request, this._factory, this.Logger, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task UpdateCurrentStateAsync(
        LoggingActivityUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ActivityId);

        string whereClause = BuildWhereClause(request.CreatedOn);

        string sql = $@"
            UPDATE {this._tableRef}
            SET
                 activity_key          = COALESCE(@ActivityKey,          activity_key)
                ,activity_type         = COALESCE(@ActivityType,         activity_type)
                ,target                = COALESCE(@Target,               target)
                ,parameters            = COALESCE(@ParametersJson,       parameters)
                ,metrics               = COALESCE(@MetricsJson,          metrics)
                ,records_impacted      = COALESCE(@RecordsImpacted,      records_impacted)
                ,scheduler_instance_id = COALESCE(@SchedulerInstanceId,  scheduler_instance_id)
                ,fire_instance_id      = COALESCE(@FireInstanceId,       fire_instance_id)
                ,quartz_job_name       = COALESCE(@QuartzJobName,        quartz_job_name)
                ,quartz_job_group      = COALESCE(@QuartzJobGroup,       quartz_job_group)
                ,quartz_trigger_name   = COALESCE(@QuartzTriggerName,    quartz_trigger_name)
                ,quartz_trigger_group  = COALESCE(@QuartzTriggerGroup,   quartz_trigger_group)
                ,last_modified_on      = @LastModifiedOn
            {whereClause}
            ;";

        var parameters = new DynamicParameters();
        parameters.Add("ActivityId", request.ActivityId);
        parameters.Add("ActivityKey", request.ActivityKey);
        parameters.Add("ActivityType", request.ActivityType);
        parameters.Add("Target", request.Target);
        parameters.Add("ParametersJson", request.ParametersJson);
        parameters.Add("MetricsJson", request.MetricsJson);
        parameters.Add("RecordsImpacted", request.RecordsImpacted);
        parameters.Add("SchedulerInstanceId", request.SchedulerInstanceId);
        parameters.Add("FireInstanceId", request.FireInstanceId);
        parameters.Add("QuartzJobName", request.QuartzJobName);
        parameters.Add("QuartzJobGroup", request.QuartzJobGroup);
        parameters.Add("QuartzTriggerName", request.QuartzTriggerName);
        parameters.Add("QuartzTriggerGroup", request.QuartzTriggerGroup);
        parameters.Add("LastModifiedOn", DateTime.UtcNow);
        if (request.CreatedOn.HasValue)
        {
            parameters.Add("CreatedOn", request.CreatedOn.Value);
        }

        var executorRequest = new DataExecutorRequest(sql)
        {
            Parameters = parameters,
        };

        await LoggingSqlDispatcher
            .ExecuteAsync(executorRequest, this._factory, this.Logger, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task RecordHeartbeatAsync(
        string activityId,
        DateTime? createdOn,
        DateTime heartbeatOn,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(activityId);

        string whereClause = BuildWhereClause(createdOn);

        string sql = $@"
            UPDATE {this._tableRef}
            SET
                 last_heartbeat_on = @HeartbeatOn
                ,last_modified_on  = @LastModifiedOn
            {whereClause}
            ;";

        var parameters = new DynamicParameters();
        parameters.Add("ActivityId", activityId);
        parameters.Add("HeartbeatOn", heartbeatOn);
        parameters.Add("LastModifiedOn", DateTime.UtcNow);
        if (createdOn.HasValue)
        {
            parameters.Add("CreatedOn", createdOn.Value);
        }

        var request = new DataExecutorRequest(sql)
        {
            Parameters = parameters,
        };

        await LoggingSqlDispatcher
            .ExecuteAsync(request, this._factory, this.Logger, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task CompleteAsync(
        string activityId,
        DateTime? createdOn,
        LoggingActivityStatus status,
        DateTime completedOn,
        long? recordsImpacted,
        string? metricsJson,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(activityId);

        string whereClause = BuildWhereClause(createdOn);

        string sql = $@"
            UPDATE {this._tableRef}
            SET
                 status            = @Status
                ,completed_on      = @CompletedOn
                ,records_impacted  = COALESCE(@RecordsImpacted, records_impacted)
                ,metrics           = COALESCE(@MetricsJson,     metrics)
                ,last_modified_on  = @LastModifiedOn
            {whereClause}
            ;";

        var parameters = new DynamicParameters();
        parameters.Add("ActivityId", activityId);
        parameters.Add("Status", status.ToString().ToLowerInvariant());
        parameters.Add("CompletedOn", completedOn);
        parameters.Add("RecordsImpacted", recordsImpacted);
        parameters.Add("MetricsJson", metricsJson);
        parameters.Add("LastModifiedOn", DateTime.UtcNow);
        if (createdOn.HasValue)
        {
            parameters.Add("CreatedOn", createdOn.Value);
        }

        var request = new DataExecutorRequest(sql)
        {
            Parameters = parameters,
        };

        await LoggingSqlDispatcher
            .ExecuteAsync(request, this._factory, this.Logger, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task FailAsync(
        string activityId,
        DateTime? createdOn,
        DateTime completedOn,
        string error,
        string errorType,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(activityId);
        ArgumentNullException.ThrowIfNull(error);
        ArgumentNullException.ThrowIfNull(errorType);

        string whereClause = BuildWhereClause(createdOn);

        string sql = $@"
            UPDATE {this._tableRef}
            SET
                 status            = @Status
                ,completed_on      = @CompletedOn
                ,error             = @Error
                ,error_type        = @ErrorType
                ,last_modified_on  = @LastModifiedOn
            {whereClause}
            ;";

        var parameters = new DynamicParameters();
        parameters.Add("ActivityId", activityId);
        parameters.Add("Status", LoggingActivityStatus.Failed.ToString().ToLowerInvariant());
        parameters.Add("CompletedOn", completedOn);
        parameters.Add("Error", error);
        parameters.Add("ErrorType", errorType);
        parameters.Add("LastModifiedOn", DateTime.UtcNow);
        if (createdOn.HasValue)
        {
            parameters.Add("CreatedOn", createdOn.Value);
        }

        var request = new DataExecutorRequest(sql)
        {
            Parameters = parameters,
        };

        await LoggingSqlDispatcher
            .ExecuteAsync(request, this._factory, this.Logger, cancellationToken)
            .ConfigureAwait(false);
    }

    #endregion Public Methods

    #region Internal Methods

    /// <summary>
    /// Builds the WHERE clause used by every activity UPDATE.
    /// </summary>
    /// <param name="createdOn">
    /// When supplied, the clause includes
    /// <c>AND created_on = @CreatedOn</c> so MySQL prunes the UPDATE to
    /// the single monthly partition that owns the row. When <c>null</c>,
    /// the clause filters only on <c>id</c> and the UPDATE has to probe
    /// every defined partition.
    /// </param>
    /// <returns>A <c>WHERE</c> clause string ready for SQL concatenation.</returns>
    /// <remarks>Exposed to <c>Roadbed.Test.Unit</c> for SQL-shape verification.</remarks>
    internal static string BuildWhereClause(DateTime? createdOn)
    {
        return createdOn.HasValue
            ? "WHERE id = @ActivityId AND created_on = @CreatedOn"
            : "WHERE id = @ActivityId";
    }

    #endregion Internal Methods
}
