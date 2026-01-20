namespace Roadbed.Test.Unit.Scheduling;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Scheduling;

/// <summary>
/// Contains unit tests for verifying the behavior of the BaseSchedulingJob class.
/// </summary>
[TestClass]
public class BaseSchedulingJobTests
{
    #region Public Methods

    #region Constructor Tests - Parameterless Constructor

    /// <summary>
    /// Unit test to verify that parameterless constructor initializes successfully.
    /// </summary>
    [TestMethod]
    public void Constructor_Parameterless_InitializesSuccessfully()
    {
        // Arrange (Given)
        var logger = new TestLogger<PropertyOverrideJob>();

        // Act (When)
        var job = new PropertyOverrideJob(logger);

        // Assert (Then)
        Assert.IsNotNull(
            job,
            "Job should be created successfully with parameterless constructor.");
    }

    #endregion Constructor Tests - Parameterless Constructor

    #region Constructor Tests - Full Constructor

    /// <summary>
    /// Unit test to verify that full constructor initializes properties correctly.
    /// </summary>
    [TestMethod]
    public void Constructor_WithAllParameters_InitializesPropertiesCorrectly()
    {
        // Arrange (Given)
        var logger = new TestLogger<ConstructorJob>();
        string expectedName = "TestJob";
        string expectedDescription = "Test job description";
        var expectedSchedule = new SchedulingSchedule(TimeSpan.FromMinutes(5));

        // Act (When)
        var job = new ConstructorJob(logger, expectedName, expectedDescription, expectedSchedule);

        // Assert (Then)
        Assert.AreEqual(
            expectedName,
            job.Name,
            "Name should be set to the provided value.");
        Assert.AreEqual(
            expectedDescription,
            job.Description,
            "Description should be set to the provided value.");
        Assert.AreSame(
            expectedSchedule,
            job.Schedule,
            "Schedule should be set to the provided value.");
    }

    /// <summary>
    /// Unit test to verify that full constructor throws ArgumentException when name is null.
    /// </summary>
    [TestMethod]
    public void Constructor_WithNullName_ThrowsArgumentException()
    {
        // Arrange (Given)
        var logger = new TestLogger<ConstructorJob>();
        string? nullName = null;
        string description = "Test description";
        var schedule = new SchedulingSchedule(TimeSpan.FromMinutes(5));
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            var job = new ConstructorJob(logger, nullName!, description, schedule);
        }
        catch (ArgumentException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "Constructor should throw ArgumentException when name is null.");
    }

    /// <summary>
    /// Unit test to verify that full constructor throws ArgumentException when name is empty.
    /// </summary>
    [TestMethod]
    public void Constructor_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange (Given)
        var logger = new TestLogger<ConstructorJob>();
        string emptyName = string.Empty;
        string description = "Test description";
        var schedule = new SchedulingSchedule(TimeSpan.FromMinutes(5));
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            var job = new ConstructorJob(logger, emptyName, description, schedule);
        }
        catch (ArgumentException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "Constructor should throw ArgumentException when name is empty.");
    }

    /// <summary>
    /// Unit test to verify that full constructor throws ArgumentException when name is whitespace.
    /// </summary>
    [TestMethod]
    public void Constructor_WithWhitespaceName_ThrowsArgumentException()
    {
        // Arrange (Given)
        var logger = new TestLogger<ConstructorJob>();
        string whitespaceName = "   ";
        string description = "Test description";
        var schedule = new SchedulingSchedule(TimeSpan.FromMinutes(5));
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            var job = new ConstructorJob(logger, whitespaceName, description, schedule);
        }
        catch (ArgumentException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "Constructor should throw ArgumentException when name is whitespace.");
    }

    /// <summary>
    /// Unit test to verify that full constructor throws ArgumentException when description is null.
    /// </summary>
    [TestMethod]
    public void Constructor_WithNullDescription_ThrowsArgumentException()
    {
        // Arrange (Given)
        var logger = new TestLogger<ConstructorJob>();
        string name = "TestJob";
        string? nullDescription = null;
        var schedule = new SchedulingSchedule(TimeSpan.FromMinutes(5));
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            var job = new ConstructorJob(logger, name, nullDescription!, schedule);
        }
        catch (ArgumentException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "Constructor should throw ArgumentException when description is null.");
    }

    /// <summary>
    /// Unit test to verify that full constructor throws ArgumentException when description is empty.
    /// </summary>
    [TestMethod]
    public void Constructor_WithEmptyDescription_ThrowsArgumentException()
    {
        // Arrange (Given)
        var logger = new TestLogger<ConstructorJob>();
        string name = "TestJob";
        string emptyDescription = string.Empty;
        var schedule = new SchedulingSchedule(TimeSpan.FromMinutes(5));
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            var job = new ConstructorJob(logger, name, emptyDescription, schedule);
        }
        catch (ArgumentException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "Constructor should throw ArgumentException when description is empty.");
    }

    /// <summary>
    /// Unit test to verify that full constructor throws ArgumentException when description is whitespace.
    /// </summary>
    [TestMethod]
    public void Constructor_WithWhitespaceDescription_ThrowsArgumentException()
    {
        // Arrange (Given)
        var logger = new TestLogger<ConstructorJob>();
        string name = "TestJob";
        string whitespaceDescription = "   ";
        var schedule = new SchedulingSchedule(TimeSpan.FromMinutes(5));
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            var job = new ConstructorJob(logger, name, whitespaceDescription, schedule);
        }
        catch (ArgumentException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "Constructor should throw ArgumentException when description is whitespace.");
    }

    /// <summary>
    /// Unit test to verify that full constructor throws ArgumentNullException when schedule is null.
    /// </summary>
    [TestMethod]
    public void Constructor_WithNullSchedule_ThrowsArgumentNullException()
    {
        // Arrange (Given)
        var logger = new TestLogger<ConstructorJob>();
        string name = "TestJob";
        string description = "Test description";
        SchedulingSchedule? nullSchedule = null;
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            var job = new ConstructorJob(logger, name, description, nullSchedule!);
        }
        catch (ArgumentNullException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "Constructor should throw ArgumentNullException when schedule is null.");
    }

    #endregion Constructor Tests - Full Constructor

    #region Property Tests - Property Override Pattern

    /// <summary>
    /// Unit test to verify that Name property returns overridden value.
    /// </summary>
    [TestMethod]
    public void Name_PropertyOverride_ReturnsOverriddenValue()
    {
        // Arrange (Given)
        var logger = new TestLogger<PropertyOverrideJob>();
        var job = new PropertyOverrideJob(logger);

        // Act (When)
        var name = job.Name;

        // Assert (Then)
        Assert.AreEqual(
            "PropertyOverrideJob",
            name,
            "Name should return the overridden value.");
    }

    /// <summary>
    /// Unit test to verify that Description property returns overridden value.
    /// </summary>
    [TestMethod]
    public void Description_PropertyOverride_ReturnsOverriddenValue()
    {
        // Arrange (Given)
        var logger = new TestLogger<PropertyOverrideJob>();
        var job = new PropertyOverrideJob(logger);

        // Act (When)
        var description = job.Description;

        // Assert (Then)
        Assert.AreEqual(
            "Job using property override pattern",
            description,
            "Description should return the overridden value.");
    }

    /// <summary>
    /// Unit test to verify that Schedule property returns overridden value.
    /// </summary>
    [TestMethod]
    public void Schedule_PropertyOverride_ReturnsOverriddenValue()
    {
        // Arrange (Given)
        var logger = new TestLogger<PropertyOverrideJob>();
        var job = new PropertyOverrideJob(logger);

        // Act (When)
        var schedule = job.Schedule;

        // Assert (Then)
        Assert.IsNotNull(
            schedule,
            "Schedule should return the overridden value.");
        Assert.AreEqual(
            SchedulingScheduleType.SimpleInterval,
            schedule.ScheduleType,
            "Schedule should have correct type.");
    }

    #endregion Property Tests - Property Override Pattern

    #region Property Tests - Constructor Pattern

    /// <summary>
    /// Unit test to verify that Name property returns constructor value.
    /// </summary>
    [TestMethod]
    public void Name_ConstructorPattern_ReturnsConstructorValue()
    {
        // Arrange (Given)
        var logger = new TestLogger<ConstructorJob>();
        string expectedName = "ConstructorJob";
        var job = new ConstructorJob(
            logger,
            expectedName,
            "Test description",
            new SchedulingSchedule(TimeSpan.FromMinutes(5)));

        // Act (When)
        var name = job.Name;

        // Assert (Then)
        Assert.AreEqual(
            expectedName,
            name,
            "Name should return the constructor value.");
    }

    /// <summary>
    /// Unit test to verify that Description property returns constructor value.
    /// </summary>
    [TestMethod]
    public void Description_ConstructorPattern_ReturnsConstructorValue()
    {
        // Arrange (Given)
        var logger = new TestLogger<ConstructorJob>();
        string expectedDescription = "Test description";
        var job = new ConstructorJob(
            logger,
            "TestJob",
            expectedDescription,
            new SchedulingSchedule(TimeSpan.FromMinutes(5)));

        // Act (When)
        var description = job.Description;

        // Assert (Then)
        Assert.AreEqual(
            expectedDescription,
            description,
            "Description should return the constructor value.");
    }

    /// <summary>
    /// Unit test to verify that Schedule property returns constructor value.
    /// </summary>
    [TestMethod]
    public void Schedule_ConstructorPattern_ReturnsConstructorValue()
    {
        // Arrange (Given)
        var logger = new TestLogger<ConstructorJob>();
        var expectedSchedule = new SchedulingSchedule(TimeSpan.FromMinutes(5));
        var job = new ConstructorJob(
            logger,
            "TestJob",
            "Test description",
            expectedSchedule);

        // Act (When)
        var schedule = job.Schedule;

        // Assert (Then)
        Assert.AreSame(
            expectedSchedule,
            schedule,
            "Schedule should return the constructor value.");
    }

    #endregion Property Tests - Constructor Pattern

    #region Property Tests - Error Cases

    /// <summary>
    /// Unit test to verify that Name property throws InvalidOperationException when not set.
    /// </summary>
    [TestMethod]
    public void Name_NotOverriddenOrSet_ThrowsInvalidOperationException()
    {
        // Arrange (Given)
        var logger = new TestLogger<IncompleteJob>();
        var job = new IncompleteJob(logger);
        bool exceptionThrown = false;
        string? exceptionMessage = null;

        // Act (When)
        try
        {
            var name = job.Name;
        }
        catch (InvalidOperationException ex)
        {
            exceptionThrown = true;
            exceptionMessage = ex.Message;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "Name property should throw InvalidOperationException when not set.");
        StringAssert.Contains(
            exceptionMessage!,
            "Name must be provided",
            "Exception message should explain that Name must be provided.");
        StringAssert.Contains(
            exceptionMessage!,
            nameof(IncompleteJob),
            "Exception message should include the job type name.");
    }

    /// <summary>
    /// Unit test to verify that Description property throws InvalidOperationException when not set.
    /// </summary>
    [TestMethod]
    public void Description_NotOverriddenOrSet_ThrowsInvalidOperationException()
    {
        // Arrange (Given)
        var logger = new TestLogger<IncompleteJob>();
        var job = new IncompleteJob(logger);
        bool exceptionThrown = false;
        string? exceptionMessage = null;

        // Act (When)
        try
        {
            var description = job.Description;
        }
        catch (InvalidOperationException ex)
        {
            exceptionThrown = true;
            exceptionMessage = ex.Message;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "Description property should throw InvalidOperationException when not set.");
        StringAssert.Contains(
            exceptionMessage!,
            "Description must be provided",
            "Exception message should explain that Description must be provided.");
        StringAssert.Contains(
            exceptionMessage!,
            nameof(IncompleteJob),
            "Exception message should include the job type name.");
    }

    /// <summary>
    /// Unit test to verify that Schedule property throws InvalidOperationException when not set.
    /// </summary>
    [TestMethod]
    public void Schedule_NotOverriddenOrSet_ThrowsInvalidOperationException()
    {
        // Arrange (Given)
        var logger = new TestLogger<IncompleteJob>();
        var job = new IncompleteJob(logger);
        bool exceptionThrown = false;
        string? exceptionMessage = null;

        // Act (When)
        try
        {
            var schedule = job.Schedule;
        }
        catch (InvalidOperationException ex)
        {
            exceptionThrown = true;
            exceptionMessage = ex.Message;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "Schedule property should throw InvalidOperationException when not set.");
        StringAssert.Contains(
            exceptionMessage!,
            "Schedule must be provided",
            "Exception message should explain that Schedule must be provided.");
        StringAssert.Contains(
            exceptionMessage!,
            nameof(IncompleteJob),
            "Exception message should include the job type name.");
    }

    #endregion Property Tests - Error Cases

    #region Integration Tests

    /// <summary>
    /// Unit test to verify that both constructor patterns can coexist.
    /// </summary>
    [TestMethod]
    public void BothConstructorPatterns_CanCoexist()
    {
        // Arrange (Given)
        var logger1 = new TestLogger<PropertyOverrideJob>();
        var logger2 = new TestLogger<ConstructorJob>();

        // Act (When)
        var job1 = new PropertyOverrideJob(logger1);
        var job2 = new ConstructorJob(
            logger2,
            "ConstructorJob",
            "Constructor-based job",
            new SchedulingSchedule(TimeSpan.FromMinutes(10)));

        // Assert (Then)
        Assert.AreEqual(
            "PropertyOverrideJob",
            job1.Name,
            "Property override job should have correct name.");
        Assert.AreEqual(
            "ConstructorJob",
            job2.Name,
            "Constructor job should have correct name.");
    }

    #endregion Integration Tests

    #endregion Public Methods

    #region Test Helper Classes

    /// <summary>
    /// Test logger implementation.
    /// </summary>
    /// <typeparam name="T">Type being logged.</typeparam>
    private class TestLogger<T> : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            // No-op for testing
        }
    }

    /// <summary>
    /// Job using property override pattern.
    /// </summary>
    private class PropertyOverrideJob : BaseSchedulingJob<PropertyOverrideJob>
    {
        public PropertyOverrideJob(ILogger<PropertyOverrideJob> logger)
            : base(logger)
        {
        }

        public override string Name => "PropertyOverrideJob";

        public override string Description => "Job using property override pattern";

        public override SchedulingSchedule Schedule => new SchedulingSchedule(TimeSpan.FromMinutes(15));

        public override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Job using constructor pattern.
    /// </summary>
    private class ConstructorJob : BaseSchedulingJob<ConstructorJob>
    {
        public ConstructorJob(
            ILogger<ConstructorJob> logger,
            string name,
            string description,
            SchedulingSchedule schedule)
            : base(name, description, schedule, logger)
        {
        }

        public override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Incomplete job that doesn't override properties (used to test error cases).
    /// </summary>
    private class IncompleteJob : BaseSchedulingJob<IncompleteJob>
    {
        public IncompleteJob(ILogger<IncompleteJob> logger)
            : base(logger)
        {
        }

        public override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    #endregion Test Helper Classes
}