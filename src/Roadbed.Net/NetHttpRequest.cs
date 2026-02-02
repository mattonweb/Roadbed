namespace Roadbed.Net;

using System;
using System.Collections.Generic;

/// <summary>
/// Http Request entity.
/// </summary>
public class NetHttpRequest
{
    #region Private Fields

    /// <summary>
    /// Default Timeout to use the HttpClient.
    /// </summary>
    private const int DefaultTimeoutInSeconds = 15;

    #endregion Private Fields

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="NetHttpRequest"/> class.
    /// </summary>
    public NetHttpRequest()
    {
        // Set Defaults
        this.Method = HttpMethod.Get;
        this.EnableCompression = true;
        this.TimeoutInSecondsPerAttempt = DefaultTimeoutInSeconds;
        this.RetryPattern = new NetHttpRetryPattern()
        {
            MaxAttempts = 3,
            DelayMultiplierInSeconds = 5,
        };

        // Initalize
        this.HttpHeaders = new List<NetHttpHeader>();
    }

    #endregion Public Constructors

    #region Public Properties

    /// <summary>
    /// Gets or sets the authentication used in the HttpClient.
    /// </summary>
    public NetHttpAuthentication? Authentication
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the content used in the in the HttpClient.
    /// </summary>
    public HttpContent? Content
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the compression should be used with the HttpClient.
    /// </summary>
    public bool EnableCompression
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the url used in the in the HttpClient.
    /// </summary>
    public Uri? HttpEndPoint
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the Http Headers used in the in the HttpClient.
    /// </summary>
    public List<NetHttpHeader> HttpHeaders
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the Http Method used in the in the HttpClient.
    /// </summary>
    public HttpMethod Method
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the authentication used in the HttpClient.
    /// </summary>
    public NetHttpRetryPattern RetryPattern
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the timeout used in the in the HttpClient.
    /// </summary>
    public int TimeoutInSecondsPerAttempt
    {
        get;
        set;
    }

    #endregion Public Properties
}