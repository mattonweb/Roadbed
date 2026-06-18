/*
 * The namespace Roadbed.Messaging.Entities was removed on purpose and replaced with Roadbed.Messaging so that no additional using statements are required.
 */

namespace Roadbed.Messaging;

using System.Text.Json.Serialization;

/// <summary>
/// Base Message.
/// </summary>
/// <typeparam name="T">Type of data in the message payload.</typeparam>
public abstract class BaseMessagingMessage<T>
{
    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseMessagingMessage{T}"/> class with default
    /// values suitable for deserialization.
    /// </summary>
    /// <remarks>
    /// Required so that <see cref="System.Text.Json.JsonSerializer.Deserialize{T}(string, System.Text.Json.JsonSerializerOptions)"/> can
    /// instantiate concrete subclasses without ambiguity over which parameterized constructor to use.
    /// <see cref="Publisher"/> is initialized to <see langword="null"/> via the null-forgiving operator;
    /// the deserializer is expected to populate it from the JSON immediately after construction.
    /// Code paths that do not deserialize must use one of the parameterized constructors instead.
    /// </remarks>
    protected BaseMessagingMessage()
    {
        this.Publisher = null!;
        this.Identifier = Ulid.NewUlid().ToString();
        this.SourceCreatedOn = DateTimeOffset.UtcNow;
        this.CreatedOn = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseMessagingMessage{T}"/> class.
    /// </summary>
    /// <param name="publisher">Publisher for the message.</param>
    /// <remarks>
    /// The identifier is generated as a new ULID.
    /// </remarks>
    protected BaseMessagingMessage(MessagingPublisher publisher)
    {
        this.Publisher = publisher;
        this.Identifier = Ulid.NewUlid().ToString();
        this.SourceCreatedOn = DateTimeOffset.UtcNow;
        this.CreatedOn = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseMessagingMessage{T}"/> class.
    /// </summary>
    /// <param name="publisher">Publisher for the message.</param>
    /// <param name="typeCodename">Codename indicating the type of message.</param>
    /// <remarks>
    /// The identifier is generated as a new ULID.
    /// </remarks>
    protected BaseMessagingMessage(MessagingPublisher publisher, string typeCodename)
    {
        this.Publisher = publisher;
        this.MessageTypeCodename = typeCodename;
        this.Identifier = Ulid.NewUlid().ToString();
        this.SourceCreatedOn = DateTimeOffset.UtcNow;
        this.CreatedOn = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseMessagingMessage{T}"/> class.
    /// </summary>
    /// <param name="publisher">Publisher for the message.</param>
    /// <param name="typeCodename">Codename indicating the type of message.</param>
    /// <param name="identifier">Unique Identifer of the message publisher.</param>
    /// <param name="data">Message payload.</param>
    protected BaseMessagingMessage(MessagingPublisher publisher, string typeCodename, string identifier, T data)
        : this(publisher)
    {
        this.Publisher = publisher;
        this.MessageTypeCodename = typeCodename;
        this.Identifier = identifier;
        this.Data = data;
        this.SourceCreatedOn = DateTimeOffset.UtcNow;
        this.CreatedOn = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseMessagingMessage{T}"/> class.
    /// </summary>
    /// <param name="publisher">Publisher for the message.</param>
    /// <param name="typeCodename">Codename indicating the type of message.</param>
    /// <param name="identifier">Unique Identifer of the message publisher.</param>
    /// <param name="data">Message payload.</param>
    /// <param name="createdOn">Message created on according to the source.</param>
    protected BaseMessagingMessage(MessagingPublisher publisher, string typeCodename, string identifier, T data, DateTimeOffset createdOn)
    {
        this.Publisher = publisher;
        this.MessageTypeCodename = typeCodename;
        this.Identifier = identifier;
        this.Data = data;
        this.SourceCreatedOn = createdOn;
        this.CreatedOn = DateTimeOffset.UtcNow;
    }

    #endregion Public Constructors

    #region Public Properties

    /// <summary>
    /// Gets the time the message was created.
    /// </summary>
    /// <remarks>
    /// <see cref="JsonIncludeAttribute"/> is required so System.Text.Json
    /// will use the <c>internal</c> setter during deserialization. Without
    /// it, STJ skips non-public accessors and the parameterless
    /// constructor's freshly generated value would silently survive the
    /// round-trip.
    /// </remarks>
    [JsonPropertyName("message_create_on")]
    [JsonInclude]
    public DateTimeOffset? CreatedOn
    {
        get;
        internal set;
    }

    /// <summary>
    /// Gets or sets the object in the message.
    /// </summary>
    [JsonPropertyName("data")]
    public T? Data
    {
        get;
        set;
    }

    /// <summary>
    /// Gets the identifier for the message.
    /// </summary>
    /// <remarks>
    /// <see cref="JsonIncludeAttribute"/> is required so System.Text.Json
    /// will use the <c>internal</c> setter during deserialization. Without
    /// it, STJ skips non-public accessors and the parameterless
    /// constructor's freshly generated ULID would silently survive the
    /// round-trip.
    /// </remarks>
    [JsonPropertyName("message_identifier")]
    [JsonInclude]
    public string? Identifier
    {
        get;
        internal set;
    }

    /// <summary>
    /// Gets or sets the type of message.
    /// </summary>
    [JsonPropertyName("message_type")]
    public string? MessageTypeCodename
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the publisher for the message.
    /// </summary>
    [JsonPropertyName("publisher")]
    public MessagingPublisher Publisher
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the time the message was created according to the source.
    /// </summary>
    [JsonPropertyName("source_create_on")]
    public DateTimeOffset? SourceCreatedOn
    {
        get;
        set;
    }

    #endregion Public Properties
}