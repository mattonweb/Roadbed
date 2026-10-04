namespace Roadbed.Logging;

using System;
using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// Represents a row in the <c>job_checkpoint</c> table: the position a
/// checkpointed job's next run starts from.
/// </summary>
/// <remarks>
/// <para>
/// The primary key is (<see cref="Application"/>, <see cref="Environment"/>,
/// <see cref="JobGroup"/>, <see cref="JobName"/>, <see cref="CheckpointKey"/>).
/// The table is control state: it is never pruned and never partitioned.
/// </para>
/// <para>
/// <see cref="LastRunStartedOn"/>, <see cref="AdvancedOn"/>,
/// <see cref="CreatedOn"/> and <see cref="LastModifiedOn"/> are UTC and are
/// returned with <see cref="DateTimeKind.Utc"/>. <see cref="PositionOn"/> and
/// <see cref="PreviousPositionOn"/> are returned exactly as the job stored them.
/// </para>
/// </remarks>
public sealed class LoggingJobCheckpoint
{
    #region Public Properties

    /// <summary>
    /// Gets or sets the application that owns the checkpoint (<c>LoggingOptions.Application</c>).
    /// </summary>
    [Column("application")]
    public string Application { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the deployment environment that owns the checkpoint (<c>LoggingOptions.Environment</c>).
    /// </summary>
    [Column("environment")]
    public string Environment { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Quartz job group.
    /// </summary>
    [Column("job_group")]
    public string JobGroup { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Quartz job name.
    /// </summary>
    [Column("job_name")]
    public string JobName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of this checkpoint within the job; empty for a job's single checkpoint.
    /// </summary>
    [Column("checkpoint_key")]
    public string CheckpointKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the highest numeric id processed, or <c>null</c> when the job does not use an id cursor.
    /// </summary>
    [Column("position_id")]
    public long? PositionId { get; set; }

    /// <summary>
    /// Gets or sets the latest timestamp processed, or <c>null</c> when the job does not use a time cursor.
    /// </summary>
    [Column("position_on")]
    public DateTime? PositionOn { get; set; }

    /// <summary>
    /// Gets or sets the text cursor, or <c>null</c> when the job does not use one.
    /// </summary>
    [Column("position_text")]
    public string? PositionText { get; set; }

    /// <summary>
    /// Gets or sets the id position the last advance started from.
    /// </summary>
    [Column("previous_position_id")]
    public long? PreviousPositionId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp position the last advance started from.
    /// </summary>
    [Column("previous_position_on")]
    public DateTime? PreviousPositionOn { get; set; }

    /// <summary>
    /// Gets or sets the text position the last advance started from.
    /// </summary>
    [Column("previous_position_text")]
    public string? PreviousPositionText { get; set; }

    /// <summary>
    /// Gets or sets the compare-and-set token. It starts at 0 and increases by
    /// one each time the position advances.
    /// </summary>
    /// <remarks>
    /// Pass this value as the expected version when advancing. Recording a run
    /// start does not change it.
    /// </remarks>
    [Column("version")]
    public long Version { get; set; }

    /// <summary>
    /// Gets or sets the Quartz fire instance id of the run that last advanced the position.
    /// </summary>
    /// <remarks>
    /// Matches <c>activity.fire_instance_id</c> of that run's activity row, when it wrote one.
    /// </remarks>
    [Column("advanced_by_fire_instance_id")]
    public string? AdvancedByFireInstanceId { get; set; }

    /// <summary>
    /// Gets or sets when a run last recorded a start (UTC), whether or not it advanced the position.
    /// </summary>
    [Column("last_run_started_on")]
    public DateTime? LastRunStartedOn { get; set; }

    /// <summary>
    /// Gets or sets when the position last moved (UTC), or <c>null</c> when it never has.
    /// </summary>
    [Column("advanced_on")]
    public DateTime? AdvancedOn { get; set; }

    /// <summary>
    /// Gets or sets when the row was created (UTC).
    /// </summary>
    [Column("created_on")]
    public DateTime CreatedOn { get; set; }

    /// <summary>
    /// Gets or sets when the row last changed (UTC).
    /// </summary>
    [Column("last_modified_on")]
    public DateTime LastModifiedOn { get; set; }

    #endregion Public Properties
}
