namespace Roadbed.DbQueue;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Caller-supplied delegate that processes a single claimed
/// <see cref="QueueMessage{T}"/>.
/// </summary>
/// <typeparam name="T">The deserialized payload type the handler consumes.</typeparam>
/// <param name="message">The claimed message, with its FIFO id, UUIDv7 external id, UTC enqueue timestamp, and deserialized payload.</param>
/// <param name="cancellationToken">Token observed by <see cref="QueueProcessor{T}.ProcessBatchAsync"/>; the handler is expected to honor it.</param>
/// <returns>A task that completes when the message has been processed.</returns>
/// <remarks>
/// <para>
/// <strong>Outcome.</strong> Returning normally counts as success; throwing
/// counts as failure. <see cref="QueueProcessor{T}.ProcessBatchAsync"/> catches
/// the throw per message, records one
/// <c>queue_processed_{name}</c> row with
/// <c>is_processed_successfully = 0</c>, logs at Error, and continues the
/// batch. One bad message does not stop the rest.
/// </para>
/// <para>
/// <strong>Idempotency.</strong> A failed row is <em>never</em>
/// auto-retried. The only way to retry is for an operator to delete the
/// processed row externally, at which point the anti-join will re-claim the
/// original message — so handlers MUST be idempotent. The library does not
/// enforce this.
/// </para>
/// </remarks>
public delegate Task QueueMessageHandler<T>(
    QueueMessage<T> message,
    CancellationToken cancellationToken);
