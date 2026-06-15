namespace Roadbed.Net;

/// <summary>
/// Request to stream an HTTP response body to a local file. Inherits the
/// endpoint, headers, authentication, retry pattern, and compression settings
/// of <see cref="NetHttpRequest"/>; adds the destination and download-specific
/// options.
/// </summary>
public class NetHttpDownloadRequest : NetHttpRequest
{
    #region Private Fields

    /// <summary>
    /// Default per-attempt timeout for a download. Much larger than the base
    /// request default because a healthy large-file transfer routinely runs
    /// longer than a JSON call.
    /// </summary>
    private const int DefaultDownloadTimeoutInSeconds = 300;

    #endregion Private Fields

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="NetHttpDownloadRequest"/> class.
    /// </summary>
    public NetHttpDownloadRequest()
    {
        this.TimeoutInSecondsPerAttempt = DefaultDownloadTimeoutInSeconds;
    }

    #endregion Public Constructors

    #region Public Properties

    /// <summary>
    /// Gets or sets the local file path the response body is written to.
    /// </summary>
    public string DestinationPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether an existing file at
    /// <see cref="DestinationPath"/> may be overwritten. When <c>false</c> and
    /// the file already exists, the download fails without contacting the server.
    /// </summary>
    public bool Overwrite { get; set; } = true;

    /// <summary>
    /// Gets or sets the copy buffer size in bytes (matches
    /// <see cref="System.IO.Stream.CopyToAsync(System.IO.Stream, int)"/>'s default).
    /// </summary>
    public int BufferSizeBytes { get; set; } = 81920;

    /// <summary>
    /// Gets or sets a value indicating whether a SHA-256 of the downloaded bytes
    /// is computed during the single copy pass and returned on
    /// <see cref="NetHttpDownloadResult.ContentSha256"/>. The hash is the
    /// provenance anchor for live-fetched files.
    /// </summary>
    public bool ComputeContentHash { get; set; } = true;

    #endregion Public Properties
}
