namespace Roadbed.Test.Unit.Scheduling;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Scheduling;
using System;

/// <summary>
/// Contains unit tests for verifying the behavior of the NullSchedulingMetrics class.
/// </summary>
[TestClass]
public class NullSchedulingMetricsTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that Instance property returns a singleton instance.
    /// </summary>
    [TestMethod]
    public void Instance_Singleton_ReturnsSameInstance()
    {
        // Arrange (Given)
        // Act (When)
        var instance1 = NullSchedulingMetrics.Instance;
        var instance2 = NullSchedulingMetrics.Instance;

        // Assert (Then)
        Assert.IsNotNull(
            instance1,
            "Instance should not be null.");
        Assert.AreSame(
            instance1,
            instance2,
            "Instance property should return the same singleton instance.");
    }

    /// <summary>
    /// Unit test to verify that JobStarted method executes without throwing.
    /// </summary>
    [TestMethod]
    public void JobStarted_ValidInfo_DoesNotThrow()
    {
        // Arrange (Given)
        var metrics = NullSchedulingMetrics.Instance;
        var info = new JobExecutionInfo
        {
            FireInstanceId = "test-instance",
            JobGroup = "TestGroup",
            JobName = "TestJob",
            TriggerGroup = "TriggerGroup",
            TriggerName = "TriggerName",
        };

        bool exceptionThrown = false;

        // Act (When)
        try
        {
            metrics.JobStarted(info);
        }
        catch (Exception)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsFalse(
            exceptionThrown,
            "JobStarted should not throw any exceptions.");
    }

    /// <summary>
    /// Unit test to verify that JobCompleted method executes without throwing.
    /// </summary>
    [TestMethod]
    public void JobCompleted_ValidInfo_DoesNotThrow()
    {
        // Arrange (Given)
        var metrics = NullSchedulingMetrics.Instance;
        var info = new JobExecutionInfo
        {
            FireInstanceId = "test-instance",
            JobGroup = "TestGroup",
            JobName = "TestJob",
            TriggerGroup = "TriggerGroup",
            TriggerName = "TriggerName",
            ResultMessage = "Test result",
        };
        var duration = TimeSpan.FromSeconds(5);

        bool exceptionThrown = false;

        // Act (When)
        try
        {
            metrics.JobCompleted(info, duration);
        }
        catch (Exception)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsFalse(
            exceptionThrown,
            "JobCompleted should not throw any exceptions.");
    }

    /// <summary>
    /// Unit test to verify that JobFailed method executes without throwing.
    /// </summary>
    [TestMethod]
    public void JobFailed_ValidInfo_DoesNotThrow()
    {
        // Arrange (Given)
        var metrics = NullSchedulingMetrics.Instance;
        var info = new JobExecutionInfo
        {
            FireInstanceId = "test-instance",
            JobGroup = "TestGroup",
            JobName = "TestJob",
            TriggerGroup = "TriggerGroup",
            TriggerName = "TriggerName",
            ResultMessage = "Partial result",
        };
        var exception = new InvalidOperationException("Test exception");
        var duration = TimeSpan.FromSeconds(3);

        bool exceptionThrown = false;

        // Act (When)
        try
        {
            metrics.JobFailed(info, exception, duration);
        }
        catch (Exception)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsFalse(
            exceptionThrown,
            "JobFailed should not throw any exceptions.");
    }

    /// <summary>
    /// Unit test to verify that JobMisfired method executes without throwing.
    /// </summary>
    [TestMethod]
    public void JobMisfired_ValidInfo_DoesNotThrow()
    {
        // Arrange (Given)
        var metrics = NullSchedulingMetrics.Instance;
        var info = new JobExecutionInfo
        {
            FireInstanceId = "test-instance",
            JobGroup = "TestGroup",
            JobName = "TestJob",
            TriggerGroup = "TriggerGroup",
            TriggerName = "TriggerName",
        };

        bool exceptionThrown = false;

        // Act (When)
        try
        {
            metrics.JobMisfired(info);
        }
        catch (Exception)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsFalse(
            exceptionThrown,
            "JobMisfired should not throw any exceptions.");
    }

    /// <summary>
    /// Unit test to verify that all methods have no side effects.
    /// </summary>
    [TestMethod]
    public void AllMethods_MultipleInvocations_NoSideEffects()
    {
        // Arrange (Given)
        var metrics = NullSchedulingMetrics.Instance;
        var info = new JobExecutionInfo
        {
            FireInstanceId = "test-instance",
            JobGroup = "TestGroup",
            JobName = "TestJob",
            TriggerGroup = "TriggerGroup",
            TriggerName = "TriggerName",
            ResultMessage = "Test result",
        };
        var exception = new InvalidOperationException("Test exception");
        var duration = TimeSpan.FromSeconds(1);

        bool exceptionThrown = false;

        // Act (When)
        try
        {
            // Call each method multiple times
            metrics.JobStarted(info);
            metrics.JobStarted(info);
            metrics.JobCompleted(info, duration);
            metrics.JobCompleted(info, duration);
            metrics.JobFailed(info, exception, duration);
            metrics.JobFailed(info, exception, duration);
            metrics.JobMisfired(info);
            metrics.JobMisfired(info);
        }
        catch (Exception)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsFalse(
            exceptionThrown,
            "Multiple invocations of no-op methods should not throw exceptions or have side effects.");
    }

    #endregion Public Methods
}