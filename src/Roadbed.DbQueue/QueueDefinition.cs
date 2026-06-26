namespace Roadbed.DbQueue;

using System;
using Roadbed.Data;

/// <summary>
/// Binds a queue name to its payload type and the connection that reaches the
/// MySQL/MariaDB schema where its tables live.
/// </summary>
/// <typeparam name="T">The strongly-typed payload that <see cref="QueueProcessor{T}"/> serializes on enqueue and deserializes on claim.</typeparam>
/// <remarks>
/// <para>
/// <strong>Connection per queue.</strong> Each queue lives <em>in</em> the
/// business schema it serves — not a dedicated queue schema and not the
/// logging schema — so the connection factory is supplied per queue, not once
/// per assembly. The host passes the marker
/// <see cref="IDataConnectionFactory"/> (e.g.
/// <c>IFooSubscriptionsDatabaseFactory</c>) it registered for the matching
/// schema, and the queue's SQL is routed through that connection. Multiple
/// queues in the same host can therefore live in different databases without
/// the library managing any connection strings.
/// </para>
/// <para>
/// <strong>Validation up front.</strong> The constructor calls
/// <see cref="QueueNameValidator"/> before any SQL string can be built, so a
/// bad name is rejected with <see cref="ArgumentException"/> at host startup
/// rather than at the first <c>EnqueueAsync</c>/<c>ProcessBatchAsync</c>
/// call. Once validated, the queue name is interpolated into table identifier
/// suffixes (<c>queue_message_{name}</c> / <c>queue_processed_{name}</c>),
/// always backtick-wrapped as defense-in-depth.
/// </para>
/// </remarks>
public sealed class QueueDefinition<T>
{
    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="QueueDefinition{T}"/> class.
    /// </summary>
    /// <param name="queueName">
    /// Queue name, validated against the whitelist
    /// (<c>^[a-z0-9_]+$</c>, length 1..<see cref="QueueNameValidator.MaxLength"/>).
    /// </param>
    /// <param name="connectionFactory">
    /// Marker <see cref="IDataConnectionFactory"/> for the schema this queue
    /// lives in. Pass the per-schema marker the host registered (e.g.
    /// <c>IFooSubscriptionsDatabaseFactory</c>); each queue can therefore
    /// reach a different schema.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connectionFactory"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="queueName"/> is null, empty, whitespace, too long, or contains characters outside the whitelist.</exception>
    public QueueDefinition(string queueName, IDataConnectionFactory connectionFactory)
    {
        QueueNameValidator.Validate(queueName);
        ArgumentNullException.ThrowIfNull(connectionFactory);

        this.QueueName = queueName;
        this.ConnectionFactory = connectionFactory;
        this.MessageTableName = $"queue_message_{queueName}";
        this.ProcessedTableName = $"queue_processed_{queueName}";
    }

    #endregion Public Constructors

    #region Public Properties

    /// <summary>
    /// Gets the connection factory routed at the schema this queue lives in.
    /// </summary>
    public IDataConnectionFactory ConnectionFactory { get; }

    /// <summary>
    /// Gets the payload type this queue carries. Useful for diagnostics and
    /// for typed wrappers that want to log <c>PayloadType.Name</c> alongside
    /// the queue name without re-deriving it from generics.
    /// </summary>
    public Type PayloadType { get; } = typeof(T);

    /// <summary>
    /// Gets the unqualified message table name (<c>queue_message_{queueName}</c>).
    /// </summary>
    /// <remarks>
    /// Composed into SQL as a backtick-wrapped identifier by
    /// <see cref="QueueProcessor{T}"/>. The queue name itself has already
    /// been whitelist-validated.
    /// </remarks>
    public string MessageTableName { get; }

    /// <summary>
    /// Gets the unqualified processed-companion table name
    /// (<c>queue_processed_{queueName}</c>).
    /// </summary>
    /// <remarks>
    /// Composed into SQL as a backtick-wrapped identifier by
    /// <see cref="QueueProcessor{T}"/>. The queue name itself has already
    /// been whitelist-validated.
    /// </remarks>
    public string ProcessedTableName { get; }

    /// <summary>
    /// Gets the validated queue name.
    /// </summary>
    public string QueueName { get; }

    #endregion Public Properties
}
