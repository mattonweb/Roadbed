namespace Roadbed.Net;

/// <summary>
/// Authentication used in the HttpClient.
/// </summary>
public class NetHttpAuthentication
{
    #region Public Properties

    /// <summary>
    /// Gets or sets the type authentication used in the HttpClient.
    /// </summary>
    public NetHttpAuthenticationType AuthenticationType
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the authentication value used in the HttpClient.
    /// </summary>
    public string? Value
    {
        get;
        set;
    }

    #endregion Public Properties
}