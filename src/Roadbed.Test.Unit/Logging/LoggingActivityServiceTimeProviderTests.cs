namespace Roadbed.Test.Unit.Logging;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Roadbed.Logging;

/// <summary>
/// Verifies that the injected <see cref="TimeProvider"/> is the sole source
/// of framework-stamped timestamps written by
/// <see cref="LoggingActivityService"/>.
/// </summary>
/// <remarks>
/// Step 4 of the TimeProvider migration brief: prove an injected fake clock
/// drives every <c>created_on</c> / <c>last_modified_on</c> stamp the service
/// writes. Without the seam, these tests would be impossible — the activity
/// row's UTC timestamps would always be <c>DateTime.UtcNow</c>.
/// </remarks>
[TestClass]
public class LoggingActivityServiceTimeProviderTests
{
    #region Public Methods

    /// <summary>
    /// Verifies that <see cref="LoggingActivityService.BeginAsync"/> stamps
    /// the activity row's <c>created_on</c> and <c>last_modified_on</c>
    /// from the injected <see cref="TimeProvider"/>, not from the wall clock.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task BeginAsync_WithInjectedTimeProvider_StampsCreatedOnFromFakeClock()
    {
        // Arrange (Given) — fix the fake clock at a precise instant well
        // away from real "now" so the assertion proves the stamp came
        // from the injection, not from a coincident wall-clock read.
        var fakeInstant = new DateTimeOffset(2026, 6, 18, 12, 34, 56, TimeSpan.Zero);
        var fakeTime = new FakeTimeProvider(fakeInstant);

        var activityRepository = new Mock<ILoggingActivityRepository>();
        LoggingActivity? captured = null;
        activityRepository
            .Setup(r => r.InsertAsync(It.IsAny<LoggingActivity>(), It.IsAny<CancellationToken>()))
            .Callback<LoggingActivity, CancellationToken>((entity, _) => captured = entity)
            .Returns(Task.CompletedTask);

        var inputRepository = new Mock<ILoggingActivityInputRepository>();

        var service = new LoggingActivityService(
            activityRepository.Object,
            inputRepository.Object,
            new LoggingOptions { Application = "test-app" },
            fakeTime,
            NullLogger<LoggingActivityService>.Instance);

        // Act (When)
        using LoggingActivityScope scope = await service.BeginAsync(
            new LoggingActivityBeginRequest
            {
                Id = "01TIMEPROVIDERACTIVITYIDXX",
                ActivityType = "ingestion",
                Target = "ops.places",
            });

        // Assert (Then)
        Assert.IsNotNull(
            captured,
            "Repository.InsertAsync should have been called.");
        Assert.AreEqual(
            fakeInstant.UtcDateTime,
            captured!.CreatedOn,
            "CreatedOn must come from the injected TimeProvider, not the wall clock.");
        Assert.AreEqual(
            fakeInstant.UtcDateTime,
            captured.LastModifiedOn,
            "LastModifiedOn must come from the injected TimeProvider, not the wall clock.");
        Assert.AreEqual(
            fakeInstant.UtcDateTime,
            scope.CreatedOn,
            "Scope.CreatedOn must match the row stamp so partition-pruned UPDATEs land on the right MySQL partition.");
    }

    /// <summary>
    /// Verifies that advancing the fake clock between two <c>BeginAsync</c>
    /// calls produces two different <c>created_on</c> stamps, both drawn
    /// from the fake — proving the service does not cache the timestamp.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task BeginAsync_FakeClockAdvances_StampsTrackAdvancedTime()
    {
        // Arrange (Given)
        var first = new DateTimeOffset(2026, 6, 18, 12, 34, 56, TimeSpan.Zero);
        var fakeTime = new FakeTimeProvider(first);

        var activityRepository = new Mock<ILoggingActivityRepository>();
        var captured = new System.Collections.Generic.List<LoggingActivity>();
        activityRepository
            .Setup(r => r.InsertAsync(It.IsAny<LoggingActivity>(), It.IsAny<CancellationToken>()))
            .Callback<LoggingActivity, CancellationToken>((entity, _) => captured.Add(entity))
            .Returns(Task.CompletedTask);

        var inputRepository = new Mock<ILoggingActivityInputRepository>();

        var service = new LoggingActivityService(
            activityRepository.Object,
            inputRepository.Object,
            new LoggingOptions { Application = "test-app" },
            fakeTime,
            NullLogger<LoggingActivityService>.Instance);

        // Act (When)
        using (await service.BeginAsync(new LoggingActivityBeginRequest { Id = "01FIRSTACTIVITYIDXXXXXXXXXX" }))
        {
        }

        fakeTime.Advance(TimeSpan.FromMinutes(5));

        using (await service.BeginAsync(new LoggingActivityBeginRequest { Id = "01SECONDACTIVITYIDXXXXXXXX" }))
        {
        }

        // Assert (Then)
        Assert.AreEqual(2, captured.Count, "Both activities should have been inserted.");
        Assert.AreEqual(
            first.UtcDateTime,
            captured[0].CreatedOn,
            "First CreatedOn should be the initial fake instant.");
        Assert.AreEqual(
            first.UtcDateTime.AddMinutes(5),
            captured[1].CreatedOn,
            "Second CreatedOn should reflect the advanced fake clock, proving stamps are not cached.");
    }

    #endregion Public Methods
}
