namespace Roadbed.Net;

/// <summary>
/// Outcome payload for a successful <c>DownloadFileAsync</c> call.
/// </summary>
public sealed class NetHttpDownloadResult
{
    #region Public Properties

    /// <summary>
    /// Gets the local path the body was written to.
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// Gets the number of bytes written to <see cref="FilePath"/>.
    /// </summary>
    public long BytesWritten { get; init; }

    /// <summary>
    /// Gets the response's <c>Content-Type</c>, when the server supplied one.
    /// </summary>
    public string? ContentType { get; init; }

    /// <summary>
    /// Gets the lowercase hex SHA-256 of the downloaded bytes, or <c>null</c>
    /// when <see cref="NetHttpDownloadRequest.ComputeContentHash"/> was disabled.
    /// </summary>
    public string? ContentSha256 { get; init; }

    #endregion Public Properties
}
