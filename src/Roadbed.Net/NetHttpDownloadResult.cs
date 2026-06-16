namespace Roadbed.Net;

using System.Collections.Generic;

/// <summary>
/// Outcome payload for a <c>DownloadFileAsync</c> call. Returned on a successful
/// 2xx download and on a 304 <c>Not Modified</c> short-circuit (distinguished by
/// <see cref="NotModified"/>).
/// </summary>
public sealed class NetHttpDownloadResult
{
    #region Public Properties

    /// <summary>
    /// Gets the local path the body was written to. On a <see cref="NotModified"/>
    /// outcome the path is still the caller's requested destination, but no bytes
    /// were written and any pre-existing file at the path is untouched.
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// Gets the number of bytes written to <see cref="FilePath"/>. Always
    /// <c>0</c> on a <see cref="NotModified"/> outcome.
    /// </summary>
    public long BytesWritten { get; init; }

    /// <summary>
    /// Gets the response's <c>Content-Type</c>, when the server supplied one.
    /// </summary>
    public string? ContentType { get; init; }

    /// <summary>
    /// Gets the lowercase hex SHA-256 of the downloaded bytes, or <c>null</c>
    /// when <see cref="NetHttpDownloadRequest.ComputeContentHash"/> was disabled
    /// or when <see cref="NotModified"/> is <c>true</c> (no bytes were read).
    /// </summary>
    public string? ContentSha256 { get; init; }

    /// <summary>
    /// Gets a value indicating whether the server replied <c>304 Not Modified</c>
    /// in response to an <c>If-None-Match</c> / <c>If-Modified-Since</c> on the
    /// request. When <c>true</c>: no bytes were written, no <c>.part</c> file was
    /// created, and any pre-existing file at <see cref="FilePath"/> was left
    /// untouched. Callers should treat this as a "cached copy is still fresh"
    /// signal rather than as a failure.
    /// </summary>
    public bool NotModified { get; init; }

    /// <summary>
    /// Gets the response headers returned by the server. Same merged-and-flattened
    /// shape as <see cref="NetHttpResponse{T}.ResponseHeaders"/>: general headers
    /// (<c>ETag</c>, <c>Retry-After</c>, <c>RateLimit-*</c>, ...) plus content
    /// headers (<c>Last-Modified</c>, <c>Content-Type</c>, <c>Content-Length</c>),
    /// with one <see cref="NetHttpHeader"/> entry per value for multi-valued
    /// headers. Populated on success AND on a 304 outcome.
    /// </summary>
    public IReadOnlyList<NetHttpHeader> ResponseHeaders { get; init; } =
        System.Array.Empty<NetHttpHeader>();

    #endregion Public Properties
}
