namespace Roadbed.Test.Unit.Net;

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Configurable mock <see cref="HttpMessageHandler"/> and <see cref="IHttpClientFactory"/>
/// for testing <see cref="Roadbed.Net.NetHttpClient"/> without making real HTTP calls.
/// </summary>
/// <remarks>
/// <para>
/// Supports two response configuration modes:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// <see cref="OnSendAsync"/> delegate for simple single-response scenarios.
/// </description>
/// </item>
/// <item>
/// <description>
/// Response queue via <see cref="EnqueueResponse(HttpResponseMessage)"/> and
/// <see cref="EnqueueException"/> for sequential retry testing. Queued responses
/// take priority over the <see cref="OnSendAsync"/> delegate.
/// </description>
/// </item>
/// </list>
/// <para>
/// When neither a queued response nor an <see cref="OnSendAsync"/> delegate is
/// configured, <see cref="SendAsync"/> returns <see cref="HttpStatusCode.OK"/>
/// with empty content.
/// </para>
/// </remarks>
public sealed class MockHttpMessageHandler
    : HttpMessageHandler, IHttpClientFactory
{
    #region Private Fields

    /// <summary>
    /// Queue of response factories for sequential testing (e.g., retry scenarios).
    /// </summary>
    private readonly Queue<Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>> _responseQueue;

    #endregion Private Fields

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="MockHttpMessageHandler"/> class.
    /// </summary>
    public MockHttpMessageHandler()
    {
        this._responseQueue =
            new Queue<Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>>();
        this.SentRequests = new List<HttpRequestMessage>();
    }

    #endregion Public Constructors

    #region Public Properties

    /// <summary>
    /// Gets or sets the delegate invoked when <see cref="SendAsync"/> is called
    /// and the response queue is empty. Use this for simple single-response scenarios.
    /// </summary>
    public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? OnSendAsync
    {
        get;
        set;
    }

    /// <summary>
    /// Gets the number of times <see cref="SendAsync"/> was called.
    /// </summary>
    public int SendAsyncCallCount
    {
        get;
        private set;
    }

    /// <summary>
    /// Gets the most recent client name passed to <see cref="CreateClient"/>.
    /// </summary>
    public string? LastClientName
    {
        get;
        private set;
    }

    /// <summary>
    /// Gets the list of all <see cref="HttpRequestMessage"/> instances passed to
    /// <see cref="SendAsync"/>. Request method, URI, and headers remain accessible
    /// after disposal; content may not.
    /// </summary>
    public List<HttpRequestMessage> SentRequests
    {
        get;
    }

    #endregion Public Properties

    #region Public Methods

    /// <summary>
    /// Enqueues a pre-built <see cref="HttpResponseMessage"/> to be returned
    /// by the next call to <see cref="SendAsync"/>.
    /// </summary>
    /// <param name="response">Response to return.</param>
    public void EnqueueResponse(HttpResponseMessage response)
    {
        this._responseQueue.Enqueue(
            (req, ct) => Task.FromResult(response));
    }

    /// <summary>
    /// Enqueues a response with the specified status code and optional content
    /// to be returned by the next call to <see cref="SendAsync"/>.
    /// </summary>
    /// <param name="statusCode">HTTP status code for the response.</param>
    /// <param name="content">Response body content. Defaults to empty string.</param>
    public void EnqueueResponse(HttpStatusCode statusCode, string content = "")
    {
        this._responseQueue.Enqueue(
            (req, ct) => Task.FromResult(
                new HttpResponseMessage(statusCode)
                {
                    Content = new StringContent(content),
                }));
    }

    /// <summary>
    /// Enqueues an exception to be thrown by the next call to <see cref="SendAsync"/>.
    /// </summary>
    /// <param name="exception">Exception to throw.</param>
    public void EnqueueException(Exception exception)
    {
        this._responseQueue.Enqueue(
            (req, ct) => Task.FromException<HttpResponseMessage>(exception));
    }

    /// <summary>
    /// Creates an <see cref="HttpClient"/> backed by this handler.
    /// The handler is not disposed when the client is disposed.
    /// </summary>
    /// <param name="name">Named client configuration (e.g., "DefaultClient", "CompressedClient").</param>
    /// <returns><see cref="HttpClient"/> that routes all requests through this handler.</returns>
    public HttpClient CreateClient(string name)
    {
        this.LastClientName = name;
        return new HttpClient(this, disposeHandler: false);
    }

    #endregion Public Methods

    #region Protected Methods

    /// <summary>
    /// Intercepts the HTTP request, records it, and returns the next configured response.
    /// </summary>
    /// <param name="request">The outgoing HTTP request.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The configured <see cref="HttpResponseMessage"/>.</returns>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        this.SendAsyncCallCount++;
        this.SentRequests.Add(request);

        if (this._responseQueue.Count > 0)
        {
            var handler = this._responseQueue.Dequeue();
            return await handler(request, cancellationToken);
        }

        if (this.OnSendAsync is not null)
        {
            return await this.OnSendAsync(request, cancellationToken);
        }

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(string.Empty),
        };
    }

    #endregion Protected Methods
}