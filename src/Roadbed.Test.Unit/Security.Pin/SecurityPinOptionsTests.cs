namespace Roadbed.Test.Unit.Security.Pin;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Security.Pin;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="SecurityPinOptions"/> POCO.
/// </summary>
[TestClass]
public sealed class SecurityPinOptionsTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that the secret defaults to the published pattern.
    /// </summary>
    [TestMethod]
    public void SecretKeyWithPatternSupport_DefaultConstructor_DefaultsToPublishedPattern()
    {
        // Arrange (Given)

        // Act (When)
        SecurityPinOptions options = new SecurityPinOptions();

        // Assert (Then)
        Assert.AreEqual(
            "IA{YO}SD{MO}WY{DO}CO{HO}KS",
            options.SecretKeyWithPatternSupport,
            "SecretKeyWithPatternSupport should default to the published pattern.");
    }

    /// <summary>
    /// Unit test to verify that the four offsets default to their fixed published values,
    /// so the defaults cannot drift between the producer and the consumer.
    /// </summary>
    [TestMethod]
    public void Offsets_DefaultConstructor_DefaultToPublishedValues()
    {
        // Arrange (Given)

        // Act (When)
        SecurityPinOptions options = new SecurityPinOptions();

        // Assert (Then)
        Assert.AreEqual(
            9085031473001827058L,
            options.OffsetYear,
            "OffsetYear should default to its published value.");
        Assert.AreEqual(
            -6709113040557976202L,
            options.OffsetMonth,
            "OffsetMonth should default to its published value.");
        Assert.AreEqual(
            3167112204274793981L,
            options.OffsetDay,
            "OffsetDay should default to its published value.");
        Assert.AreEqual(
            -7134370028420314626L,
            options.OffsetHour,
            "OffsetHour should default to its published value.");
    }

    /// <summary>
    /// Unit test to verify that the window, PIN length and adjacent-window flag
    /// default to 600 seconds, six digits and on.
    /// </summary>
    [TestMethod]
    public void WindowLengthAndAdjacency_DefaultConstructor_DefaultToSixHundredSixAndTrue()
    {
        // Arrange (Given)

        // Act (When)
        SecurityPinOptions options = new SecurityPinOptions();

        // Assert (Then)
        Assert.AreEqual(
            600,
            options.WindowInSeconds,
            "WindowInSeconds should default to 600.");
        Assert.AreEqual(
            6,
            options.PinLength,
            "PinLength should default to 6.");
        Assert.IsTrue(
            options.AcceptAdjacentWindows,
            "AcceptAdjacentWindows should default to true.");
    }

    /// <summary>
    /// Unit test to verify that every property can be set through the init accessor.
    /// </summary>
    [TestMethod]
    public void InitAccessors_ValuesSupplied_RoundTrip()
    {
        // Arrange (Given)

        // Act (When)
        SecurityPinOptions options = new SecurityPinOptions
        {
            SecretKeyWithPatternSupport = "JBSWY3DPEHPK3PXP",
            OffsetYear = 1,
            OffsetMonth = -2,
            OffsetDay = 3,
            OffsetHour = -4,
            WindowInSeconds = 30,
            PinLength = 8,
            AcceptAdjacentWindows = false,
        };

        // Assert (Then)
        Assert.AreEqual(
            "JBSWY3DPEHPK3PXP",
            options.SecretKeyWithPatternSupport,
            "SecretKeyWithPatternSupport should round-trip.");
        Assert.AreEqual(
            1L,
            options.OffsetYear,
            "OffsetYear should round-trip.");
        Assert.AreEqual(
            -2L,
            options.OffsetMonth,
            "OffsetMonth should round-trip.");
        Assert.AreEqual(
            3L,
            options.OffsetDay,
            "OffsetDay should round-trip.");
        Assert.AreEqual(
            -4L,
            options.OffsetHour,
            "OffsetHour should round-trip.");
        Assert.AreEqual(
            30,
            options.WindowInSeconds,
            "WindowInSeconds should round-trip.");
        Assert.AreEqual(
            8,
            options.PinLength,
            "PinLength should round-trip.");
        Assert.IsFalse(
            options.AcceptAdjacentWindows,
            "AcceptAdjacentWindows should round-trip.");
    }

    #endregion Public Methods
}
