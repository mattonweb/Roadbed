namespace Roadbed.Net;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Contract for making HTTP requests with retry and backoff support.
/// </summary>
/// <remarks>
/// <para>
/// Inject <see cref="INetHttpClient"/> via constructor injection in repository
/// implementations. The concrete <see cref="NetHttpClient"/> is registered
/// automatically by <see cref="Installers.InstallNetHttpClient"/>.
/// </para>
/// </remarks>
public interface INetHttpClient
{
    /// <summary>
    /// Make an HTTP request with automatic retry and backoff.
    /// </summary>
    /// <typeparam name="T">
    /// Type to deserialize the response body into. Use <see cref="string"/> for raw
    /// response body, or a DTO type for automatic JSON deserialization.
    /// </typeparam>
    /// <param name="request">API request configuration.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>HTTP response wrapping the deserialized result or error details.</returns>
    /// <exception cref="System.ArgumentNullException">
    /// <paramref name="request"/> is <c>null</c>.
    /// </exception>
    Task<NetHttpResponse<T>> MakeHttpRequestAsync<T>(
        NetHttpRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stream an HTTP response body to a local file with automatic retry and
    /// backoff. The body is never buffered in memory: it is copied to disk as it
    /// arrives, and a mid-transfer failure restarts the whole attempt.
    /// </summary>
    /// <param name="request">Download configuration, including the destination path.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// HTTP response wrapping a <see cref="NetHttpDownloadResult"/> (file path,
    /// bytes written, content type, and optional SHA-256) on success, or error
    /// details on failure.
    /// </returns>
    /// <exception cref="System.ArgumentNullException">
    /// <paramref name="request"/> is <c>null</c>.
    /// </exception>
    Task<NetHttpResponse<NetHttpDownloadResult>> DownloadFileAsync(
        NetHttpDownloadRequest request,
        CancellationToken cancellationToken = default);
}