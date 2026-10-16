namespace Roadbed.Logging;

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.Extensions.Logging;

/// <summary>
/// Default implementation of <see cref="ILoggingJobCheckpointService"/>.
/// </summary>
/// <remarks>
/// <para>
/// Validates every key part before touching the database: SQLite does not
/// enforce declared column widths, and a clear <see cref="ArgumentException"/>
/// beats a provider-specific truncation error. Values with leading or trailing
/// whitespace are rejected because MariaDB's <c>utf8mb4_bin</c> is a PAD SPACE
/// collation that ignores trailing spaces when comparing keys.
/// </para>
/// <para>
/// Nothing here catches a database exception. A checkpoint read that failed
/// silently would look like "never run" and trigger a baseline or a full
/// backfill, so every failure propagates.
/// </para>
/// </remarks>
public sealed class LoggingJobCheckpointService
    : BaseClassWithLogging,
      ILoggingJobCheckpointService
{
    #region Internal Fields

    /// <summary>
    /// Width of <c>job_checkpoint.application</c>.
    /// </summary>
    internal const int MaxApplicationLength = 100;

    /// <summary>
    /// Width of <c>job_checkpoint.environment</c>.
    /// </summary>
    internal const int MaxEnvironmentLength = 20;

    /// <summary>
    /// Width of <c>job_checkpoint.job_group</c> and <c>job_checkpoint.job_name</c>.
    /// </summary>
    internal const int MaxJobIdentityLength = 200;

    /// <summary>
    /// Width of <c>job_checkpoint.checkpoint_key</c>.
    /// </summary>
    internal const int MaxCheckpointKeyLength = 100;

    /// <summary>
    /// Width of <c>job_checkpoint.position_text</c>.
    /// </summary>
    internal const int MaxPositionTextLength = 1024;

    /// <summary>
    /// Width of <c>job_checkpoint.advanced_by_fire_instance_id</c>; matches
    /// Quartz.NET's own <c>QRTZ_FIRED_TRIGGERS.ENTRY_ID</c>.
    /// </summary>
    internal const int MaxFireInstanceIdLength = 140;

    #endregion Internal Fields

    #region Private Fields

    private readonly ILoggingJobCheckpointRepository _repository;
    private readonly LoggingOptions _options;
    private readonly TimeProvider _timeProvider;

    #endregion Private Fields

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingJobCheckpointService"/> class.
    /// </summary>
    /// <param name="logger">Represents a type used to perform logging.</param>
    /// <remarks>
    /// Resolves the repository and <see cref="LoggingOptions"/> from
    /// <see cref="ServiceLocator"/>, and <see cref="TimeProvider"/> when one has
    /// been registered (e.g. a test fake), falling back to
    /// <see cref="TimeProvider.System"/>.
    /// </remarks>
    public LoggingJobCheckpointService(
        ILogger<LoggingJobCheckpointService> logger)
        : this(
            ServiceLocator.GetService<ILoggingJobCheckpointRepository>(),
            ServiceLocator.GetService<LoggingOptions>(),
            ServiceLocator.TryGetService<TimeProvider>() ?? TimeProvider.System,
            logger)
    {
    }

    #endregion Public Constructors

    #region Internal Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingJobCheckpointService"/> class.
    /// </summary>
    /// <param name="repository">Repository for the <c>job_checkpoint</c> table.</param>
    /// <param name="options">Host-supplied logging options; supplies the application and environment key parts.</param>
    /// <param name="timeProvider">Clock source for every timestamp this service writes.</param>
    /// <param name="logger">Represents a type used to perform logging.</param>
    internal LoggingJobCheckpointService(
        ILoggingJobCheckpointRepository repository,
        LoggingOptions options,
        TimeProvider timeProvider,
        ILogger<LoggingJobCheckpointService> logger)
        : base(logger)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);

        this._repository = repository;
        this._options = options;
        this._timeProvider = timeProvider;
    }

    #endregion Internal Constructors

    #region Public Methods

    /// <inheritdoc/>
    public Task<LoggingJobCheckpoint?> ReadAsync(
        ILoggingCheckpointedJob job,
        string checkpointKey,
        CancellationToken cancellationToken = default)
    {
        LoggingJobCheckpointKey key = this.BuildKey(job, checkpointKey);

        return this._repository.ReadAsync(key, cancellationToken);
    }

    /// <inheritdoc/>
    public Task MarkRunStartedAsync(
        ILoggingCheckpointedJob job,
        string checkpointKey,
        CancellationToken cancellationToken = default)
    {
        LoggingJobCheckpointKey key = this.BuildKey(job, checkpointKey);
        ThrowIfInAmbientTransaction(nameof(this.MarkRunStartedAsync));

        return this._repository.MarkRunStartedAsync(
            key,
            this._timeProvider.GetUtcNow().UtcDateTime,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> TryAdvanceAsync(
        ILoggingCheckpointedJob job,
        string checkpointKey,
        long expectedVersion,
        LoggingJobCheckpointPosition position,
        string? fireInstanceId,
        CancellationToken cancellationToken = default)
    {
        LoggingJobCheckpointKey key = this.BuildKey(job, checkpointKey);
        ArgumentOutOfRangeException.ThrowIfNegative(expectedVersion);
        ValidatePosition(position);
        ValidateFireInstanceId(fireInstanceId);
        ThrowIfInAmbientTransaction(nameof(this.TryAdvanceAsync));

        LoggingJobCheckpointPosition stored = NormalizePosition(position);
        DateTime nowUtc = this._timeProvider.GetUtcNow().UtcDateTime;

        // A "never run" checkpoint has no row to compare against, so make sure
        // one exists (version 0) first. The compare-and-set below then decides
        // the winner for first and later advances alike.
        await this._repository
            .EnsureExistsAsync(key, nowUtc, cancellationToken)
            .ConfigureAwait(false);

        int changed = await this._repository
            .AdvanceAsync(key, expectedVersion, stored, fireInstanceId, nowUtc, cancellationToken)
            .ConfigureAwait(false);

        if (changed == 1)
        {
            return true;
        }

        if (changed > 1)
        {
            throw new InvalidOperationException(
                $"Advancing checkpoint '{key.JobGroup}.{key.JobName}' [{key.CheckpointKey}] changed {changed} rows. " +
                "job_checkpoint must have its five-column primary key; check the table against install_mysql.txt.");
        }

        // No row changed. Either another run advanced first, or this call's own
        // UPDATE committed and the executor retried after the acknowledgement
        // was lost (MySQL errors 2006 / 2013). Only the fire instance id can
        // tell the two apart.
        if (fireInstanceId is not null)
        {
            LoggingJobCheckpoint? current = await this._repository
                .ReadAsync(key, cancellationToken)
                .ConfigureAwait(false);

            if (IsAdvanceFromThisCall(current, expectedVersion, stored, fireInstanceId))
            {
                this.LogDebug(
                    "Checkpoint {JobGroup}.{JobName} [{CheckpointKey}] advance to version {Version} had already committed on an earlier attempt",
                    key.JobGroup,
                    key.JobName,
                    key.CheckpointKey,
                    expectedVersion + 1);

                return true;
            }
        }

        this.LogDebug(
            "Checkpoint {JobGroup}.{JobName} [{CheckpointKey}] was already advanced past version {ExpectedVersion} by another run",
            key.JobGroup,
            key.JobName,
            key.CheckpointKey,
            expectedVersion);

        return false;
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Decides whether a row read after a zero-row UPDATE is the result of this
    /// call's own advance.
    /// </summary>
    /// <param name="current">The row as re-read.</param>
    /// <param name="expectedVersion">The version this call advanced from.</param>
    /// <param name="stored">The position this call wrote.</param>
    /// <param name="fireInstanceId">The fire instance id this call wrote.</param>
    /// <returns><c>true</c> when the row shows exactly this call's advance.</returns>
    private static bool IsAdvanceFromThisCall(
        LoggingJobCheckpoint? current,
        long expectedVersion,
        LoggingJobCheckpointPosition stored,
        string fireInstanceId)
    {
        return current is not null
            && current.Version == expectedVersion + 1
            && string.Equals(current.AdvancedByFireInstanceId, fireInstanceId, StringComparison.Ordinal)
            && current.PositionId == stored.Id
            && current.PositionOn == stored.On
            && string.Equals(current.PositionText, stored.Text, StringComparison.Ordinal);
    }

    /// <summary>
    /// Returns <c>true</c> when <paramref name="value"/> can be stored in a key
    /// column of the given width.
    /// </summary>
    /// <param name="value">The candidate key part.</param>
    /// <param name="maxLength">The column width.</param>
    /// <param name="allowEmpty">Whether the empty string is a valid value.</param>
    /// <returns><c>true</c> when the value is non-null, within width, and has no leading or trailing whitespace.</returns>
    private static bool IsValidKeyPart(string? value, int maxLength, bool allowEmpty)
    {
        if (value is null)
        {
            return false;
        }

        if (value.Length == 0)
        {
            return allowEmpty;
        }

        return value.Length <= maxLength
            && !char.IsWhiteSpace(value[0])
            && !char.IsWhiteSpace(value[^1]);
    }

    /// <summary>
    /// Truncates the timestamp cursor to microsecond precision so the stored
    /// value is identical on MariaDB (<c>DATETIME(6)</c>) and SQLite.
    /// </summary>
    /// <param name="position">The caller's position.</param>
    /// <returns>The position exactly as it will be stored.</returns>
    private static LoggingJobCheckpointPosition NormalizePosition(LoggingJobCheckpointPosition position)
    {
        DateTime? on = position.On;

        if (on.HasValue)
        {
            long ticks = on.Value.Ticks;
            on = new DateTime(ticks - (ticks % TimeSpan.TicksPerMicrosecond), on.Value.Kind);
        }

        return new LoggingJobCheckpointPosition
        {
            Id = position.Id,
            On = on,
            Text = position.Text,
        };
    }

    /// <summary>
    /// Throws when an ambient <c>System.Transactions</c> transaction is active.
    /// </summary>
    /// <param name="operation">The operation name, for the exception message.</param>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="Transaction.Current"/> is set.</exception>
    /// <remarks>
    /// The logging database is reached on its own connection, so a checkpoint
    /// write can never be atomic with the job's work. Inside a
    /// <c>TransactionScope</c> it would either enlist that connection in the
    /// scope or commit ahead of the work the scope has not committed yet.
    /// </remarks>
    private static void ThrowIfInAmbientTransaction(string operation)
    {
        if (Transaction.Current is not null)
        {
            throw new InvalidOperationException(
                $"{operation} cannot run inside an ambient System.Transactions transaction. " +
                "Complete and dispose the TransactionScope that saves the job's work first, then advance the checkpoint.");
        }
    }

    /// <summary>
    /// Validates the optional Quartz fire instance id.
    /// </summary>
    /// <param name="fireInstanceId">The value to validate.</param>
    /// <exception cref="ArgumentException">Thrown when the value is blank or longer than 140 characters.</exception>
    private static void ValidateFireInstanceId(string? fireInstanceId)
    {
        if (fireInstanceId is not null
            && (string.IsNullOrWhiteSpace(fireInstanceId) || fireInstanceId.Length > MaxFireInstanceIdLength))
        {
            throw new ArgumentException(
                $"The fire instance id must be null or a non-blank value of at most {MaxFireInstanceIdLength} characters.",
                nameof(fireInstanceId));
        }
    }

    /// <summary>
    /// Validates the position a caller is advancing to.
    /// </summary>
    /// <param name="position">The position to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="position"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when no cursor value is set or the text cursor is too long.</exception>
    private static void ValidatePosition(LoggingJobCheckpointPosition position)
    {
        ArgumentNullException.ThrowIfNull(position);

        if (position.Id is null && position.On is null && position.Text is null)
        {
            throw new ArgumentException(
                "A checkpoint position needs at least one of Id, On or Text.",
                nameof(position));
        }

        if (position.Text is not null && position.Text.Length > MaxPositionTextLength)
        {
            throw new ArgumentException(
                $"The text cursor is longer than {MaxPositionTextLength} characters.",
                nameof(position));
        }
    }

    /// <summary>
    /// Validates the job identity, the checkpoint key and the configured
    /// application and environment, and assembles the row key.
    /// </summary>
    /// <param name="job">The job that owns the checkpoint.</param>
    /// <param name="checkpointKey">The checkpoint's name within the job.</param>
    /// <returns>The validated five-part key.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="job"/> or <paramref name="checkpointKey"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the job identity or checkpoint key is invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the configured application or environment is invalid.</exception>
    private LoggingJobCheckpointKey BuildKey(ILoggingCheckpointedJob job, string checkpointKey)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(checkpointKey);

        string jobGroup = job.JobGroup;
        string jobName = job.Name;

        if (!IsValidKeyPart(jobGroup, MaxJobIdentityLength, allowEmpty: false)
            || !IsValidKeyPart(jobName, MaxJobIdentityLength, allowEmpty: false))
        {
            throw new ArgumentException(
                $"The job's Name and JobGroup must each be non-blank, at most {MaxJobIdentityLength} characters, " +
                "and free of leading or trailing whitespace.",
                nameof(job));
        }

        if (!IsValidKeyPart(checkpointKey, MaxCheckpointKeyLength, allowEmpty: true))
        {
            throw new ArgumentException(
                $"The checkpoint key must be at most {MaxCheckpointKeyLength} characters " +
                "and free of leading or trailing whitespace. Use string.Empty for a job's single checkpoint.",
                nameof(checkpointKey));
        }

        string? application = this._options.Application;
        string? environment = this._options.Environment;

        if (!IsValidKeyPart(application, MaxApplicationLength, allowEmpty: false))
        {
            throw new InvalidOperationException(
                $"Job checkpoints require LoggingOptions.Application to be non-blank, at most {MaxApplicationLength} " +
                "characters, and free of leading or trailing whitespace.");
        }

        // Checkpoints are deliberately per environment. A blank environment
        // would let a misconfigured dev host and prod host share one row.
        if (!IsValidKeyPart(environment, MaxEnvironmentLength, allowEmpty: false))
        {
            throw new InvalidOperationException(
                $"Job checkpoints require LoggingOptions.Environment to be non-blank, at most {MaxEnvironmentLength} " +
                "characters, and free of leading or trailing whitespace.");
        }

        return new LoggingJobCheckpointKey(
            application,
            environment!,
            jobGroup,
            jobName,
            checkpointKey);
    }

    #endregion Private Methods
}
