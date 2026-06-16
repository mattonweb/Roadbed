namespace Roadbed.Net;

using System.Collections.Generic;

/// <summary>
/// API Response.
/// </summary>
/// <typeparam name="T"></typeparam>
public record NetHttpResponse<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NetHttpResponse{T}"/> class.
    /// </summary>
    /// <param name="statusCode">Http status code.</param>
    /// <param name="statusDescription">Http status description.</param>
    /// <param name="isSuccess">Flag indicating whether the response was successful.</param>
    /// <param name="value">Object returned from the API.</param>
    /// <param name="error">Error message returned from the API.</param>
    internal NetHttpResponse(int statusCode, string? statusDescription, bool isSuccess, T value, string error)
    {
        this.HttpStatusCode = statusCode;
        this.IsSuccessStatusCode = isSuccess;
        this.HttpStatusCodeDescription = statusDescription;
        this.Data = value;

        this.Errors = new List<string>();
        this.ResponseHeaders = System.Array.Empty<NetHttpHeader>();

        if (!string.IsNullOrEmpty(error))
        {
            this.Errors.Add(error);
        }
    }

    /// <summary>
    /// Gets a value indicating whether the response was successful.
    /// </summary>
    public bool IsSuccessStatusCode
    {
        get;
    }

    /// <summary>
    /// Gets the object returned from the API.
    /// </summary>
    public T Data
    {
        get;
        internal set;
    }

    /// <summary>
    /// Gets the list of error messages associated with the current operation or object.
    /// </summary>
    public List<string> Errors
    {
        get;
    }

    /// <summary>
    /// Gets the HTTP status code.
    /// </summary>
    public int HttpStatusCode
    {
        get;
        internal set;
    }

    /// <summary>
    /// Gets the HTTP status code description.
    /// </summary>
    public string? HttpStatusCodeDescription
    {
        get;
        internal set;
    }

    /// <summary>
    /// Gets the response headers returned by the server. Includes both general
    /// response headers (e.g. <c>ETag</c>, <c>Retry-After</c>,
    /// <c>RateLimit-*</c>/<c>X-RateLimit-*</c>) and content headers
    /// (e.g. <c>Last-Modified</c>, <c>Content-Type</c>, <c>Content-Length</c>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Populated on every outcome — 2xx success, error, and 304 Not Modified.
    /// Empty (never <c>null</c>) when no response was received (for example,
    /// the request failed before reaching the server).
    /// </para>
    /// <para>
    /// <b>Multi-valued headers</b> are represented as one
    /// <see cref="NetHttpHeader"/> entry per value, all sharing the same
    /// <see cref="NetHttpHeader.Name"/>. Callers that need a specific header's
    /// values should filter by name; the order of entries for a given name
    /// preserves the order returned by <see cref="System.Net.Http.HttpResponseMessage"/>.
    /// </para>
    /// </remarks>
    public IReadOnlyList<NetHttpHeader> ResponseHeaders
    {
        get;
        internal set;
    }

    /// <summary>
    /// Create a successful API response.
    /// </summary>
    /// <param name="statusCode">Http status code.</param>
    /// <param name="statusDescription">Http status description.</param>
    /// <param name="value">Object returned from the API.</param>
    /// <returns><see cref="NetHttpResponse{T}"/> indicating a successful API call.</returns>
    internal static NetHttpResponse<T> Success(int statusCode, string? statusDescription, T value) =>
        new NetHttpResponse<T>(statusCode, statusDescription, true, value, string.Empty);

    /// <summary>
    /// Create a failed API response.
    /// </summary>
    /// <param name="statusCode">Http status code.</param>
    /// <param name="statusDescription">Http status description.</param>
    /// <param name="error">Error message returned from the API.</param>
    /// <returns><see cref="NetHttpResponse{T}"/> indicating a failed API call.</returns>
    internal static NetHttpResponse<T> Failure(int statusCode, string? statusDescription, string error) =>
        new NetHttpResponse<T>(statusCode, statusDescription, false, default!, error);
}
