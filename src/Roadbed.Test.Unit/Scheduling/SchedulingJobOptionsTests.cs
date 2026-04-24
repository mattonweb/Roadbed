namespace Roadbed.Test.Unit.Scheduling;

using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Scheduling;

/// <summary>
/// Contains unit tests for verifying the behavior of the
/// <see cref="SchedulingJobOptions"/> and <see cref="SchedulingJobFeature"/> classes.
/// </summary>
[TestClass]
public class SchedulingJobOptionsTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that SchedulingJobOptions.Features defaults to an empty dictionary.
    /// </summary>
    [TestMethod]
    public void SchedulingJobOptions_DefaultConstructor_FeaturesIsEmpty()
    {
        // Arrange (Given)

        // Act (When)
        var options = new SchedulingJobOptions();

        // Assert (Then)
        Assert.IsNotNull(
            options.Features,
            "Features should not be null by default.");
        Assert.AreEqual(
            0,
            options.Features.Count,
            "Features should default to an empty dictionary.");
    }

    /// <summary>
    /// Unit test to verify that SchedulingJobOptions.Features can be populated via the init accessor.
    /// </summary>
    [TestMethod]
    public void SchedulingJobOptions_FeaturesPopulatedViaInit_ReturnsPopulatedDictionary()
    {
        // Arrange (Given)
        var feature = new SchedulingJobFeature { Enabled = false };

        // Act (When)
        var options = new SchedulingJobOptions
        {
            Features = new Dictionary<string, SchedulingJobFeature>
            {
                ["MyJob"] = feature,
            },
        };

        // Assert (Then)
        Assert.AreEqual(
            1,
            options.Features.Count,
            "Features should contain the single entry supplied at initialization.");
        Assert.AreSame(
            feature,
            options.Features["MyJob"],
            "Features should reference the same SchedulingJobFeature instance that was supplied.");
    }

    /// <summary>
    /// Unit test to verify that SchedulingJobFeature.Enabled defaults to true.
    /// </summary>
    [TestMethod]
    public void SchedulingJobFeature_DefaultConstructor_EnabledIsTrue()
    {
        // Arrange (Given)

        // Act (When)
        var feature = new SchedulingJobFeature();

        // Assert (Then)
        Assert.IsTrue(
            feature.Enabled,
            "Enabled should default to true.");
    }

    /// <summary>
    /// Unit test to verify that SchedulingJobFeature.CronExpression defaults to null.
    /// </summary>
    [TestMethod]
    public void SchedulingJobFeature_DefaultConstructor_CronExpressionIsNull()
    {
        // Arrange (Given)

        // Act (When)
        var feature = new SchedulingJobFeature();

        // Assert (Then)
        Assert.IsNull(
            feature.CronExpression,
            "CronExpression should default to null.");
    }

    /// <summary>
    /// Unit test to verify that SchedulingJobFeature.Arguments defaults to null.
    /// </summary>
    [TestMethod]
    public void SchedulingJobFeature_DefaultConstructor_ArgumentsIsNull()
    {
        // Arrange (Given)

        // Act (When)
        var feature = new SchedulingJobFeature();

        // Assert (Then)
        Assert.IsNull(
            feature.Arguments,
            "Arguments should default to null.");
    }

    /// <summary>
    /// Unit test to verify that SchedulingJobFeature properties can be set via init.
    /// </summary>
    [TestMethod]
    public void SchedulingJobFeature_AllPropertiesInitialized_ReturnsSetValues()
    {
        // Arrange (Given)
        string expectedCron = "0 */5 * * * ?";
        var expectedArguments = new Dictionary<string, string>
        {
            ["zone"] = "public",
            ["region"] = "us-east",
        };

        // Act (When)
        var feature = new SchedulingJobFeature
        {
            Enabled = false,
            CronExpression = expectedCron,
            Arguments = expectedArguments,
        };

        // Assert (Then)
        Assert.IsFalse(
            feature.Enabled,
            "Enabled should reflect the initialized value.");
        Assert.AreEqual(
            expectedCron,
            feature.CronExpression,
            "CronExpression should reflect the initialized value.");
        Assert.AreSame(
            expectedArguments,
            feature.Arguments,
            "Arguments should reference the same dictionary that was supplied.");
    }

    #endregion Public Methods
}
