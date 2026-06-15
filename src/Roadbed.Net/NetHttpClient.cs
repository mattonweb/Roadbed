namespace Roadbed.Net;

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

/// <summary>
/// Service for managing HttpClient operations using IHttpClientFactory.
/// </summary>
public class NetHttpClient
    : BaseClassWithLogging, INetHttpClient
{
    #region Private Fields

    /// <summary>
    /// Factory for creating HttpClient instances.
    /// </summary>
    private readonly IHttpClientFactory _httpClientFactory;

    #endregion Private Fields

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="NetHttpClient"/> class.
    /// </summary>
    /// <param name="httpClientFactory">Factory for creating HttpClient instances.</param>
    /// <param name="logger">Represents a type used to perform logging.</param>
    /// <exception cref="ArgumentNullException"><paramref name="httpClientFactory"/> is <c>null</c>.</exception>
    public NetHttpClient(
        IHttpClientFactory httpClientFactory,
        ILogger<NetHttpClient> logger)
        : base(logger)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);

        this._httpClientFactory = httpClientFactory;
    }

    #endregion Public Constructors

    #region Public Methods

    /// <inheritdoc/>
    public async Task<NetHttpResponse<T>> MakeHttpRequestAsync<T>(
        NetHttpRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        this.LogDebug(
            "HTTP {Method} {Endpoint} (Compression: {Compression})",
            request.Method,
            request.HttpEndPoint?.ToString() ?? "unknown",
            request.EnableCompression);

        // Response Container
        NetHttpResponse<T> response;

        try
        {
            HttpResponseMessage message =
                await this.ExecuteWithBackoffRetryAsync(
                    request,
                    HttpCompletionOption.ResponseContentRead,
                    onSuccessAsync: null,
                    cancellationToken);

            // Add the Data
            if (message.IsSuccessStatusCode &&
                (message.StatusCode != HttpStatusCode.NotFound))
            {
                // Grab the body of the response
                string responseBody = await message.Content.ReadAsStringAsync(cancellationToken);

                this.LogDebug(
                    "HTTP {StatusCode} from {Endpoint}",
                    (int)message.StatusCode,
                    request.HttpEndPoint?.ToString() ?? "unknown");

                // Return raw string or deserialize JSON based on type parameter
                if (typeof(T) == typeof(string))
                {
                    response = NetHttpResponse<T>.Success(
                        (int)message.StatusCode,
                        message.ReasonPhrase,
                        (T)(object)responseBody);
                }
                else
                {
                    try
                    {
                        T? deserialized = JsonConvert.DeserializeObject<T>(responseBody);

                        response = NetHttpResponse<T>.Success(
                            (int)message.StatusCode,
                            message.ReasonPhrase,
                            deserialized!);
                    }
                    catch (JsonException ex)
                    {
                        this.LogDebug(
                            ex,
                            "Failed to deserialize response from {Endpoint} to {Type}",
                            request.HttpEndPoint?.ToString() ?? "unknown",
                            typeof(T).Name);

                        response = NetHttpResponse<T>.Failure(
                            (int)message.StatusCode,
                            message.ReasonPhrase,
                            $"Failed to deserialize response to {typeof(T).Name}: {ex.Message}");
                    }
                }
            }
            else
            {
                response = NetHttpResponse<T>.Failure(
                        (int)message.StatusCode,
                        message.ReasonPhrase,
                        "Not a successful HTTP call. Status code indicates a failed HTTP Request.");
            }
        }
        catch (HttpRequestException ex) when (ex.InnerException is SocketException)
        {
            response = this.HandleHttpRequestWithSocketException<T>(ex, request);
        }
        catch (SocketException ex)
        {
            this.LogDebug(
                ex,
                "Socket error calling {Endpoint}",
                request.HttpEndPoint?.ToString() ?? "unknown");

            response = NetHttpResponse<T>.Failure(
                500,
                string.Concat(ex?.Message, " ", ex?.InnerException?.Message),
                "Not a successful HTTP call. An unknown error occurred with the socket.");
        }
        catch (Exception ex)
        {
            this.LogDebug(
                ex,
                "Unexpected error calling {Endpoint}",
                request.HttpEndPoint?.ToString() ?? "unknown");

            response = NetHttpResponse<T>.Failure(
                500,
                string.Concat(ex?.Message, " ", ex?.InnerException?.Message),
                "Not a successful HTTP call. An unknown error occurred.");
        }

        return response;
    }

    /// <inheritdoc/>
    public async Task<NetHttpResponse<NetHttpDownloadResult>> DownloadFileAsync(
        NetHttpDownloadRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.DestinationPath);

        this.LogDebug(
            "HTTP download {Endpoint} -> {Destination}",
            request.HttpEndPoint?.ToString() ?? "unknown",
            request.DestinationPath);

        if (!request.Overwrite && File.Exists(request.DestinationPath))
        {
            return NetHttpResponse<NetHttpDownloadResult>.Failure(
                409,
                "Conflict",
                $"A file already exists at '{request.DestinationPath}' and Overwrite is false.");
        }

        string? directory = Path.GetDirectoryName(request.DestinationPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Stream to a sibling .part file, then move into place only on success,
        // so a failed/retried download never leaves a truncated file that looks
        // complete.
        string partPath = request.DestinationPath + ".part";
        var progress = new DownloadProgress();

        try
        {
            HttpResponseMessage message = await this.ExecuteWithBackoffRetryAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                (response, token) => CopyResponseToPartFileAsync(response, partPath, request, progress, token),
                cancellationToken);

            if (!message.IsSuccessStatusCode)
            {
                DeleteIfExists(partPath);
                return NetHttpResponse<NetHttpDownloadResult>.Failure(
                    (int)message.StatusCode,
                    message.ReasonPhrase,
                    "Not a successful HTTP call. Status code indicates a failed download.");
            }

            // Atomic publish of the completed bytes.
            File.Move(partPath, request.DestinationPath, overwrite: request.Overwrite);

            this.LogDebug(
                "HTTP download complete {Endpoint} ({Bytes} bytes)",
                request.HttpEndPoint?.ToString() ?? "unknown",
                progress.BytesWritten);

            return NetHttpResponse<NetHttpDownloadResult>.Success(
                (int)message.StatusCode,
                message.ReasonPhrase,
                new NetHttpDownloadResult
                {
                    FilePath = request.DestinationPath,
                    BytesWritten = progress.BytesWritten,
                    ContentType = progress.ContentType,
                    ContentSha256 = progress.ContentSha256,
                });
        }
        catch (Exception ex)
        {
            DeleteIfExists(partPath);

            this.LogDebug(
                ex,
                "Download failed {Endpoint}",
                request.HttpEndPoint?.ToString() ?? "unknown");

            return NetHttpResponse<NetHttpDownloadResult>.Failure(
                500,
                string.Concat(ex?.Message, " ", ex?.InnerException?.Message),
                "Not a successful HTTP call. The download could not be completed.");
        }
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Create <see cref="HttpRequestMessage"/>.
    /// </summary>
    /// <param name="request"><see cref="NetHttpRequest"/> to use in the creation of the <see cref="HttpRequestMessage"/>.</param>
    /// <returns><see cref="HttpRequestMessage"/> based on the <see cref="NetHttpRequest"/>.</returns>
    /// <exception cref="ArgumentNullException">Request is null.</exception>
    /// <remarks>
    /// This method creates a NEW HttpRequestMessage instance each time it is called.
    /// This is required because HttpRequestMessage can only be sent once.
    /// The retry pattern calls this method for each retry attempt.
    /// </remarks>
    private static HttpRequestMessage CreateHttpRequestMessage(NetHttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        HttpRequestMessage message = new HttpRequestMessage(
            request.Method,
            request.HttpEndPoint)
        {
            Content = request.Content,
        };

        // Add HTTP Headers
        if (request.HttpHeaders != null)
        {
            foreach (NetHttpHeader header in request.HttpHeaders)
            {
                if (!string.IsNullOrEmpty(header.Name))
                {
                    message.Headers.Add(header.Name.Trim(), header.Value);
                }
            }
        }

        // Add Authentication
        if ((request.Authentication != null) &&
            (request.Authentication.AuthenticationType != NetHttpAuthenticationType.Unknown))
        {
            string name = request.Authentication.AuthenticationType switch
            {
                NetHttpAuthenticationType.Basic => "Basic",
                NetHttpAuthenticationType.Bearer => "Bearer",
                _ => string.Empty
            };

            if (!string.IsNullOrEmpty(name))
            {
                message.Headers.Authorization = new AuthenticationHeaderValue(
                    name,
                    request.Authentication.Value);
            }
        }

        return message;
    }

    /// <summary>
    /// Implements the wait logic for the backoff strategy.
    /// </summary>
    /// <param name="request"><see cref="NetHttpRequest"/> to use in the backoff calculation.</param>
    /// <param name="attempt">Count representing which attempt.</param>
    /// <param name="cancellationToken">Token to cancel tasks.</param>
    /// <returns>Completed Task after the delay.</returns>
    private static async Task WaitAsync(
        NetHttpRequest request,
        int attempt,
        CancellationToken cancellationToken)
    {
        // Backoff strategy - increase delay with each attempt
        var amount = Math.Pow(request.RetryPattern.DelayMultiplierInSeconds, attempt);
        await Task.Delay(TimeSpan.FromSeconds(amount), cancellationToken);
    }

    /// <summary>
    /// Copies a successful response body to the supplied <c>.part</c> file,
    /// recording bytes written, content type, and (optionally) a SHA-256 hash
    /// computed in the same single pass. Recreates the file on each invocation so
    /// a retried attempt starts from an empty file.
    /// </summary>
    /// <param name="response">The successful HTTP response.</param>
    /// <param name="partPath">Sibling temp path to write to.</param>
    /// <param name="request">The originating download request.</param>
    /// <param name="progress">Mutable carrier the caller reads after a successful copy.</param>
    /// <param name="cancellationToken">Token to cancel the copy.</param>
    /// <returns>A task that completes when the body has been written to disk.</returns>
    private static async Task CopyResponseToPartFileAsync(
        HttpResponseMessage response,
        string partPath,
        NetHttpDownloadRequest request,
        DownloadProgress progress,
        CancellationToken cancellationToken)
    {
        progress.ContentType = response.Content.Headers.ContentType?.ToString();

        int bufferSize = request.BufferSizeBytes > 0 ? request.BufferSizeBytes : 81920;

        using SHA256? sha256 = request.ComputeContentHash ? SHA256.Create() : null;

        await using (var fileStream = new FileStream(
            partPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize,
            useAsync: true))
        {
            await using Stream body = await response.Content.ReadAsStreamAsync(cancellationToken);

            if (sha256 is not null)
            {
                // leaveOpen so we can read fileStream.Length before the outer using disposes it.
                await using var crypto = new CryptoStream(fileStream, sha256, CryptoStreamMode.Write, leaveOpen: true);
                await body.CopyToAsync(crypto, bufferSize, cancellationToken);
                await crypto.FlushFinalBlockAsync(cancellationToken);
            }
            else
            {
                await body.CopyToAsync(fileStream, bufferSize, cancellationToken);
            }

            await fileStream.FlushAsync(cancellationToken);
            progress.BytesWritten = fileStream.Length;
        }

        if (sha256 is not null)
        {
            progress.ContentSha256 = Convert.ToHexString(sha256.Hash!).ToLowerInvariant();
        }
    }

    /// <summary>
    /// Deletes a file if it exists, swallowing I/O errors (best-effort cleanup).
    /// </summary>
    /// <param name="path">Path to delete.</param>
    private static void DeleteIfExists(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            // Best-effort cleanup; a leftover .part is preferable to masking the real error.
        }
        catch (UnauthorizedAccessException)
        {
            // Same rationale as IOException.
        }
    }

    /// <summary>
    /// Create <see cref="HttpClient"/> using the factory.
    /// </summary>
    /// <param name="request"><see cref="NetHttpRequest"/> to use in the creation of the <see cref="HttpClient"/>.</param>
    /// <returns><see cref="HttpClient"/> based on the <see cref="NetHttpRequest"/>.</returns>
    private HttpClient CreateHttpClient(NetHttpRequest request)
    {
        // Create client with named configuration or default
        string clientName = request.EnableCompression ? "CompressedClient" : "DefaultClient";

        HttpClient client = this._httpClientFactory.CreateClient(clientName);

        // Set timeout per request
        client.Timeout = TimeSpan.FromSeconds(request.TimeoutInSecondsPerAttempt);

        return client;
    }

    /// <summary>
    /// Implementation of the retry pattern with backoff strategy.
    /// </summary>
    /// <param name="request"><see cref="NetHttpRequest"/> to use in the creation of the <see cref="HttpClient"/>.</param>
    /// <param name="cancellationToken">Token to cancel tasks.</param>
    /// <returns>API response.</returns>
    /// <remarks>
    /// This method creates a NEW HttpRequestMessage for each retry attempt.
    /// HttpRequestMessage can only be sent once, so we must create a fresh instance
    /// for each attempt in the retry loop.
    /// </remarks>
    private async Task<HttpResponseMessage> ExecuteWithBackoffRetryAsync(
        NetHttpRequest request,
        HttpCompletionOption completion,
        Func<HttpResponseMessage, CancellationToken, Task>? onSuccessAsync,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage? lastResponse = null;
        int totalAttempts = request.RetryPattern.MaxAttempts + 1;
        TimeSpan attemptTimeout = TimeSpan.FromSeconds(request.TimeoutInSecondsPerAttempt);

        for (int attempt = 0; attempt <= request.RetryPattern.MaxAttempts; attempt++)
        {
            // Create a NEW HttpRequestMessage for THIS attempt
            using (HttpRequestMessage message = CreateHttpRequestMessage(request))
            {
                try
                {
                    using (HttpClient client = this.CreateHttpClient(request))
                    {
                        // Send request with timeout
                        HttpResponseMessage response = await client.SendAsync(
                            message,
                            completion,
                            cancellationToken)
                            .WaitAsync(attemptTimeout, cancellationToken);

                        // Determine if we want to try again
                        if ((response.StatusCode == HttpStatusCode.ServiceUnavailable) ||
                            (response.StatusCode == HttpStatusCode.RequestTimeout) ||
                            (response.StatusCode == HttpStatusCode.GatewayTimeout))
                        {
                            lastResponse = response;

                            // Don't retry on last attempt
                            if (attempt < request.RetryPattern.MaxAttempts)
                            {
                                this.LogDebug(
                                    "HTTP {StatusCode} from {Endpoint}, attempt {Attempt} of {TotalAttempts}, retrying after backoff",
                                    (int)response.StatusCode,
                                    request.HttpEndPoint?.ToString() ?? "unknown",
                                    attempt + 1,
                                    totalAttempts);

                                await WaitAsync(request, attempt, cancellationToken);
                                continue;
                            }

                            // Last attempt with retriable status code, fall through to return below
                            this.LogWarning(
                                "All retry attempts exhausted for {Method} {Endpoint}, last status {StatusCode}",
                                request.Method,
                                request.HttpEndPoint?.ToString() ?? "unknown",
                                (int)response.StatusCode);

                            break;
                        }

                        // On success, consume the response inside the retried region so a
                        // mid-body failure (e.g. a dropped connection while copying a large
                        // download to disk) is retried, not surfaced as a partial success.
                        // The per-attempt timeout covers the consume as well as the send.
                        if (onSuccessAsync is not null && response.IsSuccessStatusCode)
                        {
                            await onSuccessAsync(response, cancellationToken).WaitAsync(attemptTimeout, cancellationToken);
                        }

                        // Success or non-retriable status code
                        return response;
                    }
                }
                catch (HttpRequestException ex)
                {
                    // Networking issue occurred - retry if attempts remain
                    if (attempt < request.RetryPattern.MaxAttempts)
                    {
                        this.LogDebug(
                            ex,
                            "Network error calling {Endpoint}, attempt {Attempt} of {TotalAttempts}, retrying after backoff",
                            request.HttpEndPoint?.ToString() ?? "unknown",
                            attempt + 1,
                            totalAttempts);

                        await WaitAsync(request, attempt, cancellationToken);
                        continue;
                    }

                    // No more attempts, fall through to return BadRequest below
                    this.LogWarning(
                        ex,
                        "All retry attempts exhausted for {Method} {Endpoint} due to network error",
                        request.Method,
                        request.HttpEndPoint?.ToString() ?? "unknown");

                    break;
                }
                catch (IOException ex)
                {
                    // A mid-stream read/write failure (most commonly a dropped connection
                    // partway through a large body copy) - retry if attempts remain.
                    if (attempt < request.RetryPattern.MaxAttempts)
                    {
                        this.LogDebug(
                            ex,
                            "I/O error streaming {Endpoint}, attempt {Attempt} of {TotalAttempts}, retrying after backoff",
                            request.HttpEndPoint?.ToString() ?? "unknown",
                            attempt + 1,
                            totalAttempts);

                        await WaitAsync(request, attempt, cancellationToken);
                        continue;
                    }

                    this.LogWarning(
                        ex,
                        "All retry attempts exhausted for {Method} {Endpoint} due to an I/O error",
                        request.Method,
                        request.HttpEndPoint?.ToString() ?? "unknown");

                    break;
                }
                catch (TimeoutException ex)
                {
                    // Task timeout occurred - retry if attempts remain
                    if (attempt < request.RetryPattern.MaxAttempts)
                    {
                        this.LogDebug(
                            ex,
                            "Timeout calling {Endpoint}, attempt {Attempt} of {TotalAttempts}, retrying after backoff",
                            request.HttpEndPoint?.ToString() ?? "unknown",
                            attempt + 1,
                            totalAttempts);

                        await WaitAsync(request, attempt, cancellationToken);
                        continue;
                    }

                    // No more attempts, fall through to return BadRequest below
                    this.LogWarning(
                        ex,
                        "All retry attempts exhausted for {Method} {Endpoint} due to timeout",
                        request.Method?.ToString() ?? "unknown",
                        request.HttpEndPoint?.ToString() ?? "unknown");

                    break;
                }
            } // HttpRequestMessage is disposed here after each attempt
        }

        // Retry pattern exhausted - return last response if available, otherwise BadRequest
        return lastResponse ?? new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("Unable to complete Http Request."),
        };
    }

    /// <summary>
    /// Handles an <see cref="HttpRequestException"/> whose inner exception is a
    /// <see cref="SocketException"/>. This is defensive code that guards against
    /// future changes introducing a code path where such an exception escapes the
    /// retry method.
    /// </summary>
    /// <typeparam name="T">Type of the expected response data.</typeparam>
    /// <param name="ex">The caught exception.</param>
    /// <param name="request">The original HTTP request.</param>
    /// <returns><see cref="NetHttpResponse{T}"/> indicating failure with status 500.</returns>
    [ExcludeFromCodeCoverage]
    private NetHttpResponse<T> HandleHttpRequestWithSocketException<T>(
        HttpRequestException ex,
        NetHttpRequest request)
    {
        this.LogDebug(
            ex,
            "Socket error calling {Endpoint}",
            request.HttpEndPoint?.ToString() ?? "unknown");

        return NetHttpResponse<T>.Failure(
            500,
            string.Concat(ex?.Message, " ", ex?.InnerException?.Message),
            "Not a successful HTTP call. An unknown error occurred with the HTTP Request.");
    }

    #endregion Private Methods

    #region Private Types

    /// <summary>
    /// Mutable carrier the download copy writes into and
    /// <see cref="DownloadFileAsync"/> reads after a successful attempt.
    /// </summary>
    private sealed class DownloadProgress
    {
        public long BytesWritten { get; set; }

        public string? ContentType { get; set; }

        public string? ContentSha256 { get; set; }
    }

    #endregion Private Types
}