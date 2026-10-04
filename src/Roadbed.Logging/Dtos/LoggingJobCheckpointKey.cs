namespace Roadbed.Logging;

/// <summary>
/// The five-part primary key of a <c>job_checkpoint</c> row, already validated
/// by <see cref="LoggingJobCheckpointService"/>.
/// </summary>
internal sealed class LoggingJobCheckpointKey
{
    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingJobCheckpointKey"/> class.
    /// </summary>
    /// <param name="application">The owning application (<c>LoggingOptions.Application</c>).</param>
    /// <param name="environment">The owning environment (<c>LoggingOptions.Environment</c>).</param>
    /// <param name="jobGroup">The Quartz job group.</param>
    /// <param name="jobName">The Quartz job name.</param>
    /// <param name="checkpointKey">The checkpoint's name within the job; empty for a job's single checkpoint.</param>
    public LoggingJobCheckpointKey(
        string application,
        string environment,
        string jobGroup,
        string jobName,
        string checkpointKey)
    {
        this.Application = application;
        this.Environment = environment;
        this.JobGroup = jobGroup;
        this.JobName = jobName;
        this.CheckpointKey = checkpointKey;
    }

    #endregion Public Constructors

    #region Public Properties

    /// <summary>
    /// Gets the owning application.
    /// </summary>
    public string Application { get; }

    /// <summary>
    /// Gets the owning environment.
    /// </summary>
    public string Environment { get; }

    /// <summary>
    /// Gets the Quartz job group.
    /// </summary>
    public string JobGroup { get; }

    /// <summary>
    /// Gets the Quartz job name.
    /// </summary>
    public string JobName { get; }

    /// <summary>
    /// Gets the checkpoint's name within the job; empty for a job's single checkpoint.
    /// </summary>
    public string CheckpointKey { get; }

    #endregion Public Properties
}
