namespace Roadbed.Test.Unit.Logging;

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Roadbed.Logging;

/// <summary>
/// Unit tests for <see cref="LoggingActivityService"/>.
/// </summary>
[TestClass]
public class LoggingActivityServiceTests
{
    #region Public Methods

    /// <summary>
    /// Verifies that BeginAsync inserts a running activity row, returns a
    /// scope that exposes the supplied identifier, and surfaces an Activity-
    /// derived trace identifier.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task BeginAsync_HappyPath_InsertsRunningRowAndReturnsScope()
    {
        // Arrange (Given)
        const string ActivityId = "01BEGINACTIVITYIDXXXXXXXXX";
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
            TimeProvider.System,
            NullLogger<LoggingActivityService>.Instance);

        // Act (When)
        using LoggingActivityScope returnedScope = await service.BeginAsync(
            new LoggingActivityBeginRequest
            {
                Id = ActivityId,
                ActivityType = "ingestion",
                Target = "ops.places",
                Application = "override-app",
            });

        // Assert (Then)
        Assert.IsNotNull(captured);
        Assert.AreEqual(ActivityId, captured!.Id);
        Assert.AreEqual(LoggingActivityStatus.Running, captured.Status);
        Assert.AreEqual("ingestion", captured.ActivityType);
        Assert.AreEqual("ops.places", captured.Target);
        Assert.AreEqual("override-app", captured.Application);
        Assert.IsNotNull(captured.StartedOn);
        Assert.IsNotNull(captured.LastHeartbeatOn);
        Assert.AreEqual(DateTimeKind.Utc, captured.CreatedOn.Kind, "CreatedOn must be stamped explicitly with a UTC value.");
        Assert.AreEqual(DateTimeKind.Utc, captured.LastModifiedOn.Kind, "LastModifiedOn must be stamped explicitly with a UTC value.");
        Assert.AreEqual(captured.CreatedOn, returnedScope.CreatedOn, "Scope.CreatedOn must match the row's stamped created_on so update WHEREs prune to one partition.");
        Assert.AreEqual(ActivityId, returnedScope.ActivityId);
        Assert.AreEqual(32, returnedScope.TraceId!.Length, "Scope should expose a W3C trace id.");
        activityRepository.Verify(
            r => r.InsertAsync(It.IsAny<LoggingActivity>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Regression test for the AsyncLocal/ExecutionContext bug that left
    /// <c>log_entries.activity_id</c> NULL: after <c>await BeginAsync(...)</c>
    /// returns, the ambient state the service pushed must be live in the
    /// <em>caller's</em> execution context so the exporter can read it from
    /// subsequent log records. We assert on <see cref="Activity.Current"/>
    /// (the exporter's fallback channel), which shares the same
    /// AsyncLocal-flow mechanics as the MEL scope.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task BeginAsync_AfterAwait_AmbientActivityFlowsToCaller()
    {
        // Arrange (Given)
        const string ActivityId = "01AMBIENTFLOWSTOCALLERXXXX";
        const string ActivityIdTagKey = "roadbed.activity_id";
        var activityRepository = new Mock<ILoggingActivityRepository>();
        activityRepository
            .Setup(r => r.InsertAsync(It.IsAny<LoggingActivity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new LoggingActivityService(
            activityRepository.Object,
            Mock.Of<ILoggingActivityInputRepository>(),
            new LoggingOptions(),
            TimeProvider.System,
            NullLogger<LoggingActivityService>.Instance);

        Assert.IsNull(Activity.Current, "Precondition: no ambient activity before BeginAsync.");

        // Act (When) — emulate the documented `using var scope = await BeginAsync(...)`
        // pattern and inspect the ambient state on the caller's own async frame.
        using LoggingActivityScope scope = await service.BeginAsync(
            new LoggingActivityBeginRequest { Id = ActivityId, ActivityType = "ingestion" });

        // Assert (Then) — the ambient survived the await into the caller's context.
        Assert.IsNotNull(Activity.Current, "Activity.Current must be live on the caller after the await.");
        Assert.AreEqual(
            ActivityId,
            Activity.Current!.GetTagItem(ActivityIdTagKey)?.ToString(),
            "The caller's ambient activity must carry the activity id the exporter stamps into log_entries.activity_id.");
    }

    /// <summary>
    /// Verifies that disposing the scope reverts the caller's ambient
    /// <see cref="Activity.Current"/> so later log lines are not stamped.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task BeginAsync_ScopeDisposed_AmbientActivityReverts()
    {
        // Arrange (Given)
        var activityRepository = new Mock<ILoggingActivityRepository>();
        activityRepository
            .Setup(r => r.InsertAsync(It.IsAny<LoggingActivity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new LoggingActivityService(
            activityRepository.Object,
            Mock.Of<ILoggingActivityInputRepository>(),
            new LoggingOptions(),
            TimeProvider.System,
            NullLogger<LoggingActivityService>.Instance);

        // Act (When)
        LoggingActivityScope scope = await service.BeginAsync(
            new LoggingActivityBeginRequest { Id = "01AMBIENTREVERTSXXXXXXXXXX", ActivityType = "ingestion" });
        Assert.IsNotNull(Activity.Current, "Activity.Current must be live before dispose.");
        scope.Dispose();

        // Assert (Then)
        Assert.IsNull(Activity.Current, "Disposing the scope must revert Activity.Current on the caller.");
    }

    /// <summary>
    /// Verifies that BeginAsync defaults RootActivityId to the supplied Id when omitted.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task BeginAsync_OmittedRootActivityId_DefaultsToId()
    {
        // Arrange (Given)
        const string ActivityId = "01ROOTDEFAULTSTOSELFXXXXXX";
        var activityRepository = new Mock<ILoggingActivityRepository>();
        LoggingActivity? captured = null;
        activityRepository
            .Setup(r => r.InsertAsync(It.IsAny<LoggingActivity>(), It.IsAny<CancellationToken>()))
            .Callback<LoggingActivity, CancellationToken>((entity, _) => captured = entity)
            .Returns(Task.CompletedTask);

        var service = new LoggingActivityService(
            activityRepository.Object,
            Mock.Of<ILoggingActivityInputRepository>(),
            new LoggingOptions(),
            TimeProvider.System,
            NullLogger<LoggingActivityService>.Instance);

        // Act (When)
        using LoggingActivityScope discardedScope = await service.BeginAsync(
            new LoggingActivityBeginRequest { Id = ActivityId, ActivityType = "ingestion" });

        // Assert (Then)
        Assert.AreEqual(ActivityId, captured!.RootActivityId);
    }

    /// <summary>
    /// Verifies that BeginAsync rolls back its scope and activity when the
    /// repository insert throws.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task BeginAsync_RepositoryThrows_ReleasesScopeAndPropagates()
    {
        // Arrange (Given)
        var activityRepository = new Mock<ILoggingActivityRepository>();
        var thrown = new InvalidOperationException("db down");
        activityRepository
            .Setup(r => r.InsertAsync(It.IsAny<LoggingActivity>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(thrown);

        var service = new LoggingActivityService(
            activityRepository.Object,
            Mock.Of<ILoggingActivityInputRepository>(),
            new LoggingOptions(),
            TimeProvider.System,
            NullLogger<LoggingActivityService>.Instance);

        // Act (When)
        InvalidOperationException? caught = null;
        try
        {
            using var discardedScope = await service.BeginAsync(
                new LoggingActivityBeginRequest { Id = "01ROLLBACKACTIVITYIDXXXXXX", ActivityType = "ingestion" });
        }
        catch (InvalidOperationException ex)
        {
            caught = ex;
        }

        // Assert (Then)
        Assert.AreSame(thrown, caught, "Repository exception should propagate to caller.");
    }

    /// <summary>
    /// Verifies that HeartbeatAsync delegates to the repository with a UtcNow timestamp.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task HeartbeatAsync_DelegatesToRepositoryWithUtcNow()
    {
        // Arrange (Given)
        var activityRepository = new Mock<ILoggingActivityRepository>();
        DateTime? captured = null;
        activityRepository
            .Setup(r => r.RecordHeartbeatAsync(It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Callback<string, DateTime?, DateTime, CancellationToken>((_, _, when, _) => captured = when)
            .Returns(Task.CompletedTask);

        var service = new LoggingActivityService(
            activityRepository.Object,
            Mock.Of<ILoggingActivityInputRepository>(),
            new LoggingOptions(),
            TimeProvider.System,
            NullLogger<LoggingActivityService>.Instance);

        DateTime before = DateTime.UtcNow.AddSeconds(-1);

        // Act (When)
        await service.HeartbeatAsync("01HEARTBEATACTIVITYIDXXXXX");

        // Assert (Then)
        Assert.IsNotNull(captured);
        Assert.IsTrue(captured!.Value >= before, "Heartbeat timestamp should be UtcNow.");
        Assert.AreEqual(DateTimeKind.Utc, captured.Value.Kind);
    }

    /// <summary>
    /// Verifies that UpdateAsync forwards the patch request to the repository.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task UpdateAsync_ForwardsRequestToRepository()
    {
        // Arrange (Given)
        var activityRepository = new Mock<ILoggingActivityRepository>();
        var request = new LoggingActivityUpdateRequest
        {
            ActivityId = "01UPDATEACTIVITYIDXXXXXXXX",
            Target = "ops.cousubs",
            QuartzJobName = "Pebble.LoadCousubs",
        };

        var service = new LoggingActivityService(
            activityRepository.Object,
            Mock.Of<ILoggingActivityInputRepository>(),
            new LoggingOptions(),
            TimeProvider.System,
            NullLogger<LoggingActivityService>.Instance);

        // Act (When)
        await service.UpdateAsync(request);

        // Assert (Then)
        activityRepository.Verify(
            r => r.UpdateCurrentStateAsync(request, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Verifies that CompleteAsync rejects an explicit Failed status (callers must use FailAsync).
    /// </summary>
    [TestMethod]
    public void CompleteAsync_StatusIsFailed_Throws()
    {
        // Arrange (Given)
        var service = new LoggingActivityService(
            Mock.Of<ILoggingActivityRepository>(),
            Mock.Of<ILoggingActivityInputRepository>(),
            new LoggingOptions(),
            TimeProvider.System,
            NullLogger<LoggingActivityService>.Instance);

        // Act (When) + Assert (Then)
        Assert.ThrowsExactly<ArgumentException>(
            () => service.CompleteAsync("01COMPLETEACTIVITYIDXXXXXX", LoggingActivityStatus.Failed).GetAwaiter().GetResult());
    }

    /// <summary>
    /// Verifies that FailAsync forwards the exception type and message to the repository.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task FailAsync_ForwardsExceptionDetailsToRepository()
    {
        // Arrange (Given)
        var activityRepository = new Mock<ILoggingActivityRepository>();
        string? capturedError = null;
        string? capturedErrorType = null;
        activityRepository
            .Setup(r => r.FailAsync(
                It.IsAny<string>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, DateTime?, DateTime, string, string, CancellationToken>(
                (_, _, _, error, errorType, _) =>
                {
                    capturedError = error;
                    capturedErrorType = errorType;
                })
            .Returns(Task.CompletedTask);

        var service = new LoggingActivityService(
            activityRepository.Object,
            Mock.Of<ILoggingActivityInputRepository>(),
            new LoggingOptions(),
            TimeProvider.System,
            NullLogger<LoggingActivityService>.Instance);

        // Act (When)
        await service.FailAsync("01FAILACTIVITYIDAAAAAAAAAA", new InvalidOperationException("boom"));

        // Assert (Then)
        Assert.AreEqual("boom", capturedError);
        Assert.AreEqual("System.InvalidOperationException", capturedErrorType);
    }

    /// <summary>
    /// Verifies that AddInputAsync constructs the lineage edge entity and delegates insertion.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task AddInputAsync_ForwardsLineageEdgeToRepository()
    {
        // Arrange (Given)
        var inputRepository = new Mock<ILoggingActivityInputRepository>();
        LoggingActivityInput? captured = null;
        inputRepository
            .Setup(r => r.InsertAsync(It.IsAny<LoggingActivityInput>(), It.IsAny<CancellationToken>()))
            .Callback<LoggingActivityInput, CancellationToken>((entity, _) => captured = entity)
            .Returns(Task.CompletedTask);

        var service = new LoggingActivityService(
            Mock.Of<ILoggingActivityRepository>(),
            inputRepository.Object,
            new LoggingOptions(),
            TimeProvider.System,
            NullLogger<LoggingActivityService>.Instance);

        // Act (When)
        await service.AddInputAsync(
            "01CONSUMERACTIVITYIDXXXXXX",
            "01INPUTACTIVITYIDAAAAAAAAA",
            "places");

        // Assert (Then)
        Assert.IsNotNull(captured);
        Assert.AreEqual("01CONSUMERACTIVITYIDXXXXXX", captured!.ActivityId);
        Assert.AreEqual("01INPUTACTIVITYIDAAAAAAAAA", captured.InputActivityId);
        Assert.AreEqual("places", captured.InputRole);
    }

    #endregion Public Methods
}
