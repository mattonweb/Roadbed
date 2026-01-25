namespace Roadbed.Test.Unit.Scheduling;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Scheduling;
using System;

/// <summary>
/// Contains unit tests for verifying the behavior of the JobExecutionInfo record.
/// </summary>
[TestClass]
public class JobExecutionInfoTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that constructor initializes all required properties correctly.
    /// </summary>
    [TestMethod]
    public void Constructor_AllRequiredProperties_SetsCorrectly()
    {
        // Arrange (Given)
        string expectedFireInstanceId = "test-instance-123";
        string expectedJobGroup = "TestGroup";
        string expectedJobName = "TestJob";
        string expectedTriggerGroup = "TriggerGroup";
        string expectedTriggerName = "TriggerName";

        // Act (When)
        var info = new JobExecutionInfo
        {
            FireInstanceId = expectedFireInstanceId,
            JobGroup = expectedJobGroup,
            JobName = expectedJobName,
            TriggerGroup = expectedTriggerGroup,
            TriggerName = expectedTriggerName,
        };

        // Assert (Then)
        Assert.AreEqual(
            expectedFireInstanceId,
            info.FireInstanceId,
            "FireInstanceId should be set to the provided value.");
        Assert.AreEqual(
            expectedJobGroup,
            info.JobGroup,
            "JobGroup should be set to the provided value.");
        Assert.AreEqual(
            expectedJobName,
            info.JobName,
            "JobName should be set to the provided value.");
        Assert.AreEqual(
            expectedTriggerGroup,
            info.TriggerGroup,
            "TriggerGroup should be set to the provided value.");
        Assert.AreEqual(
            expectedTriggerName,
            info.TriggerName,
            "TriggerName should be set to the provided value.");
    }

    /// <summary>
    /// Unit test to verify that optional timing properties can be null.
    /// </summary>
    [TestMethod]
    public void Constructor_OptionalTimingProperties_CanBeNull()
    {
        // Arrange (Given)
        // Act (When)
        var info = new JobExecutionInfo
        {
            FireInstanceId = "test-instance",
            JobGroup = "TestGroup",
            JobName = "TestJob",
            TriggerGroup = "TriggerGroup",
            TriggerName = "TriggerName",
            FireTimeUtc = null,
            ScheduledFireTimeUtc = null,
            PreviousFireTimeUtc = null,
            NextFireTimeUtc = null,
        };

        // Assert (Then)
        Assert.IsNull(
            info.FireTimeUtc,
            "FireTimeUtc should be null when not provided.");
        Assert.IsNull(
            info.ScheduledFireTimeUtc,
            "ScheduledFireTimeUtc should be null when not provided.");
        Assert.IsNull(
            info.PreviousFireTimeUtc,
            "PreviousFireTimeUtc should be null when not provided.");
        Assert.IsNull(
            info.NextFireTimeUtc,
            "NextFireTimeUtc should be null when not provided.");
    }

    /// <summary>
    /// Unit test to verify that optional timing properties can be set to specific values.
    /// </summary>
    [TestMethod]
    public void Constructor_OptionalTimingProperties_CanBeSet()
    {
        // Arrange (Given)
        var expectedFireTime = new DateTimeOffset(2026, 1, 24, 10, 30, 0, TimeSpan.Zero);
        var expectedScheduledTime = new DateTimeOffset(2026, 1, 24, 10, 30, 0, TimeSpan.Zero);
        var expectedPreviousTime = new DateTimeOffset(2026, 1, 24, 9, 30, 0, TimeSpan.Zero);
        var expectedNextTime = new DateTimeOffset(2026, 1, 24, 11, 30, 0, TimeSpan.Zero);

        // Act (When)
        var info = new JobExecutionInfo
        {
            FireInstanceId = "test-instance",
            JobGroup = "TestGroup",
            JobName = "TestJob",
            TriggerGroup = "TriggerGroup",
            TriggerName = "TriggerName",
            FireTimeUtc = expectedFireTime,
            ScheduledFireTimeUtc = expectedScheduledTime,
            PreviousFireTimeUtc = expectedPreviousTime,
            NextFireTimeUtc = expectedNextTime,
        };

        // Assert (Then)
        Assert.AreEqual(
            expectedFireTime,
            info.FireTimeUtc,
            "FireTimeUtc should be set to the provided value.");
        Assert.AreEqual(
            expectedScheduledTime,
            info.ScheduledFireTimeUtc,
            "ScheduledFireTimeUtc should be set to the provided value.");
        Assert.AreEqual(
            expectedPreviousTime,
            info.PreviousFireTimeUtc,
            "PreviousFireTimeUtc should be set to the provided value.");
        Assert.AreEqual(
            expectedNextTime,
            info.NextFireTimeUtc,
            "NextFireTimeUtc should be set to the provided value.");
    }

    /// <summary>
    /// Unit test to verify that ResultMessage property can be null.
    /// </summary>
    [TestMethod]
    public void Constructor_ResultMessage_CanBeNull()
    {
        // Arrange (Given)
        // Act (When)
        var info = new JobExecutionInfo
        {
            FireInstanceId = "test-instance",
            JobGroup = "TestGroup",
            JobName = "TestJob",
            TriggerGroup = "TriggerGroup",
            TriggerName = "TriggerName",
            ResultMessage = null,
        };

        // Assert (Then)
        Assert.IsNull(
            info.ResultMessage,
            "ResultMessage should be null when not provided.");
    }

    /// <summary>
    /// Unit test to verify that ResultMessage property can be set to a value.
    /// </summary>
    [TestMethod]
    public void Constructor_ResultMessage_CanBeSet()
    {
        // Arrange (Given)
        string expectedResultMessage = "Processed 1,234 records successfully";

        // Act (When)
        var info = new JobExecutionInfo
        {
            FireInstanceId = "test-instance",
            JobGroup = "TestGroup",
            JobName = "TestJob",
            TriggerGroup = "TriggerGroup",
            TriggerName = "TriggerName",
            ResultMessage = expectedResultMessage,
        };

        // Assert (Then)
        Assert.AreEqual(
            expectedResultMessage,
            info.ResultMessage,
            "ResultMessage should be set to the provided value.");
    }

    /// <summary>
    /// Unit test to verify that record equality works correctly with same values.
    /// </summary>
    [TestMethod]
    public void Equality_SameValues_AreEqual()
    {
        // Arrange (Given)
        var info1 = new JobExecutionInfo
        {
            FireInstanceId = "test-instance",
            JobGroup = "TestGroup",
            JobName = "TestJob",
            TriggerGroup = "TriggerGroup",
            TriggerName = "TriggerName",
            ResultMessage = "Test result",
        };

        var info2 = new JobExecutionInfo
        {
            FireInstanceId = "test-instance",
            JobGroup = "TestGroup",
            JobName = "TestJob",
            TriggerGroup = "TriggerGroup",
            TriggerName = "TriggerName",
            ResultMessage = "Test result",
        };

        // Act (When)
        bool areEqual = info1 == info2;
        bool areEqualByEquals = info1.Equals(info2);

        // Assert (Then)
        Assert.IsTrue(
            areEqual,
            "Records with same values should be equal using == operator.");
        Assert.IsTrue(
            areEqualByEquals,
            "Records with same values should be equal using Equals method.");
    }

    /// <summary>
    /// Unit test to verify that record equality works correctly with different values.
    /// </summary>
    [TestMethod]
    public void Equality_DifferentValues_AreNotEqual()
    {
        // Arrange (Given)
        var info1 = new JobExecutionInfo
        {
            FireInstanceId = "test-instance-1",
            JobGroup = "TestGroup",
            JobName = "TestJob",
            TriggerGroup = "TriggerGroup",
            TriggerName = "TriggerName",
        };

        var info2 = new JobExecutionInfo
        {
            FireInstanceId = "test-instance-2",
            JobGroup = "TestGroup",
            JobName = "TestJob",
            TriggerGroup = "TriggerGroup",
            TriggerName = "TriggerName",
        };

        // Act (When)
        bool areEqual = info1 == info2;
        bool areEqualByEquals = info1.Equals(info2);

        // Assert (Then)
        Assert.IsFalse(
            areEqual,
            "Records with different FireInstanceId should not be equal using == operator.");
        Assert.IsFalse(
            areEqualByEquals,
            "Records with different FireInstanceId should not be equal using Equals method.");
    }

    /// <summary>
    /// Unit test to verify that creating new instance with modified property works correctly.
    /// </summary>
    [TestMethod]
    public void CreateNewInstance_ModifyProperty_CreatesNewRecord()
    {
        // Arrange (Given)
        var original = new JobExecutionInfo
        {
            FireInstanceId = "original-instance",
            JobGroup = "TestGroup",
            JobName = "TestJob",
            TriggerGroup = "TriggerGroup",
            TriggerName = "TriggerName",
            ResultMessage = "Original result",
        };

        string newResultMessage = "Updated result";

        // Act (When)
        var modified = new JobExecutionInfo
        {
            FireInstanceId = original.FireInstanceId,
            JobGroup = original.JobGroup,
            JobName = original.JobName,
            TriggerGroup = original.TriggerGroup,
            TriggerName = original.TriggerName,
            ResultMessage = newResultMessage,
        };

        // Assert (Then)
        Assert.AreEqual(
            "original-instance",
            modified.FireInstanceId,
            "FireInstanceId should remain unchanged in new record.");
        Assert.AreEqual(
            newResultMessage,
            modified.ResultMessage,
            "ResultMessage should be updated in new record.");
        Assert.AreEqual(
            "Original result",
            original.ResultMessage,
            "Original record should remain unchanged.");
        Assert.AreNotSame(
            original,
            modified,
            "New instance should be a different object.");
    }

    #endregion Public Methods
}