namespace Roadbed.Test.Unit.Scheduling;

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Scheduling;

/// <summary>
/// Contains unit tests for verifying the behavior of the SchedulingSchedule class.
/// </summary>
[TestClass]
public class SchedulingScheduleTests
{
    #region Public Methods

    #region Constructor Tests - Cron Expression

    /// <summary>
    /// Unit test to verify that constructor with cron expression initializes properties correctly.
    /// </summary>
    [TestMethod]
    public void Constructor_WithCronExpression_InitializesPropertiesCorrectly()
    {
        // Arrange (Given)
        string cronExpression = "0 30 2 * * ?";

        // Act (When)
        var schedule = new SchedulingSchedule(cronExpression);

        // Assert (Then)
        Assert.AreEqual(
            cronExpression,
            schedule.CronExpression,
            "CronExpression should be set to the provided value.");
        Assert.AreEqual(
            SchedulingScheduleType.Cron,
            schedule.ScheduleType,
            "ScheduleType should be Cron.");
        Assert.IsNull(
            schedule.Interval,
            "Interval should be null for cron schedules.");
        Assert.IsNull(
            schedule.StartAt,
            "StartAt should be null for cron schedules.");
        Assert.IsNull(
            schedule.StartDelay,
            "StartDelay should be null for cron schedules.");
    }

    /// <summary>
    /// Unit test to verify that constructor throws ArgumentException when cron expression is null.
    /// </summary>
    [TestMethod]
    public void Constructor_WithNullCronExpression_ThrowsArgumentException()
    {
        // Arrange (Given)
        string? nullCronExpression = null;
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            var schedule = new SchedulingSchedule(nullCronExpression!);
        }
        catch (ArgumentException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "Constructor should throw ArgumentException when cron expression is null.");
    }

    /// <summary>
    /// Unit test to verify that constructor throws ArgumentException when cron expression is empty.
    /// </summary>
    [TestMethod]
    public void Constructor_WithEmptyCronExpression_ThrowsArgumentException()
    {
        // Arrange (Given)
        string emptyCronExpression = string.Empty;
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            var schedule = new SchedulingSchedule(emptyCronExpression);
        }
        catch (ArgumentException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "Constructor should throw ArgumentException when cron expression is empty.");
    }

    /// <summary>
    /// Unit test to verify that constructor throws ArgumentException when cron expression is whitespace.
    /// </summary>
    [TestMethod]
    public void Constructor_WithWhitespaceCronExpression_ThrowsArgumentException()
    {
        // Arrange (Given)
        string whitespaceCronExpression = "   ";
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            var schedule = new SchedulingSchedule(whitespaceCronExpression);
        }
        catch (ArgumentException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "Constructor should throw ArgumentException when cron expression is whitespace.");
    }

    #endregion Constructor Tests - Cron Expression

    #region Constructor Tests - Simple Interval

    /// <summary>
    /// Unit test to verify that constructor with interval initializes properties correctly.
    /// </summary>
    [TestMethod]
    public void Constructor_WithInterval_InitializesPropertiesCorrectly()
    {
        // Arrange (Given)
        TimeSpan interval = TimeSpan.FromMinutes(15);

        // Act (When)
        var schedule = new SchedulingSchedule(interval);

        // Assert (Then)
        Assert.AreEqual(
            interval,
            schedule.Interval,
            "Interval should be set to the provided value.");
        Assert.AreEqual(
            TimeSpan.Zero,
            schedule.StartDelay,
            "StartDelay should default to zero.");
        Assert.AreEqual(
            SchedulingScheduleType.SimpleInterval,
            schedule.ScheduleType,
            "ScheduleType should be SimpleInterval.");
        Assert.IsNull(
            schedule.CronExpression,
            "CronExpression should be null for interval schedules.");
        Assert.IsNull(
            schedule.StartAt,
            "StartAt should be null for interval schedules.");
    }

    /// <summary>
    /// Unit test to verify that constructor with interval and start delay initializes properties correctly.
    /// </summary>
    [TestMethod]
    public void Constructor_WithIntervalAndStartDelay_InitializesPropertiesCorrectly()
    {
        // Arrange (Given)
        TimeSpan interval = TimeSpan.FromHours(1);
        TimeSpan startDelay = TimeSpan.FromMinutes(10);

        // Act (When)
        var schedule = new SchedulingSchedule(interval, startDelay);

        // Assert (Then)
        Assert.AreEqual(
            interval,
            schedule.Interval,
            "Interval should be set to the provided value.");
        Assert.AreEqual(
            startDelay,
            schedule.StartDelay,
            "StartDelay should be set to the provided value.");
        Assert.AreEqual(
            SchedulingScheduleType.SimpleInterval,
            schedule.ScheduleType,
            "ScheduleType should be SimpleInterval.");
    }

    /// <summary>
    /// Unit test to verify that constructor throws ArgumentOutOfRangeException when interval is zero.
    /// </summary>
    [TestMethod]
    public void Constructor_WithZeroInterval_ThrowsArgumentOutOfRangeException()
    {
        // Arrange (Given)
        TimeSpan zeroInterval = TimeSpan.Zero;
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            var schedule = new SchedulingSchedule(zeroInterval);
        }
        catch (ArgumentOutOfRangeException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "Constructor should throw ArgumentOutOfRangeException when interval is zero.");
    }

    /// <summary>
    /// Unit test to verify that constructor throws ArgumentOutOfRangeException when interval is negative.
    /// </summary>
    [TestMethod]
    public void Constructor_WithNegativeInterval_ThrowsArgumentOutOfRangeException()
    {
        // Arrange (Given)
        TimeSpan negativeInterval = TimeSpan.FromMinutes(-5);
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            var schedule = new SchedulingSchedule(negativeInterval);
        }
        catch (ArgumentOutOfRangeException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "Constructor should throw ArgumentOutOfRangeException when interval is negative.");
    }

    /// <summary>
    /// Unit test to verify that constructor throws ArgumentOutOfRangeException when start delay is negative.
    /// </summary>
    [TestMethod]
    public void Constructor_WithNegativeStartDelay_ThrowsArgumentOutOfRangeException()
    {
        // Arrange (Given)
        TimeSpan interval = TimeSpan.FromMinutes(15);
        TimeSpan negativeStartDelay = TimeSpan.FromSeconds(-30);
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            var schedule = new SchedulingSchedule(interval, negativeStartDelay);
        }
        catch (ArgumentOutOfRangeException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "Constructor should throw ArgumentOutOfRangeException when start delay is negative.");
    }

    #endregion Constructor Tests - Simple Interval

    #region Constructor Tests - Specific Time

    /// <summary>
    /// Unit test to verify that constructor with specific time initializes properties correctly for one-time execution.
    /// </summary>
    [TestMethod]
    public void Constructor_WithSpecificTimeOnce_InitializesPropertiesCorrectly()
    {
        // Arrange (Given)
        DateTime startAt = new DateTime(2026, 1, 20, 14, 30, 0);

        // Act (When)
        var schedule = new SchedulingSchedule(startAt);

        // Assert (Then)
        Assert.AreEqual(
            startAt,
            schedule.StartAt,
            "StartAt should be set to the provided value.");
        Assert.IsNull(
            schedule.Interval,
            "Interval should be null for one-time schedules.");
        Assert.AreEqual(
            SchedulingScheduleType.SpecificTimeOnce,
            schedule.ScheduleType,
            "ScheduleType should be SpecificTimeOnce.");
        Assert.IsNull(
            schedule.CronExpression,
            "CronExpression should be null for specific time schedules.");
        Assert.IsNull(
            schedule.StartDelay,
            "StartDelay should be null for specific time schedules.");
    }

    /// <summary>
    /// Unit test to verify that constructor with specific time and interval initializes properties correctly.
    /// </summary>
    [TestMethod]
    public void Constructor_WithSpecificTimeAndInterval_InitializesPropertiesCorrectly()
    {
        // Arrange (Given)
        DateTime startAt = new DateTime(2026, 1, 20, 14, 30, 0);
        TimeSpan interval = TimeSpan.FromMinutes(30);

        // Act (When)
        var schedule = new SchedulingSchedule(startAt, interval);

        // Assert (Then)
        Assert.AreEqual(
            startAt,
            schedule.StartAt,
            "StartAt should be set to the provided value.");
        Assert.AreEqual(
            interval,
            schedule.Interval,
            "Interval should be set to the provided value.");
        Assert.AreEqual(
            SchedulingScheduleType.SpecificTimeWithInterval,
            schedule.ScheduleType,
            "ScheduleType should be SpecificTimeWithInterval.");
    }

    /// <summary>
    /// Unit test to verify that constructor throws ArgumentOutOfRangeException when interval is zero for specific time.
    /// </summary>
    [TestMethod]
    public void Constructor_WithSpecificTimeAndZeroInterval_ThrowsArgumentOutOfRangeException()
    {
        // Arrange (Given)
        DateTime startAt = new DateTime(2026, 1, 20, 14, 30, 0);
        TimeSpan zeroInterval = TimeSpan.Zero;
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            var schedule = new SchedulingSchedule(startAt, zeroInterval);
        }
        catch (ArgumentOutOfRangeException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "Constructor should throw ArgumentOutOfRangeException when interval is zero.");
    }

    /// <summary>
    /// Unit test to verify that constructor throws ArgumentOutOfRangeException when interval is negative for specific time.
    /// </summary>
    [TestMethod]
    public void Constructor_WithSpecificTimeAndNegativeInterval_ThrowsArgumentOutOfRangeException()
    {
        // Arrange (Given)
        DateTime startAt = new DateTime(2026, 1, 20, 14, 30, 0);
        TimeSpan negativeInterval = TimeSpan.FromMinutes(-15);
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            var schedule = new SchedulingSchedule(startAt, negativeInterval);
        }
        catch (ArgumentOutOfRangeException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "Constructor should throw ArgumentOutOfRangeException when interval is negative.");
    }

    #endregion Constructor Tests - Specific Time

    #region Property Tests - Default Values

    /// <summary>
    /// Unit test to verify that Priority property defaults to Normal.
    /// </summary>
    [TestMethod]
    public void Priority_DefaultValue_IsNormal()
    {
        // Arrange (Given)
        var schedule = new SchedulingSchedule("0 0 8 * * ?");

        // Act (When)
        var priority = schedule.Priority;

        // Assert (Then)
        Assert.AreEqual(
            SchedulingJobPriority.Normal,
            priority,
            "Priority should default to Normal.");
    }

    /// <summary>
    /// Unit test to verify that TimeZone property defaults to UTC.
    /// </summary>
    [TestMethod]
    public void TimeZone_DefaultValue_IsUtc()
    {
        // Arrange (Given)
        var schedule = new SchedulingSchedule("0 0 8 * * ?");

        // Act (When)
        var timeZone = schedule.TimeZone;

        // Assert (Then)
        Assert.AreEqual(
            TimeZoneInfo.Utc,
            timeZone,
            "TimeZone should default to UTC.");
    }

    /// <summary>
    /// Unit test to verify that GroupName property defaults to Default.
    /// </summary>
    [TestMethod]
    public void GroupName_DefaultValue_IsDefault()
    {
        // Arrange (Given)
        var schedule = new SchedulingSchedule("0 0 8 * * ?");

        // Act (When)
        var groupName = schedule.GroupName;

        // Assert (Then)
        Assert.AreEqual(
            "Default",
            groupName,
            "GroupName should default to 'Default'.");
    }

    /// <summary>
    /// Unit test to verify that MisfireHandlingEnabled property defaults to true.
    /// </summary>
    [TestMethod]
    public void MisfireHandlingEnabled_DefaultValue_IsTrue()
    {
        // Arrange (Given)
        var schedule = new SchedulingSchedule("0 0 8 * * ?");

        // Act (When)
        var misfireHandlingEnabled = schedule.MisfireHandlingEnabled;

        // Assert (Then)
        Assert.IsTrue(
            misfireHandlingEnabled,
            "MisfireHandlingEnabled should default to true.");
    }

    /// <summary>
    /// Unit test to verify that MisfireStrategy property defaults to Default.
    /// </summary>
    [TestMethod]
    public void MisfireStrategy_DefaultValue_IsDefault()
    {
        // Arrange (Given)
        var schedule = new SchedulingSchedule("0 0 8 * * ?");

        // Act (When)
        var misfireStrategy = schedule.MisfireStrategy;

        // Assert (Then)
        Assert.AreEqual(
            SchedulingMisfireStrategy.Default,
            misfireStrategy,
            "MisfireStrategy should default to Default.");
    }

    /// <summary>
    /// Unit test to verify that MaxExecutionCount property defaults to null.
    /// </summary>
    [TestMethod]
    public void MaxExecutionCount_DefaultValue_IsNull()
    {
        // Arrange (Given)
        var schedule = new SchedulingSchedule("0 0 8 * * ?");

        // Act (When)
        var maxExecutionCount = schedule.MaxExecutionCount;

        // Assert (Then)
        Assert.IsNull(
            maxExecutionCount,
            "MaxExecutionCount should default to null.");
    }

    #endregion Property Tests - Default Values

    #region Property Tests - Settable Properties

    /// <summary>
    /// Unit test to verify that Priority property can be set.
    /// </summary>
    [TestMethod]
    public void Priority_SetValue_ReturnsSetValue()
    {
        // Arrange (Given)
        var schedule = new SchedulingSchedule("0 0 8 * * ?");
        var expectedPriority = SchedulingJobPriority.High;

        // Act (When)
        schedule.Priority = expectedPriority;

        // Assert (Then)
        Assert.AreEqual(
            expectedPriority,
            schedule.Priority,
            "Priority should return the value that was set.");
    }

    /// <summary>
    /// Unit test to verify that TimeZone property can be set.
    /// </summary>
    [TestMethod]
    public void TimeZone_SetValue_ReturnsSetValue()
    {
        // Arrange (Given)
        var schedule = new SchedulingSchedule("0 0 8 * * ?");
        var expectedTimeZone = TimeZoneInfo.Local;

        // Act (When)
        schedule.TimeZone = expectedTimeZone;

        // Assert (Then)
        Assert.AreEqual(
            expectedTimeZone,
            schedule.TimeZone,
            "TimeZone should return the value that was set.");
    }

    /// <summary>
    /// Unit test to verify that GroupName property can be initialized.
    /// </summary>
    [TestMethod]
    public void GroupName_InitValue_ReturnsInitValue()
    {
        // Arrange (Given)
        string expectedGroupName = "System";

        // Act (When)
        var schedule = new SchedulingSchedule("0 0 8 * * ?")
        {
            GroupName = expectedGroupName,
        };

        // Assert (Then)
        Assert.AreEqual(
            expectedGroupName,
            schedule.GroupName,
            "GroupName should return the value that was initialized.");
    }

    /// <summary>
    /// Unit test to verify that MisfireHandlingEnabled property can be set.
    /// </summary>
    [TestMethod]
    public void MisfireHandlingEnabled_SetValue_ReturnsSetValue()
    {
        // Arrange (Given)
        var schedule = new SchedulingSchedule("0 0 8 * * ?");

        // Act (When)
        schedule.MisfireHandlingEnabled = false;

        // Assert (Then)
        Assert.IsFalse(
            schedule.MisfireHandlingEnabled,
            "MisfireHandlingEnabled should return the value that was set.");
    }

    /// <summary>
    /// Unit test to verify that MisfireStrategy property can be set.
    /// </summary>
    [TestMethod]
    public void MisfireStrategy_SetValue_ReturnsSetValue()
    {
        // Arrange (Given)
        var schedule = new SchedulingSchedule("0 0 8 * * ?");
        var expectedStrategy = SchedulingMisfireStrategy.IgnoreMisfires;

        // Act (When)
        schedule.MisfireStrategy = expectedStrategy;

        // Assert (Then)
        Assert.AreEqual(
            expectedStrategy,
            schedule.MisfireStrategy,
            "MisfireStrategy should return the value that was set.");
    }

    /// <summary>
    /// Unit test to verify that MaxExecutionCount property can be set.
    /// </summary>
    [TestMethod]
    public void MaxExecutionCount_SetValue_ReturnsSetValue()
    {
        // Arrange (Given)
        var schedule = new SchedulingSchedule("0 0 8 * * ?");
        int expectedCount = 100;

        // Act (When)
        schedule.MaxExecutionCount = expectedCount;

        // Assert (Then)
        Assert.AreEqual(
            expectedCount,
            schedule.MaxExecutionCount,
            "MaxExecutionCount should return the value that was set.");
    }

    #endregion Property Tests - Settable Properties

    #region Integration Tests - Object Initializer

    /// <summary>
    /// Unit test to verify that object initializer syntax works with all properties.
    /// </summary>
    [TestMethod]
    public void ObjectInitializer_WithAllProperties_InitializesCorrectly()
    {
        // Arrange (Given)
        string cronExpression = "0 0 8 * * ?";
        var expectedTimeZone = TimeZoneInfo.Local;
        string expectedGroupName = "System";
        var expectedPriority = SchedulingJobPriority.Highest;
        int expectedMaxCount = 50;
        var expectedStrategy = SchedulingMisfireStrategy.FireAndProceed;

        // Act (When)
        var schedule = new SchedulingSchedule(cronExpression)
        {
            TimeZone = expectedTimeZone,
            GroupName = expectedGroupName,
            Priority = expectedPriority,
            MaxExecutionCount = expectedMaxCount,
            MisfireHandlingEnabled = false,
            MisfireStrategy = expectedStrategy,
        };

        // Assert (Then)
        Assert.AreEqual(
            cronExpression,
            schedule.CronExpression,
            "CronExpression should be set correctly.");
        Assert.AreEqual(
            expectedTimeZone,
            schedule.TimeZone,
            "TimeZone should be set correctly.");
        Assert.AreEqual(
            expectedGroupName,
            schedule.GroupName,
            "GroupName should be set correctly.");
        Assert.AreEqual(
            expectedPriority,
            schedule.Priority,
            "Priority should be set correctly.");
        Assert.AreEqual(
            expectedMaxCount,
            schedule.MaxExecutionCount,
            "MaxExecutionCount should be set correctly.");
        Assert.IsFalse(
            schedule.MisfireHandlingEnabled,
            "MisfireHandlingEnabled should be set correctly.");
        Assert.AreEqual(
            expectedStrategy,
            schedule.MisfireStrategy,
            "MisfireStrategy should be set correctly.");
    }

    #endregion Integration Tests - Object Initializer

    #endregion Public Methods
}