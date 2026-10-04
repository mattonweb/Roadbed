namespace Roadbed.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Roadbed.Data;

/// <summary>
/// Reads and writes rows in the <c>job_checkpoint</c> control-state table.
/// </summary>
/// <remarks>
/// <para>
/// Nothing here catches an exception: a failed read must never look like
/// "never run", so every database failure propagates to the caller.
/// </para>
/// <para>
/// The only SQL that differs between MySQL/MariaDB and SQLite is the
/// "insert when absent" form, selected per
/// <see cref="DataConnecionString.ConnectionStringType"/> the same way
/// <see cref="LoggingActivityInputRepository"/> does.
/// </para>
/// </remarks>
internal sealed class LoggingJobCheckpointRepository
    : BaseClassWithLogging,
      ILoggingJobCheckpointRepository
{
    #region Private Fields

    /// <summary>
    /// The WHERE clause that pins a statement to one row by its primary key.
    /// </summary>
    private const string KeyWhereClause = @"
            WHERE application    = @Application
              AND environment    = @Environment
              AND job_group      = @JobGroup
              AND job_name       = @JobName
              AND checkpoint_key = @CheckpointKey";

    private readonly ILoggingDataExecutor _executor;
    private readonly ILoggingDatabaseFactory _factory;
    private readonly string _tableRef;

    #endregion Private Fields

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingJobCheckpointRepository"/> class.
    /// </summary>
    /// <param name="executor">Provider-neutral execution port supplied by the active provider satellite.</param>
    /// <param name="factory">Database connection factory pointing at the logging schema.</param>
    /// <param name="options">Host-supplied logging options.</param>
    /// <param name="logger">Logger used for retry diagnostics on the data path.</param>
    public LoggingJobCheckpointRepository(
        ILoggingDataExecutor executor,
        ILoggingDatabaseFactory factory,
        LoggingOptions options,
        ILogger<LoggingJobCheckpointRepository> logger)
        : base(logger)
    {
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(options);

        this._executor = executor;
        this._factory = factory;
        this._tableRef = string.IsNullOrWhiteSpace(options.Schema)
            ? "job_checkpoint"
            : $"{options.Schema}.job_checkpoint";
    }

    #endregion Public Constructors

    #region Public Methods

    /// <inheritdoc/>
    public async Task<LoggingJobCheckpoint?> ReadAsync(
        LoggingJobCheckpointKey key,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        string sql = $@"
            SELECT
                 application
                ,environment
                ,job_group
                ,job_name
                ,checkpoint_key
                ,position_id
                ,position_on
                ,position_text
                ,previous_position_id
                ,previous_position_on
                ,previous_position_text
                ,version
                ,advanced_by_fire_instance_id
                ,last_run_started_on
                ,advanced_on
                ,created_on
                ,last_modified_on
            FROM {this._tableRef}{KeyWhereClause}
            ;";

        var request = new DataExecutorRequest(sql)
        {
            Parameters = BuildKeyParameters(key),
        };

        IEnumerable<LoggingJobCheckpoint> rows = await this._executor
            .QueryAsync<LoggingJobCheckpoint>(request, this._factory, this.Logger, cancellationToken)
            .ConfigureAwait(false);

        LoggingJobCheckpoint? row = rows.FirstOrDefault();

        if (row is not null)
        {
            // DATETIME / TEXT columns carry no time zone; every framework-
            // stamped column is written as UTC, so re-attach UTC rather than
            // converting. Position timestamps are the job's own values and are
            // left as read.
            row.LastRunStartedOn = AsUtc(row.LastRunStartedOn);
            row.AdvancedOn = AsUtc(row.AdvancedOn);
            row.CreatedOn = DateTime.SpecifyKind(row.CreatedOn, DateTimeKind.Utc);
            row.LastModifiedOn = DateTime.SpecifyKind(row.LastModifiedOn, DateTimeKind.Utc);
        }

        return row;
    }

    /// <inheritdoc/>
    public async Task EnsureExistsAsync(
        LoggingJobCheckpointKey key,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        DynamicParameters parameters = BuildKeyParameters(key);
        parameters.Add("NowUtc", nowUtc);

        var request = new DataExecutorRequest(
            BuildEnsureExistsSql(this._factory.Connecion.ConnectionStringType, this._tableRef))
        {
            Parameters = parameters,
        };

        await this._executor
            .ExecuteAsync(request, this._factory, this.Logger, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task MarkRunStartedAsync(
        LoggingJobCheckpointKey key,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        DynamicParameters parameters = BuildKeyParameters(key);
        parameters.Add("NowUtc", nowUtc);

        var request = new DataExecutorRequest(
            BuildMarkRunStartedSql(this._factory.Connecion.ConnectionStringType, this._tableRef))
        {
            Parameters = parameters,
        };

        await this._executor
            .ExecuteAsync(request, this._factory, this.Logger, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task<int> AdvanceAsync(
        LoggingJobCheckpointKey key,
        long expectedVersion,
        LoggingJobCheckpointPosition position,
        string? fireInstanceId,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(position);

        DynamicParameters parameters = BuildKeyParameters(key);
        parameters.Add("ExpectedVersion", expectedVersion);
        parameters.Add("PositionId", position.Id);
        parameters.Add("PositionOn", position.On);
        parameters.Add("PositionText", position.Text);
        parameters.Add("FireInstanceId", fireInstanceId);
        parameters.Add("NowUtc", nowUtc);

        var request = new DataExecutorRequest(BuildAdvanceSql(this._tableRef))
        {
            Parameters = parameters,
        };

        return this._executor.ExecuteAsync(request, this._factory, this.Logger, cancellationToken);
    }

    #endregion Public Methods

    #region Internal Methods

    /// <summary>
    /// Builds the compare-and-set UPDATE that advances a checkpoint.
    /// </summary>
    /// <param name="tableRef">The (optionally schema-qualified) table name.</param>
    /// <returns>The UPDATE statement, identical for MySQL/MariaDB and SQLite.</returns>
    /// <remarks>
    /// <para>
    /// MariaDB evaluates single-table UPDATE assignments left to right, so
    /// <c>previous_position_*</c> is assigned BEFORE <c>position_*</c>; the
    /// other order would copy the new position into both. SQLite evaluates
    /// every assignment against the old row, so the order is safe there too.
    /// </para>
    /// <para>
    /// <c>version = version + 1</c> always changes the row, so the rows-matched
    /// count MySqlConnector reports by default equals the rows-changed count.
    /// </para>
    /// <para>Exposed to <c>Roadbed.Test.Unit</c> for SQL-shape verification.</para>
    /// </remarks>
    internal static string BuildAdvanceSql(string tableRef)
    {
        return $@"
            UPDATE {tableRef}
            SET
                 previous_position_id         = position_id
                ,previous_position_on         = position_on
                ,previous_position_text       = position_text
                ,position_id                  = @PositionId
                ,position_on                  = @PositionOn
                ,position_text                = @PositionText
                ,version                      = version + 1
                ,advanced_by_fire_instance_id = @FireInstanceId
                ,advanced_on                  = @NowUtc
                ,last_modified_on             = @NowUtc{KeyWhereClause}
              AND version        = @ExpectedVersion
            ;";
    }

    /// <summary>
    /// Builds the statement that inserts an empty checkpoint row when none exists.
    /// </summary>
    /// <param name="connectionStringType">The provider the statement targets.</param>
    /// <param name="tableRef">The (optionally schema-qualified) table name.</param>
    /// <returns>The provider-specific INSERT statement.</returns>
    /// <remarks>
    /// <para>
    /// MySQL/MariaDB has no <c>ON CONFLICT DO NOTHING</c>; the no-op
    /// <c>ON DUPLICATE KEY UPDATE application = application</c> leaves an
    /// existing row (and its <c>last_modified_on</c>) unchanged. Neither form
    /// uses <c>INSERT IGNORE</c> / <c>INSERT OR IGNORE</c>, which would also hide
    /// truncation or NOT NULL failures instead of throwing.
    /// </para>
    /// <para>Exposed to <c>Roadbed.Test.Unit</c> for SQL-shape verification.</para>
    /// </remarks>
    internal static string BuildEnsureExistsSql(
        DataConnectionStringType connectionStringType,
        string tableRef)
    {
        string conflictClause = connectionStringType == DataConnectionStringType.MySQL
            ? "ON DUPLICATE KEY UPDATE application = application"
            : "ON CONFLICT (application, environment, job_group, job_name, checkpoint_key) DO NOTHING";

        return $@"
            INSERT INTO {tableRef}
            (
                 application
                ,environment
                ,job_group
                ,job_name
                ,checkpoint_key
                ,created_on
                ,last_modified_on
            )
            VALUES
            (
                 @Application
                ,@Environment
                ,@JobGroup
                ,@JobName
                ,@CheckpointKey
                ,@NowUtc
                ,@NowUtc
            )
            {conflictClause}
            ;";
    }

    /// <summary>
    /// Builds the upsert that records a run start without moving the position.
    /// </summary>
    /// <param name="connectionStringType">The provider the statement targets.</param>
    /// <param name="tableRef">The (optionally schema-qualified) table name.</param>
    /// <returns>The provider-specific upsert statement.</returns>
    /// <remarks>
    /// The update branch sets only <c>last_run_started_on</c> and
    /// <c>last_modified_on</c>. It must never touch <c>version</c>: a run that
    /// records its start and then advances from the version it read would
    /// otherwise lose its own compare-and-set.
    /// <para>Exposed to <c>Roadbed.Test.Unit</c> for SQL-shape verification.</para>
    /// </remarks>
    internal static string BuildMarkRunStartedSql(
        DataConnectionStringType connectionStringType,
        string tableRef)
    {
        string conflictClause = connectionStringType == DataConnectionStringType.MySQL
            ? @"ON DUPLICATE KEY UPDATE
                 last_run_started_on = @NowUtc
                ,last_modified_on    = @NowUtc"
            : @"ON CONFLICT (application, environment, job_group, job_name, checkpoint_key) DO UPDATE SET
                 last_run_started_on = excluded.last_run_started_on
                ,last_modified_on    = excluded.last_modified_on";

        return $@"
            INSERT INTO {tableRef}
            (
                 application
                ,environment
                ,job_group
                ,job_name
                ,checkpoint_key
                ,last_run_started_on
                ,created_on
                ,last_modified_on
            )
            VALUES
            (
                 @Application
                ,@Environment
                ,@JobGroup
                ,@JobName
                ,@CheckpointKey
                ,@NowUtc
                ,@NowUtc
                ,@NowUtc
            )
            {conflictClause}
            ;";
    }

    #endregion Internal Methods

    #region Private Methods

    /// <summary>
    /// Re-attaches <see cref="DateTimeKind.Utc"/> to a nullable UTC column value.
    /// </summary>
    /// <param name="value">The value as read.</param>
    /// <returns>The same instant marked UTC, or <c>null</c>.</returns>
    private static DateTime? AsUtc(DateTime? value)
    {
        return value.HasValue
            ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
            : null;
    }

    /// <summary>
    /// Builds the parameter set shared by every statement: the five key columns.
    /// </summary>
    /// <param name="key">The row's primary key.</param>
    /// <returns>A parameter bag the caller can extend.</returns>
    private static DynamicParameters BuildKeyParameters(LoggingJobCheckpointKey key)
    {
        var parameters = new DynamicParameters();
        parameters.Add("Application", key.Application);
        parameters.Add("Environment", key.Environment);
        parameters.Add("JobGroup", key.JobGroup);
        parameters.Add("JobName", key.JobName);
        parameters.Add("CheckpointKey", key.CheckpointKey);
        return parameters;
    }

    #endregion Private Methods
}
