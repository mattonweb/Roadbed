namespace Roadbed.Test.Unit.Logging;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Roadbed.Logging;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="LoggingCheckpointedJobExtensions"/> class.
/// </summary>
/// <remarks>
/// The extension methods resolve the checkpoint service from the process-wide
/// <see cref="ServiceLocator"/>, so these tests swap that static and must not run
/// in parallel with each other or with other tests.
/// </remarks>
[TestClass]
[DoNotParallelize]
public sealed class LoggingCheckpointedJobExtensionsTests
{
    #region Public Methods

    /// <summary>
    /// Restores an empty <see cref="ServiceLocator"/> after each test.
    /// </summary>
    [TestCleanup]
    public void TestCleanup()
    {
        ServiceLocator.Reset();
    }

    /// <summary>
    /// Unit test to verify that calling a checkpoint operation with no checkpoint
    /// service registered fails loudly and names what is missing, rather than
    /// behaving as if the job had never run.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task ReadCheckpointAsync_NoServiceRegistered_ThrowsInvalidOperationException()
    {
        // Arrange (Given)
        ServiceLocator.Reset();
        ILoggingCheckpointedJob job = CreateJob();
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            await job.ReadCheckpointAsync("criteria-rematch", CancellationToken.None);
        }
        catch (InvalidOperationException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "Reading a checkpoint without a registered service must throw.");
    }

    /// <summary>
    /// Unit test to verify that a null job is rejected before the service is
    /// resolved.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task ReadCheckpointAsync_NullJob_ThrowsArgumentNullException()
    {
        // Arrange (Given)
        ILoggingCheckpointedJob? job = null;
        ArgumentNullException? caughtException = null;

        // Act (When)
        try
        {
            await job!.ReadCheckpointAsync("criteria-rematch", CancellationToken.None);
        }
        catch (ArgumentNullException ex)
        {
            caughtException = ex;
        }

        // Assert (Then)
        Assert.IsNotNull(
            caughtException,
            "A null job must be rejected.");
        Assert.AreEqual(
            "job",
            caughtException.ParamName,
            "The exception must name the job parameter.");
    }

    /// <summary>
    /// Unit test to verify that reading a checkpoint delegates to the registered
    /// service with the job and key unchanged, and returns its answer.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task ReadCheckpointAsync_ServiceRegistered_ReturnsServiceResult()
    {
        // Arrange (Given)
        ILoggingCheckpointedJob job = CreateJob();
        var expected = new LoggingJobCheckpoint { Version = 3 };
        var service = new Mock<ILoggingJobCheckpointService>(MockBehavior.Strict);
        service
            .Setup(s => s.ReadAsync(job, "criteria-rematch", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        RegisterService(service.Object);

        // Act (When)
        LoggingJobCheckpoint? actual = await job.ReadCheckpointAsync("criteria-rematch", CancellationToken.None);

        // Assert (Then)
        Assert.AreSame(
            expected,
            actual,
            "The extension must return the service's checkpoint unchanged.");
    }

    /// <summary>
    /// Unit test to verify that recording a run start delegates to the
    /// registered service.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task MarkCheckpointRunStartedAsync_ServiceRegistered_DelegatesToService()
    {
        // Arrange (Given)
        ILoggingCheckpointedJob job = CreateJob();
        var service = new Mock<ILoggingJobCheckpointService>(MockBehavior.Strict);
        service
            .Setup(s => s.MarkRunStartedAsync(job, "criteria-rematch", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        RegisterService(service.Object);

        // Act (When)
        await job.MarkCheckpointRunStartedAsync("criteria-rematch", CancellationToken.None);

        // Assert (Then)
        service.Verify(
            s => s.MarkRunStartedAsync(job, "criteria-rematch", It.IsAny<CancellationToken>()),
            Times.Once,
            "The extension must record the run start through the service exactly once.");
    }

    /// <summary>
    /// Unit test to verify that advancing passes every argument through to the
    /// registered service and returns its compare-and-set answer.
    /// </summary>
    /// <returns>Task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task TryAdvanceCheckpointAsync_ServiceRegistered_PassesArgumentsThrough()
    {
        // Arrange (Given)
        ILoggingCheckpointedJob job = CreateJob();
        var position = new LoggingJobCheckpointPosition { Id = 42 };
        var service = new Mock<ILoggingJobCheckpointService>(MockBehavior.Strict);
        service
            .Setup(s => s.TryAdvanceAsync(job, "criteria-rematch", 3, position, "fire-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        RegisterService(service.Object);

        // Act (When)
        bool won = await job.TryAdvanceCheckpointAsync("criteria-rematch", 3, position, "fire-1", CancellationToken.None);

        // Assert (Then)
        Assert.IsFalse(
            won,
            "The extension must return the service's compare-and-set answer.");
        service.Verify(
            s => s.TryAdvanceAsync(job, "criteria-rematch", 3, position, "fire-1", It.IsAny<CancellationToken>()),
            Times.Once,
            "The extension must pass the key, version, position and fire instance id through unchanged.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates the checkpointed job the tests use.
    /// </summary>
    /// <returns>A job identity.</returns>
    private static ILoggingCheckpointedJob CreateJob()
    {
        var job = new Mock<ILoggingCheckpointedJob>(MockBehavior.Strict);
        job.SetupGet(j => j.Name).Returns("HelpWantedMatch");
        job.SetupGet(j => j.JobGroup).Returns("helpwanted");
        return job.Object;
    }

    /// <summary>
    /// Points <see cref="ServiceLocator"/> at a container holding only the given service.
    /// </summary>
    /// <param name="service">The checkpoint service to register.</param>
    private static void RegisterService(ILoggingJobCheckpointService service)
    {
        var services = new ServiceCollection();
        services.AddSingleton(service);
        ServiceLocator.SetLocatorProvider(services.BuildServiceProvider());
    }

    #endregion Private Methods
}
