namespace Roadbed.Logging;

/// <summary>
/// Opt-in contract for a scheduled job that keeps durable checkpoints in the
/// <c>job_checkpoint</c> table: the position its next run starts from.
/// </summary>
/// <remarks>
/// <para>
/// A Roadbed.Scheduling job opts in by inheriting <c>BaseSchedulingJob&lt;T&gt;</c>
/// as usual and also implementing this interface. <see cref="Name"/> is already
/// satisfied by <c>BaseSchedulingJob&lt;T&gt;.Name</c>; the job adds one line,
/// <c>public string JobGroup =&gt; this.Schedule.GroupName;</c>. Together the two
/// values are the job's Quartz <c>JobKey</c>, so checkpoint rows line up with
/// Quartz's own rows. Jobs that do not need a checkpoint change nothing.
/// </para>
/// <para>
/// The interface declares identity only. The operations are extension methods
/// in <see cref="LoggingCheckpointedJobExtensions"/>, so a job calls
/// <c>this.ReadCheckpointAsync(...)</c>,
/// <c>this.MarkCheckpointRunStartedAsync(...)</c> and
/// <c>this.TryAdvanceCheckpointAsync(...)</c> from inside its
/// <c>ExecuteAsync</c>.
/// </para>
/// <para>
/// Unlike the framework marker interfaces (<c>ISchedulingJob</c>,
/// <c>IDataConnectionFactory</c>), this interface has no base class: implement it
/// directly.
/// </para>
/// </remarks>
public interface ILoggingCheckpointedJob
{
    #region Public Properties

    /// <summary>
    /// Gets the job name: the <c>Name</c> half of the job's Quartz <c>JobKey</c>.
    /// </summary>
    /// <remarks>
    /// Stored in <c>job_checkpoint.job_name</c> (at most 200 characters).
    /// <c>BaseSchedulingJob&lt;T&gt;.Name</c> satisfies this member.
    /// </remarks>
    string Name { get; }

    /// <summary>
    /// Gets the job group: the <c>Group</c> half of the job's Quartz <c>JobKey</c>.
    /// </summary>
    /// <remarks>
    /// Stored in <c>job_checkpoint.job_group</c> (at most 200 characters).
    /// Implement it as <c>=&gt; this.Schedule.GroupName</c> so the value always
    /// equals the group Roadbed.Scheduling registers the job under.
    /// </remarks>
    string JobGroup { get; }

    #endregion Public Properties
}
