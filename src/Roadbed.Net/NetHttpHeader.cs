namespace Roadbed.Net;

/// <summary>
/// Http Header used in the HttpClient.
/// </summary>
public record NetHttpHeader
{
    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="NetHttpHeader"/> class.
    /// </summary>
    public NetHttpHeader()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NetHttpHeader"/> class.
    /// </summary>
    /// <param name="name">Http Header Name.</param>
    /// <param name="value">Http Header Value.</param>
    public NetHttpHeader(string name, string value)
    {
        this.Name = name;
        this.Value = value;
    }

    #endregion Public Constructors

    #region Public Properties

    /// <summary>
    /// Gets or sets the Http Header Name.
    /// </summary>
    public string? Name
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the Http Header Value.
    /// </summary>
    public string? Value
    {
        get;
        set;
    }

    #endregion Public Properties
}