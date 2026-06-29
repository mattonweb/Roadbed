namespace Roadbed.Net;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

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

    /// <summary>
    /// Clock source for retry backoff delays. Defaults to
    /// <see cref="TimeProvider.System"/> on the public constructor; a test
    /// can supply a fake clock to virtualize the wait without sleeping.
    /// </summary>
    private readonly TimeProvider _timeProvider;

    #endregion Private Fields

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="NetHttpClient"/> class
    /// using <see cref="TimeProvider.System"/> as the backoff clock.
    /// </summary>
    /// <param name="httpClientFactory">Factory for creating HttpClient instances.</param>
    /// <param name="logger">Represents a type used to perform logging.</param>
    /// <exception cref="ArgumentNullException"><paramref name="httpClientFactory"/> is <c>null</c>.</exception>
    public NetHttpClient(
        IHttpClientFactory httpClientFactory,
        ILogger<NetHttpClient> logger)
        : this(httpClientFactory, TimeProvider.System, logger)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NetHttpClient"/> class
    /// with an explicit <see cref="TimeProvider"/> driving retry backoff.
    /// </summary>
    /// <param name="httpClientFactory">Factory for creating HttpClient instances.</param>
    /// <param name="timeProvider">Clock source used by <see cref="Task.Delay(TimeSpan, TimeProvider, CancellationToken)"/> in the retry backoff path; a fake clock virtualizes the wait so retry / backoff tests do not sleep in real time.</param>
    /// <param name="logger">Represents a type used to perform logging.</param>
    /// <exception cref="ArgumentNullException"><paramref name="httpClientFactory"/> or <paramref name="timeProvider"/> is <c>null</c>.</exception>
    public NetHttpClient(
        IHttpClientFactory httpClientFactory,
        TimeProvider timeProvider,
        ILogger<NetHttpClient> logger)
        : base(logger)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(timeProvider);

        this._httpClientFactory = httpClientFactory;
        this._timeProvider = timeProvider;
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

            IReadOnlyList<NetHttpHeader> responseHeaders = ExtractResponseHeaders(message);

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
                        T? deserialized = JsonSerializer.Deserialize<T>(responseBody, RoadbedJson.Options);

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

            response.ResponseHeaders = responseHeaders;
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

            IReadOnlyList<NetHttpHeader> responseHeaders = ExtractResponseHeaders(message);

            // 304 Not Modified: a conditional GET (If-None-Match / If-Modified-Since)
            // matched the server's current resource. Treat as a non-error short-circuit:
            // leave any existing file at DestinationPath untouched, write nothing, and
            // surface the response headers (ETag / Last-Modified / RateLimit-* etc.)
            // so the caller can refresh its cache metadata.
            if (message.StatusCode == HttpStatusCode.NotModified)
            {
                DeleteIfExists(partPath);

                this.LogDebug(
                    "HTTP 304 Not Modified {Endpoint}",
                    request.HttpEndPoint?.ToString() ?? "unknown");

                NetHttpResponse<NetHttpDownloadResult> notModifiedResponse =
                    NetHttpResponse<NetHttpDownloadResult>.Success(
                        (int)message.StatusCode,
                        message.ReasonPhrase,
                        new NetHttpDownloadResult
                        {
                            FilePath = request.DestinationPath,
                            BytesWritten = 0,
                            ContentType = null,
                            ContentSha256 = null,
                            NotModified = true,
                            ResponseHeaders = responseHeaders,
                        });

                notModifiedResponse.ResponseHeaders = responseHeaders;

                return notModifiedResponse;
            }

            if (!message.IsSuccessStatusCode)
            {
                DeleteIfExists(partPath);
                NetHttpResponse<NetHttpDownloadResult> failure =
                    NetHttpResponse<NetHttpDownloadResult>.Failure(
                        (int)message.StatusCode,
                        message.ReasonPhrase,
                        "Not a successful HTTP call. Status code indicates a failed download.");
                failure.ResponseHeaders = responseHeaders;
                return failure;
            }

            // Atomic publish of the completed bytes.
            File.Move(partPath, request.DestinationPath, overwrite: request.Overwrite);

            this.LogDebug(
                "HTTP download complete {Endpoint} ({Bytes} bytes)",
                request.HttpEndPoint?.ToString() ?? "unknown",
                progress.BytesWritten);

            NetHttpResponse<NetHttpDownloadResult> success =
                NetHttpResponse<NetHttpDownloadResult>.Success(
                    (int)message.StatusCode,
                    message.ReasonPhrase,
                    new NetHttpDownloadResult
                    {
                        FilePath = request.DestinationPath,
                        BytesWritten = progress.BytesWritten,
                        ContentType = progress.ContentType,
                        ContentSha256 = progress.ContentSha256,
                        ResponseHeaders = responseHeaders,
                    });

            success.ResponseHeaders = responseHeaders;
            return success;
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

        // Add HTTP Headers. Use TryAddWithoutValidation so that:
        //   * restricted/parser-strict headers (User-Agent, If-None-Match,
        //     If-Modified-Since, Referer, ...) are transmitted as-is rather than
        //     rejected by the validating Add overload (which would throw and lose
        //     the header), and
        //   * a WAF-bypassing browser-ish User-Agent string with multiple comments
        //     reaches the server intact.
        // Content-only headers (Content-Type, Content-Length, ...) fall through to
        // a second attempt on Content.Headers when the request has Content.
        if (request.HttpHeaders != null)
        {
            foreach (NetHttpHeader header in request.HttpHeaders)
            {
                if (string.IsNullOrEmpty(header.Name))
                {
                    continue;
                }

                string name = header.Name.Trim();

                if (!message.Headers.TryAddWithoutValidation(name, header.Value))
                {
                    message.Content?.Headers.TryAddWithoutValidation(name, header.Value);
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
    /// Flattens the response's general headers and content headers into a single
    /// read-only list of <see cref="NetHttpHeader"/> entries. Multi-valued headers
    /// produce one entry per value, all sharing the same name, in the order
    /// returned by <see cref="HttpResponseMessage"/>.
    /// </summary>
    /// <param name="message">The response to read headers from. May be <c>null</c>.</param>
    /// <returns>
    /// A flattened list of headers. Empty (never <c>null</c>) when
    /// <paramref name="message"/> is <c>null</c>.
    /// </returns>
    private static IReadOnlyList<NetHttpHeader> ExtractResponseHeaders(HttpResponseMessage? message)
    {
        if (message is null)
        {
            return Array.Empty<NetHttpHeader>();
        }

        var headers = new List<NetHttpHeader>();

        foreach (KeyValuePair<string, IEnumerable<string>> kvp in message.Headers)
        {
            foreach (string value in kvp.Value)
            {
                headers.Add(new NetHttpHeader(kvp.Key, value));
            }
        }

        if (message.Content is not null)
        {
            foreach (KeyValuePair<string, IEnumerable<string>> kvp in message.Content.Headers)
            {
                foreach (string value in kvp.Value)
                {
                    headers.Add(new NetHttpHeader(kvp.Key, value));
                }
            }
        }

        return headers;
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
    /// Implements the wait logic for the backoff strategy.
    /// </summary>
    /// <param name="request"><see cref="NetHttpRequest"/> to use in the backoff calculation.</param>
    /// <param name="attempt">Count representing which attempt.</param>
    /// <param name="cancellationToken">Token to cancel tasks.</param>
    /// <returns>Completed Task after the delay.</returns>
    /// <remarks>
    /// Routes the delay through the injected <see cref="TimeProvider"/> so a
    /// fake clock can virtualize the wait — tests covering retry / backoff
    /// timing do not pay the real wall-clock cost.
    /// </remarks>
    private async Task WaitAsync(
        NetHttpRequest request,
        int attempt,
        CancellationToken cancellationToken)
    {
        // Backoff strategy - increase delay with each attempt
        var amount = Math.Pow(request.RetryPattern.DelayMultiplierInSeconds, attempt);
        await Task.Delay(TimeSpan.FromSeconds(amount), this._timeProvider, cancellationToken);
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
    /// <param name="completion">Controls whether <see cref="HttpClient.SendAsync(HttpRequestMessage, HttpCompletionOption, CancellationToken)"/> returns after reading the headers or after the entire response body has been buffered.</param>
    /// <param name="onSuccessAsync">Optional callback invoked once a successful response is received; runs while the response is still open so streaming readers can consume the body before disposal.</param>
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

                                await this.WaitAsync(request, attempt, cancellationToken);
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

                        await this.WaitAsync(request, attempt, cancellationToken);
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

                        await this.WaitAsync(request, attempt, cancellationToken);
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

                        await this.WaitAsync(request, attempt, cancellationToken);
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