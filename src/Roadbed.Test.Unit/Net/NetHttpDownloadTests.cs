namespace Roadbed.Test.Unit.Net;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Net;

/// <summary>
/// Unit tests for <see cref="NetHttpClient.DownloadFileAsync"/> — the streaming,
/// retried, hashing download path.
/// </summary>
[TestClass]
public class NetHttpDownloadTests
{
    #region Public Methods

    /// <summary>
    /// A successful download writes the exact bytes, reports the byte count and
    /// content type, and returns a SHA-256 computed in the copy pass.
    /// </summary>
    /// <returns>Task representing the test.</returns>
    [TestMethod]
    public async Task DownloadFileAsync_Success_WritesFileWithHashAndContentType()
    {
        // Arrange (Given)
        byte[] payload = RandomBytes(50_000);
        string expectedHash = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();

        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(BinaryResponse(payload, "application/octet-stream"));

        NetHttpClient client = CreateClient(handler);
        string dest = NewTempPath();

        try
        {
            // Act (When)
            NetHttpResponse<NetHttpDownloadResult> response =
                await client.DownloadFileAsync(CreateDownloadRequest(dest, maxAttempts: 0));

            // Assert (Then)
            Assert.IsTrue(response.IsSuccessStatusCode, "Download should succeed.");
            CollectionAssert.AreEqual(payload, await File.ReadAllBytesAsync(dest), "File bytes must match the payload.");
            Assert.AreEqual(payload.Length, response.Data.BytesWritten);
            Assert.AreEqual("application/octet-stream", response.Data.ContentType);
            Assert.AreEqual(expectedHash, response.Data.ContentSha256, "SHA-256 must match the payload.");
            Assert.IsFalse(File.Exists(dest + ".part"), "The .part file must be gone after a successful move.");
        }
        finally
        {
            Cleanup(dest);
        }
    }

    /// <summary>
    /// With hashing disabled, no SHA-256 is returned (and the file is still written).
    /// </summary>
    /// <returns>Task representing the test.</returns>
    [TestMethod]
    public async Task DownloadFileAsync_HashDisabled_ReturnsNullHash()
    {
        // Arrange (Given)
        byte[] payload = RandomBytes(1_000);
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(BinaryResponse(payload, "application/octet-stream"));

        NetHttpClient client = CreateClient(handler);
        string dest = NewTempPath();

        try
        {
            NetHttpDownloadRequest request = CreateDownloadRequest(dest, maxAttempts: 0);
            request.ComputeContentHash = false;

            // Act (When)
            NetHttpResponse<NetHttpDownloadResult> response = await client.DownloadFileAsync(request);

            // Assert (Then)
            Assert.IsTrue(response.IsSuccessStatusCode);
            Assert.IsNull(response.Data.ContentSha256, "No hash should be computed when disabled.");
            Assert.AreEqual(payload.Length, response.Data.BytesWritten);
        }
        finally
        {
            Cleanup(dest);
        }
    }

    /// <summary>
    /// A connection drop partway through the body copy is retried, and the second
    /// attempt produces a complete, correct file (the partial first attempt does
    /// not survive).
    /// </summary>
    /// <returns>Task representing the test.</returns>
    [TestMethod]
    public async Task DownloadFileAsync_MidBodyDrop_RetriesAndCompletes()
    {
        // Arrange (Given)
        byte[] payload = RandomBytes(40_000);
        string expectedHash = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();

        var handler = new MockHttpMessageHandler();

        // Attempt 1: stream drops after 10 KB. Attempt 2: full payload.
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(new FailingStream(payload, failAfterBytes: 10_000)),
        });
        handler.EnqueueResponse(BinaryResponse(payload, "application/octet-stream"));

        NetHttpClient client = CreateClient(handler);
        string dest = NewTempPath();

        try
        {
            // Act (When)
            NetHttpResponse<NetHttpDownloadResult> response =
                await client.DownloadFileAsync(CreateDownloadRequest(dest, maxAttempts: 1));

            // Assert (Then)
            Assert.IsTrue(response.IsSuccessStatusCode, "Download should succeed after retrying the dropped body.");
            Assert.AreEqual(2, handler.SendAsyncCallCount, "The whole attempt (incl. body copy) must be retried.");
            CollectionAssert.AreEqual(payload, await File.ReadAllBytesAsync(dest));
            Assert.AreEqual(expectedHash, response.Data.ContentSha256);
            Assert.IsFalse(File.Exists(dest + ".part"));
        }
        finally
        {
            Cleanup(dest);
        }
    }

    /// <summary>
    /// A non-success status returns a failure and leaves no file (and no .part).
    /// </summary>
    /// <returns>Task representing the test.</returns>
    [TestMethod]
    public async Task DownloadFileAsync_NonSuccessStatus_ReturnsFailureNoFile()
    {
        // Arrange (Given)
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(HttpStatusCode.NotFound, "nope");

        NetHttpClient client = CreateClient(handler);
        string dest = NewTempPath();

        try
        {
            // Act (When)
            NetHttpResponse<NetHttpDownloadResult> response =
                await client.DownloadFileAsync(CreateDownloadRequest(dest, maxAttempts: 0));

            // Assert (Then)
            Assert.IsFalse(response.IsSuccessStatusCode);
            Assert.AreEqual(404, response.HttpStatusCode);
            Assert.IsFalse(File.Exists(dest), "No destination file on failure.");
            Assert.IsFalse(File.Exists(dest + ".part"), "No leftover .part on failure.");
        }
        finally
        {
            Cleanup(dest);
        }
    }

    /// <summary>
    /// Overwrite = false against an existing file fails without contacting the server.
    /// </summary>
    /// <returns>Task representing the test.</returns>
    [TestMethod]
    public async Task DownloadFileAsync_OverwriteFalseExistingFile_FailsWithoutRequest()
    {
        // Arrange (Given)
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(HttpStatusCode.OK, "should-not-be-used");

        NetHttpClient client = CreateClient(handler);
        string dest = NewTempPath();
        await File.WriteAllTextAsync(dest, "existing");

        try
        {
            NetHttpDownloadRequest request = CreateDownloadRequest(dest, maxAttempts: 0);
            request.Overwrite = false;

            // Act (When)
            NetHttpResponse<NetHttpDownloadResult> response = await client.DownloadFileAsync(request);

            // Assert (Then)
            Assert.IsFalse(response.IsSuccessStatusCode);
            Assert.AreEqual(409, response.HttpStatusCode);
            Assert.AreEqual(0, handler.SendAsyncCallCount, "No HTTP request should be made when the file exists and Overwrite is false.");
            Assert.AreEqual("existing", await File.ReadAllTextAsync(dest), "Existing file must be untouched.");
        }
        finally
        {
            Cleanup(dest);
        }
    }

    /// <summary>
    /// On a 304 Not Modified, the download is a no-op: no file is created, no
    /// .part is left behind, an existing destination file is preserved untouched,
    /// and the result surfaces NotModified = true plus the ETag / Last-Modified /
    /// rate-limit headers needed by the consumer's good-citizen policy.
    /// </summary>
    /// <returns>Task representing the test.</returns>
    [TestMethod]
    public async Task DownloadFileAsync_NotModified_PreservesExistingFileAndSurfacesHeaders()
    {
        // Arrange (Given)
        var responseMessage = new HttpResponseMessage(HttpStatusCode.NotModified);
        responseMessage.Headers.TryAddWithoutValidation("ETag", "\"v2\"");
        responseMessage.Headers.TryAddWithoutValidation("Retry-After", "0");
        responseMessage.Headers.TryAddWithoutValidation("X-RateLimit-Remaining", "5");
        responseMessage.Content = new ByteArrayContent(Array.Empty<byte>());
        responseMessage.Content.Headers.TryAddWithoutValidation("Last-Modified", "Wed, 21 Oct 2025 07:28:00 GMT");

        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(responseMessage);

        NetHttpClient client = CreateClient(handler);
        string dest = NewTempPath();
        const string existing = "previously-cached-bytes";
        await File.WriteAllTextAsync(dest, existing);

        try
        {
            NetHttpDownloadRequest request = CreateDownloadRequest(dest, maxAttempts: 0);
            request.HttpHeaders.Add(new NetHttpHeader("If-None-Match", "\"v2\""));

            // Act (When)
            NetHttpResponse<NetHttpDownloadResult> response = await client.DownloadFileAsync(request);

            // Assert (Then)
            Assert.AreEqual(304, response.HttpStatusCode, "Status code 304 must be surfaced.");
            Assert.IsTrue(response.Data.NotModified, "NotModified must be true on a 304 outcome.");
            Assert.AreEqual(0, response.Data.BytesWritten, "No bytes are written on 304.");
            Assert.IsEmpty(response.Errors, "304 is not an error.");

            Assert.AreEqual(existing, await File.ReadAllTextAsync(dest), "Existing file must be untouched on 304.");
            Assert.IsFalse(File.Exists(dest + ".part"), "No .part file should exist after a 304.");

            // Response headers populated on BOTH the outer response and the inner result.
            foreach (IReadOnlyList<NetHttpHeader> headers in new[] { response.ResponseHeaders, response.Data.ResponseHeaders })
            {
                Assert.IsTrue(
                    headers.Any(h => string.Equals(h.Name, "ETag", StringComparison.OrdinalIgnoreCase) && h.Value == "\"v2\""),
                    "ETag must be surfaced on a 304.");
                Assert.IsTrue(
                    headers.Any(h => string.Equals(h.Name, "Last-Modified", StringComparison.OrdinalIgnoreCase)),
                    "Last-Modified (a content header) must be surfaced on a 304.");
                Assert.IsTrue(
                    headers.Any(h => string.Equals(h.Name, "X-RateLimit-Remaining", StringComparison.OrdinalIgnoreCase)),
                    "RateLimit-* headers must be surfaced on a 304.");
            }

            HttpRequestMessage sent = handler.SentRequests[0];
            Assert.IsTrue(sent.Headers.Contains("If-None-Match"), "If-None-Match must reach the server on a download GET.");
        }
        finally
        {
            Cleanup(dest);
        }
    }

    /// <summary>
    /// On a successful download, response headers from BOTH the general headers
    /// (e.g. <c>ETag</c>) and content headers (e.g. <c>Content-Type</c>) are
    /// surfaced on the result.
    /// </summary>
    /// <returns>Task representing the test.</returns>
    [TestMethod]
    public async Task DownloadFileAsync_Success_PopulatesResponseHeaders()
    {
        // Arrange (Given)
        byte[] payload = RandomBytes(2_000);
        var content = new ByteArrayContent(payload);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
        responseMessage.Headers.TryAddWithoutValidation("ETag", "\"v1\"");

        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(responseMessage);

        NetHttpClient client = CreateClient(handler);
        string dest = NewTempPath();

        try
        {
            // Act (When)
            NetHttpResponse<NetHttpDownloadResult> response =
                await client.DownloadFileAsync(CreateDownloadRequest(dest, maxAttempts: 0));

            // Assert (Then)
            Assert.IsTrue(response.IsSuccessStatusCode);
            Assert.IsFalse(response.Data.NotModified, "A 2xx download is not NotModified.");

            Assert.IsTrue(
                response.Data.ResponseHeaders.Any(h => string.Equals(h.Name, "ETag", StringComparison.OrdinalIgnoreCase) && h.Value == "\"v1\""),
                "ETag must be surfaced on a 2xx download.");
            Assert.IsTrue(
                response.Data.ResponseHeaders.Any(h => string.Equals(h.Name, "Content-Type", StringComparison.OrdinalIgnoreCase)),
                "Content-Type (a content header) must be surfaced on a 2xx download.");
        }
        finally
        {
            Cleanup(dest);
        }
    }

    /// <summary>
    /// A browser-ish User-Agent set on a download request reaches the server. The
    /// consumer's WAF-bypass policy depends on this.
    /// </summary>
    /// <returns>Task representing the test.</returns>
    [TestMethod]
    public async Task DownloadFileAsync_BrowserUserAgent_ReachesServer()
    {
        // Arrange (Given)
        const string browserUserAgent =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

        byte[] payload = RandomBytes(100);
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(BinaryResponse(payload, "application/octet-stream"));

        NetHttpClient client = CreateClient(handler);
        string dest = NewTempPath();

        try
        {
            NetHttpDownloadRequest request = CreateDownloadRequest(dest, maxAttempts: 0);
            request.HttpHeaders.Add(new NetHttpHeader("User-Agent", browserUserAgent));

            // Act (When)
            await client.DownloadFileAsync(request);

            // Assert (Then)
            HttpRequestMessage sent = handler.SentRequests[0];
            Assert.IsTrue(sent.Headers.Contains("User-Agent"));

            string sentUserAgent = string.Join(" ", sent.Headers.GetValues("User-Agent"));
            Assert.AreEqual(browserUserAgent, sentUserAgent);
        }
        finally
        {
            Cleanup(dest);
        }
    }

    /// <summary>
    /// A null request throws; a blank destination throws.
    /// </summary>
    /// <returns>Task representing the test.</returns>
    [TestMethod]
    public async Task DownloadFileAsync_InvalidArguments_Throw()
    {
        NetHttpClient client = CreateClient(new MockHttpMessageHandler());

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            async () => await client.DownloadFileAsync(null!));

        await Assert.ThrowsExactlyAsync<ArgumentException>(
            async () => await client.DownloadFileAsync(new NetHttpDownloadRequest
            {
                HttpEndPoint = new Uri("https://example.com/x"),
                DestinationPath = string.Empty,
            }));
    }

    #endregion Public Methods

    #region Private Methods

    private static NetHttpClient CreateClient(MockHttpMessageHandler handler) =>
        new NetHttpClient(handler, NullLogger<NetHttpClient>.Instance);

    private static NetHttpDownloadRequest CreateDownloadRequest(string dest, int maxAttempts) =>
        new NetHttpDownloadRequest
        {
            HttpEndPoint = new Uri("https://files.example.com/big.xlsx"),
            DestinationPath = dest,
            TimeoutInSecondsPerAttempt = 30,
            RetryPattern = new NetHttpRetryPattern
            {
                MaxAttempts = maxAttempts,
                DelayMultiplierInSeconds = 1,
            },
        };

    private static HttpResponseMessage BinaryResponse(byte[] payload, string contentType)
    {
        var content = new ByteArrayContent(payload);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
    }

    private static byte[] RandomBytes(int count)
    {
        var bytes = new byte[count];

        // Deterministic-enough filler; content value is irrelevant, only fidelity matters.
        for (int i = 0; i < count; i++)
        {
            bytes[i] = (byte)(i % 251);
        }

        return bytes;
    }

    private static string NewTempPath() =>
        Path.Combine(Path.GetTempPath(), "roadbed_dl_" + Guid.NewGuid().ToString("N") + ".bin");

    private static void Cleanup(string dest)
    {
        foreach (string path in new[] { dest, dest + ".part" })
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
                // Best-effort test cleanup.
            }
        }
    }

    #endregion Private Methods

    #region Private Types

    /// <summary>
    /// Read-only stream that yields a limited number of payload bytes, then throws
    /// <see cref="IOException"/> to simulate a dropped connection mid-body.
    /// </summary>
    private sealed class FailingStream : Stream
    {
        private readonly byte[] _data;
        private readonly int _failAfterBytes;
        private int _position;

        public FailingStream(byte[] data, int failAfterBytes)
        {
            this._data = data;
            this._failAfterBytes = failAfterBytes;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => this._position;
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (this._position >= this._failAfterBytes)
            {
                throw new IOException("Simulated mid-stream connection drop.");
            }

            int available = Math.Min(this._failAfterBytes, this._data.Length) - this._position;
            int n = Math.Min(count, available);
            Array.Copy(this._data, this._position, buffer, offset, n);
            this._position += n;
            return n;
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (this._position >= this._failAfterBytes)
            {
                throw new IOException("Simulated mid-stream connection drop.");
            }

            int available = Math.Min(this._failAfterBytes, this._data.Length) - this._position;
            int n = Math.Min(buffer.Length, available);
            this._data.AsSpan(this._position, n).CopyTo(buffer.Span);
            this._position += n;
            return ValueTask.FromResult(n);
        }

        public override void Flush() => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    #endregion Private Types
}
