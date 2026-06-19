namespace Roadbed.Test.Unit.Net;

using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Net;

/// <summary>
/// Verifies that the injected <see cref="TimeProvider"/> is the sole source
/// of retry/backoff timing in <see cref="NetHttpClient"/>, so a
/// <see cref="FakeTimeProvider"/> can virtualize the wait and tests do not
/// pay the real wall-clock backoff cost.
/// </summary>
[TestClass]
public class NetHttpClientTimeProviderTests
{
    #region Public Methods

    /// <summary>
    /// Verifies that a fake clock virtualizes the retry backoff: a
    /// configured 5-second backoff completes in milliseconds of real time
    /// once the fake clock advances by 5 seconds.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task MakeHttpRequestAsync_FakeTimeProvider_VirtualizesRetryBackoff()
    {
        // Arrange (Given) — DelayMultiplierInSeconds = 5 means the first
        // retry should wait Math.Pow(5, 0) = 1 second. Use 5 so a real
        // wall-clock sleep would be visible if injection failed.
        const int DelayMultiplierInSeconds = 5;
        var startInstant = new DateTimeOffset(2026, 6, 18, 12, 0, 0, TimeSpan.Zero);
        var fakeTime = new FakeTimeProvider(startInstant);

        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(HttpStatusCode.ServiceUnavailable, "down");
        handler.EnqueueResponse(HttpStatusCode.OK, "up");

        var client = new NetHttpClient(
            handler,
            fakeTime,
            NullLogger<NetHttpClient>.Instance);

        var request = new NetHttpRequest
        {
            HttpEndPoint = new Uri("https://api.example.com/test"),
            Method = HttpMethod.Get,
            TimeoutInSecondsPerAttempt = 30,
            RetryPattern = new NetHttpRetryPattern
            {
                MaxAttempts = 1,
                DelayMultiplierInSeconds = DelayMultiplierInSeconds,
            },
        };

        // Act (When) — kick off the request. With FakeTimeProvider it
        // suspends on Task.Delay until the clock advances.
        var wallClock = Stopwatch.StartNew();
        Task<NetHttpResponse<string>> sendTask = client.MakeHttpRequestAsync<string>(request);

        // Wait until the first HTTP attempt lands so the retry path has
        // entered Task.Delay before we advance the fake clock. Bounded so
        // a regression that breaks injection cannot hang the suite.
        for (int spins = 0; spins < 200 && handler.SendAsyncCallCount < 1; spins++)
        {
            await Task.Delay(5);
        }

        // Give the retry path a moment to register the Task.Delay timer
        // with FakeTimeProvider before we fire it.
        await Task.Delay(20);

        // Advance the fake clock by the configured backoff. The Task.Delay
        // callback fires synchronously, the awaiting code resumes, and the
        // second HTTP attempt is made.
        fakeTime.Advance(TimeSpan.FromSeconds(DelayMultiplierInSeconds));

        NetHttpResponse<string> response = await sendTask;
        wallClock.Stop();

        // Assert (Then)
        Assert.IsTrue(
            response.IsSuccessStatusCode,
            "Second attempt should succeed after virtualized backoff.");
        Assert.AreEqual(
            2,
            handler.SendAsyncCallCount,
            "Two HTTP attempts should have been made (initial 503 plus one retry).");
        Assert.IsTrue(
            wallClock.Elapsed < TimeSpan.FromSeconds(DelayMultiplierInSeconds),
            $"Backoff must be virtualized — real wall-clock elapsed ({wallClock.Elapsed.TotalMilliseconds}ms) should be far below the configured {DelayMultiplierInSeconds}s backoff.");
    }

    #endregion Public Methods
}
