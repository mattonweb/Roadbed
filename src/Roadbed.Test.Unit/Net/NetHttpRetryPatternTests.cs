namespace Roadbed.Test.Unit.Net;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Net;

/// <summary>
/// Contains unit tests for verifying the behavior of the
/// <see cref="NetHttpRetryPattern"/> class.
/// </summary>
[TestClass]
public class NetHttpRetryPatternTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that constructor initializes MaxAttempts with default value.
    /// </summary>
    [TestMethod]
    public void Constructor_NoParameters_InitializesMaxAttemptsWithDefault()
    {
        // Arrange (Given)

        // Act (When)
        var instance = new NetHttpRetryPattern();

        // Assert (Then)
        Assert.AreEqual(
            0,
            instance.MaxAttempts,
            "MaxAttempts should default to 0 when no value is provided.");
    }

    /// <summary>
    /// Unit test to verify that constructor initializes DelayMultiplierInSeconds with default value.
    /// </summary>
    [TestMethod]
    public void Constructor_NoParameters_InitializesDelayMultiplierInSecondsWithDefault()
    {
        // Arrange (Given)

        // Act (When)
        var instance = new NetHttpRetryPattern();

        // Assert (Then)
        Assert.AreEqual(
            0,
            instance.DelayMultiplierInSeconds,
            "DelayMultiplierInSeconds should default to 0 when no value is provided.");
    }

    /// <summary>
    /// Unit test to verify that MaxAttempts property can be set and retrieved.
    /// </summary>
    [TestMethod]
    public void MaxAttempts_SetValidValue_ReturnsSetValue()
    {
        // Arrange (Given)
        var instance = new NetHttpRetryPattern();
        int expectedValue = 5;

        // Act (When)
        instance.MaxAttempts = expectedValue;

        // Assert (Then)
        Assert.AreEqual(
            expectedValue,
            instance.MaxAttempts,
            "MaxAttempts should return the value that was set.");
    }

    /// <summary>
    /// Unit test to verify that DelayMultiplierInSeconds property can be set and retrieved.
    /// </summary>
    [TestMethod]
    public void DelayMultiplierInSeconds_SetValidValue_ReturnsSetValue()
    {
        // Arrange (Given)
        var instance = new NetHttpRetryPattern();
        int expectedValue = 10;

        // Act (When)
        instance.DelayMultiplierInSeconds = expectedValue;

        // Assert (Then)
        Assert.AreEqual(
            expectedValue,
            instance.DelayMultiplierInSeconds,
            "DelayMultiplierInSeconds should return the value that was set.");
    }

    #endregion Public Methods
}