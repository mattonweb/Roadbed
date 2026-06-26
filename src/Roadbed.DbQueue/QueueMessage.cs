namespace Roadbed.DbQueue;

using System;

/// <summary>
/// A single claimed queue message handed to a
/// <see cref="QueueMessageHandler{T}"/> by
/// <see cref="QueueProcessor{T}.ProcessBatchAsync"/>.
/// </summary>
/// <typeparam name="T">The deserialized payload type.</typeparam>
public sealed class QueueMessage<T>
{
    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="QueueMessage{T}"/> class.
    /// </summary>
    /// <param name="id">Internal auto-increment primary key from the message table; defines FIFO order.</param>
    /// <param name="externalId">Shareable UUIDv7 handle minted on enqueue.</param>
    /// <param name="createdOn">UTC timestamp the row was stamped with on enqueue.</param>
    /// <param name="payload">The deserialized payload.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="payload"/> or <paramref name="externalId"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="externalId"/> is whitespace.</exception>
    public QueueMessage(long id, string externalId, DateTime createdOn, T payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalId);
        ArgumentNullException.ThrowIfNull(payload);

        this.Id = id;
        this.ExternalId = externalId;
        this.CreatedOn = createdOn;
        this.Payload = payload;
    }

    #endregion Public Constructors

    #region Public Properties

    /// <summary>
    /// Gets the UTC timestamp the row was stamped with on enqueue.
    /// </summary>
    public DateTime CreatedOn { get; }

    /// <summary>
    /// Gets the shareable UUIDv7 external identifier (36-character "D" form).
    /// </summary>
    /// <remarks>
    /// This is the handle the enqueuer can correlate against and the value
    /// the library writes into log scopes when a handler throws.
    /// </remarks>
    public string ExternalId { get; }

    /// <summary>
    /// Gets the internal auto-increment row id. Used as the FIFO sort key and
    /// as the logical reference written to <c>queue_processed_{name}.fk_queue_id</c>.
    /// </summary>
    public long Id { get; }

    /// <summary>
    /// Gets the deserialized payload.
    /// </summary>
    public T Payload { get; }

    #endregion Public Properties
}
