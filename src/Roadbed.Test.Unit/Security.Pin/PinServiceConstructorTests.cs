namespace Roadbed.Test.Unit.Security.Pin;

using System;
using Microsoft.Extensions.Time.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Security.Pin;

/// <summary>
/// Contains unit tests for verifying how the <see cref="PinService"/> constructor
/// checks its arguments.
/// </summary>
[TestClass]
public sealed class PinServiceConstructorTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that null options are rejected.
    /// </summary>
    [TestMethod]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange (Given)
        SecurityPinOptions? options = null;
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            _ = new PinService(options!, new FakeTimeProvider());
        }
        catch (ArgumentNullException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "The constructor should throw ArgumentNullException when options is null.");
    }

    /// <summary>
    /// Unit test to verify that a null clock is rejected.
    /// </summary>
    [TestMethod]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        // Arrange (Given)
        TimeProvider? timeProvider = null;
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            _ = new PinService(new SecurityPinOptions(), timeProvider!);
        }
        catch (ArgumentNullException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "The constructor should throw ArgumentNullException when timeProvider is null.");
    }

    /// <summary>
    /// Unit test to verify that a blank secret is rejected.
    /// </summary>
    /// <param name="secret">A blank secret.</param>
    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public void Constructor_BlankSecret_ThrowsArgumentException(string secret)
    {
        // Arrange (Given)
        SecurityPinOptions options = new SecurityPinOptions { SecretKeyWithPatternSupport = secret };
        Exception? caught = null;

        // Act (When)
        try
        {
            _ = new PinService(options, new FakeTimeProvider());
        }
        catch (ArgumentException ex)
        {
            caught = ex;
        }

        // Assert (Then)
        Assert.IsNotNull(
            caught,
            "The constructor should reject a blank secret.");
        Assert.AreEqual(
            typeof(ArgumentException),
            caught.GetType(),
            "A blank secret should be reported as ArgumentException.");
    }

    /// <summary>
    /// Unit test to verify that a brace outside the four recognized placeholders is rejected.
    /// </summary>
    /// <param name="secret">A secret with an unknown placeholder or a stray brace.</param>
    [TestMethod]
    [DataRow("AB{DD}CD")]
    [DataRow("AB{yo}CD")]
    [DataRow("AB{CD")]
    [DataRow("AB}CD")]
    [DataRow("{YO}{MO}{DO}{HO}{")]
    public void Constructor_UnrecognizedBrace_ThrowsArgumentException(string secret)
    {
        // Arrange (Given)
        SecurityPinOptions options = new SecurityPinOptions { SecretKeyWithPatternSupport = secret };
        Exception? caught = null;

        // Act (When)
        try
        {
            _ = new PinService(options, new FakeTimeProvider());
        }
        catch (ArgumentException ex)
        {
            caught = ex;
        }

        // Assert (Then)
        Assert.IsNotNull(
            caught,
            "The constructor should reject a brace that is not part of a recognized placeholder.");
        Assert.AreEqual(
            typeof(ArgumentException),
            caught.GetType(),
            "An unrecognized brace should be reported as ArgumentException.");
    }

    /// <summary>
    /// Unit test to verify that a PIN length outside 4 to 9 is rejected.
    /// </summary>
    /// <param name="pinLength">A PIN length outside the allowed range.</param>
    [TestMethod]
    [DataRow(3)]
    [DataRow(10)]
    public void Constructor_PinLengthOutsideRange_ThrowsArgumentOutOfRangeException(int pinLength)
    {
        // Arrange (Given)
        SecurityPinOptions options = new SecurityPinOptions { PinLength = pinLength };
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            _ = new PinService(options, new FakeTimeProvider());
        }
        catch (ArgumentOutOfRangeException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "The constructor should throw ArgumentOutOfRangeException for a PIN length outside 4 to 9.");
    }

    /// <summary>
    /// Unit test to verify that a window shorter than one second is rejected.
    /// </summary>
    /// <param name="windowInSeconds">A window length below one second.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(-600)]
    public void Constructor_WindowBelowOneSecond_ThrowsArgumentOutOfRangeException(int windowInSeconds)
    {
        // Arrange (Given)
        SecurityPinOptions options = new SecurityPinOptions { WindowInSeconds = windowInSeconds };
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            _ = new PinService(options, new FakeTimeProvider());
        }
        catch (ArgumentOutOfRangeException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "The constructor should throw ArgumentOutOfRangeException for a window below one second.");
    }

    /// <summary>
    /// Unit test to verify that the shortest and longest allowed PIN lengths
    /// produce a PIN within their range.
    /// </summary>
    /// <param name="pinLength">An allowed PIN length.</param>
    /// <param name="exclusiveUpperBound">10 to the power of the PIN length.</param>
    [TestMethod]
    [DataRow(4, 10_000)]
    [DataRow(9, 1_000_000_000)]
    public void Constructor_PinLengthAtBound_GeneratesPinInRange(int pinLength, int exclusiveUpperBound)
    {
        // Arrange (Given)
        SecurityPinOptions options = new SecurityPinOptions { PinLength = pinLength };
        PinService service = new PinService(options, new FakeTimeProvider());

        // Act (When)
        int pin = service.Generate();

        // Assert (Then)
        Assert.IsInRange(
            0,
            exclusiveUpperBound - 1,
            pin,
            "The PIN should lie between 0 and 10^PinLength - 1.");
    }

    #endregion Public Methods
}
