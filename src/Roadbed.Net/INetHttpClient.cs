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
}