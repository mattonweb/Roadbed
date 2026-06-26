namespace Roadbed.DbQueue;

/// <summary>
/// Summary counts returned from
/// <see cref="QueueProcessor{T}.ProcessBatchAsync"/>.
/// </summary>
/// <remarks>
/// Use these to populate the scheduled job's <c>Context.Result</c> line and
/// any metrics counters. A non-zero <see cref="Failed"/> count means at least
/// one handler threw — the host is responsible for alerting on it, because
/// the library does <strong>not</strong> auto-retry.
/// </remarks>
public sealed class QueueProcessResult
{
    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="QueueProcessResult"/> class.
    /// </summary>
    /// <param name="attempted">Total messages drawn from the queue this batch.</param>
    /// <param name="succeeded">Subset of <paramref name="attempted"/> whose handler returned normally.</param>
    /// <param name="failed">Subset of <paramref name="attempted"/> whose handler threw.</param>
    public QueueProcessResult(int attempted, int succeeded, int failed)
    {
        this.Attempted = attempted;
        this.Succeeded = succeeded;
        this.Failed = failed;
    }

    #endregion Public Constructors

    #region Public Properties

    /// <summary>
    /// Gets the total number of messages claimed and dispatched this batch.
    /// </summary>
    public int Attempted { get; }

    /// <summary>
    /// Gets the number of dispatched messages whose handler threw. The library
    /// does not auto-retry these; the host must alert on them.
    /// </summary>
    public int Failed { get; }

    /// <summary>
    /// Gets the number of dispatched messages whose handler returned normally.
    /// </summary>
    public int Succeeded { get; }

    #endregion Public Properties
}
