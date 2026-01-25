namespace Roadbed.Test.Unit.Scheduling;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Quartz;
using Roadbed.Scheduling;
using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Contains unit tests for verifying the Context property behavior in BaseSchedulingJob.
/// </summary>
[TestClass]
public class BaseSchedulingJobContextTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that Context property throws when accessed outside execution.
    /// </summary>
    [TestMethod]
    public void Context_AccessedOutsideExecution_ThrowsInvalidOperationException()
    {
        // Arrange (Given)
        var mockLogger = new Mock<ILogger<TestSchedulingJob>>();
        var job = new TestSchedulingJob(mockLogger.Object);
        InvalidOperationException? caughtException = null;

        // Act (When)
        try
        {
            var context = job.GetContextForTest();
        }
        catch (InvalidOperationException ex)
        {
            caughtException = ex;
        }

        // Assert (Then)
        Assert.IsNotNull(
            caughtException,
            "Context should throw InvalidOperationException when accessed outside ExecuteAsync.");
        StringAssert.Contains(
            caughtException.Message,
            "only available during job execution",
            "Exception message should explain when Context is available.");
    }

    /// <summary>
    /// Unit test to verify that Context property is available during ExecuteAsync execution.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task Context_AccessedDuringExecution_ReturnsContext()
    {
        // Arrange (Given)
        var mockLogger = new Mock<ILogger<TestSchedulingJob>>();
        var job = new TestSchedulingJob(mockLogger.Object);
        var mockContext = this.CreateMockContext();
        IJobExecutionContext? capturedContext = null;

        job.OnExecuteAsync = (ct) =>
        {
            capturedContext = job.GetContextForTest();
            return Task.CompletedTask;
        };

        // Act (When)
        await ((IJob)job).Execute(mockContext.Object);

        // Assert (Then)
        Assert.IsNotNull(
            capturedContext,
            "Context should be available during ExecuteAsync execution.");
        Assert.AreSame(
            mockContext.Object,
            capturedContext,
            "Context should return the same instance provided by Quartz.");
    }

    /// <summary>
    /// Unit test to verify that Context property is cleared after ExecuteAsync completes.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task Context_AfterExecutionCompletes_IsCleared()
    {
        // Arrange (Given)
        var mockLogger = new Mock<ILogger<TestSchedulingJob>>();
        var job = new TestSchedulingJob(mockLogger.Object);
        var mockContext = this.CreateMockContext();

        // Act (When)
        await ((IJob)job).Execute(mockContext.Object);

        InvalidOperationException? caughtException = null;
        try
        {
            var context = job.GetContextForTest();
        }
        catch (InvalidOperationException ex)
        {
            caughtException = ex;
        }

        // Assert (Then)
        Assert.IsNotNull(
            caughtException,
            "Context should throw InvalidOperationException after ExecuteAsync completes.");
    }

    /// <summary>
    /// Unit test to verify that Context property is cleared even when ExecuteAsync throws.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task Context_AfterExecutionThrows_IsCleared()
    {
        // Arrange (Given)
        var mockLogger = new Mock<ILogger<TestSchedulingJob>>();
        var job = new TestSchedulingJob(mockLogger.Object);
        var mockContext = this.CreateMockContext();

        job.OnExecuteAsync = (ct) =>
        {
            throw new InvalidOperationException("Test exception");
        };

        // Act (When)
        try
        {
            await ((IJob)job).Execute(mockContext.Object);
        }
        catch (InvalidOperationException)
        {
            // Expected exception
        }

        InvalidOperationException? caughtException = null;
        try
        {
            var context = job.GetContextForTest();
        }
        catch (InvalidOperationException ex)
        {
            caughtException = ex;
        }

        // Assert (Then)
        Assert.IsNotNull(
            caughtException,
            "Context should throw InvalidOperationException after ExecuteAsync throws exception.");
    }

    /// <summary>
    /// Unit test to verify that job can set Context.Result during execution.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task Context_SetResultDuringExecution_StoresResult()
    {
        // Arrange (Given)
        var mockLogger = new Mock<ILogger<TestSchedulingJob>>();
        var job = new TestSchedulingJob(mockLogger.Object);
        var mockContext = this.CreateMockContext();
        string expectedResult = "Processed 1,234 records";

        job.OnExecuteAsync = (ct) =>
        {
            var ctx = job.GetContextForTest();
            ctx.Result = expectedResult;
            return Task.CompletedTask;
        };

        // Act (When)
        await ((IJob)job).Execute(mockContext.Object);

        // Assert (Then)
        mockContext.VerifySet(
            c => c.Result = expectedResult,
            Times.Once,
            "Job should be able to set Result on Context during execution.");
    }

    /// <summary>
    /// Unit test to verify that IJob.Execute throws when context is null.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task Execute_NullContext_ThrowsArgumentNullException()
    {
        // Arrange (Given)
        var mockLogger = new Mock<ILogger<TestSchedulingJob>>();
        var job = new TestSchedulingJob(mockLogger.Object);
        IJobExecutionContext? nullContext = null;
        ArgumentNullException? caughtException = null;

        // Act (When)
        try
        {
            await ((IJob)job).Execute(nullContext!);
        }
        catch (ArgumentNullException ex)
        {
            caughtException = ex;
        }

        // Assert (Then)
        Assert.IsNotNull(
            caughtException,
            "Execute should throw ArgumentNullException when context is null.");
        Assert.AreEqual(
            "context",
            caughtException.ParamName,
            "Exception should indicate correct parameter name.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates a mock IJobExecutionContext for testing.
    /// </summary>
    /// <returns>Mock context configured for testing.</returns>
    private Mock<IJobExecutionContext> CreateMockContext()
    {
        var mockContext = new Mock<IJobExecutionContext>();
        mockContext.SetupProperty(c => c.Result);
        mockContext.Setup(c => c.CancellationToken).Returns(CancellationToken.None);
        return mockContext;
    }

    #endregion Private Methods
}

/// <summary>
/// Test implementation of BaseSchedulingJob for testing Context property.
/// </summary>
public class TestSchedulingJob : BaseSchedulingJob<TestSchedulingJob>
{
    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="TestSchedulingJob"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public TestSchedulingJob(ILogger<TestSchedulingJob> logger)
        : base(
            name: "TestJob",
            description: "Test job for context tests",
            schedule: new SchedulingSchedule(TimeSpan.FromMinutes(5)),
            logger: logger)
    {
    }

    #endregion Public Constructors

    #region Public Properties

    /// <summary>
    /// Gets or sets the delegate to execute during ExecuteAsync.
    /// </summary>
    public Func<CancellationToken, Task>? OnExecuteAsync { get; set; }

    #endregion Public Properties

    #region Public Methods

    /// <summary>
    /// Executes the job.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        if (this.OnExecuteAsync != null)
        {
            return this.OnExecuteAsync(cancellationToken);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the Context property for testing purposes.
    /// </summary>
    /// <returns>The current job execution context.</returns>
    public IJobExecutionContext GetContextForTest()
    {
        return this.Context;
    }

    #endregion Public Methods
}