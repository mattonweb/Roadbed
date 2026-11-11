namespace Roadbed.Test.Unit.Security.Pin;

using System;
using System.Globalization;
using Microsoft.Extensions.Time.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Security.Pin;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="PinService.Generate"/> method.
/// </summary>
[TestClass]
public sealed class PinServiceGenerateTests
{
    #region Private Fields

    /// <summary>
    /// The 32-byte ASCII key RFC 6238 Appendix B uses for its HMAC-SHA256 test vectors.
    /// </summary>
    private const string Rfc6238Sha256Key = "12345678901234567890123456789012";

    private static readonly DateTimeOffset StartInstant =
        new DateTimeOffset(2026, 11, 10, 14, 23, 17, TimeSpan.Zero);

    #endregion Private Fields

    #region Public Methods

    /// <summary>
    /// Unit test to verify that, with a secret that has no placeholders, the
    /// calculation reproduces the published RFC 6238 HMAC-SHA256 test vectors.
    /// </summary>
    /// <param name="unixSeconds">The test vector's time, in seconds since the Unix epoch.</param>
    /// <param name="expectedPin">The test vector's published 8-digit TOTP.</param>
    [TestMethod]
    [DataRow(59L, 46119246)]
    [DataRow(1111111109L, 68084774)]
    [DataRow(1111111111L, 67062674)]
    [DataRow(1234567890L, 91819424)]
    [DataRow(2000000000L, 90698825)]
    [DataRow(20000000000L, 77737706)]
    public void Generate_Rfc6238Sha256Vector_ReturnsPublishedPin(long unixSeconds, int expectedPin)
    {
        // Arrange (Given)
        SecurityPinOptions options = new SecurityPinOptions
        {
            SecretKeyWithPatternSupport = Rfc6238Sha256Key,
            WindowInSeconds = 30,
            PinLength = 8,
        };
        FakeTimeProvider timeProvider = new FakeTimeProvider(DateTimeOffset.FromUnixTimeSeconds(unixSeconds));
        PinService service = new PinService(options, timeProvider);

        // Act (When)
        int pin = service.Generate();

        // Assert (Then)
        Assert.AreEqual(
            expectedPin,
            pin,
            "Generate should reproduce the RFC 6238 Appendix B HMAC-SHA256 test vector.");
    }

    /// <summary>
    /// Unit test to verify that two calls inside the same window return the same PIN.
    /// </summary>
    [TestMethod]
    public void Generate_TwoCallsInSameWindow_ReturnSamePin()
    {
        // Arrange (Given)
        FakeTimeProvider timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 11, 10, 14, 20, 1, TimeSpan.Zero));
        PinService service = new PinService(new SecurityPinOptions(), timeProvider);
        int first = service.Generate();
        timeProvider.Advance(TimeSpan.FromSeconds(598));

        // Act (When)
        int second = service.Generate();

        // Assert (Then)
        Assert.AreEqual(
            first,
            second,
            "Generate should return the same PIN for every call inside one window.");
    }

    /// <summary>
    /// Unit test to verify that the offsets reach the key through the placeholders:
    /// changing only the day offset changes the PIN.
    /// </summary>
    [TestMethod]
    public void Generate_DifferentOffsetDay_ReturnsDifferentPin()
    {
        // Arrange (Given)
        FakeTimeProvider timeProvider = new FakeTimeProvider(StartInstant);
        PinService first = new PinService(new SecurityPinOptions { OffsetDay = 1000 }, timeProvider);
        PinService second = new PinService(new SecurityPinOptions { OffsetDay = 1001 }, timeProvider);

        // Act (When)
        int firstPin = first.Generate();
        int secondPin = second.Generate();

        // Assert (Then)
        Assert.AreNotEqual(
            firstPin,
            secondPin,
            "A different OffsetDay should produce a different PIN at the same instant.");
    }

    /// <summary>
    /// Unit test to verify that a secret with no placeholders ignores the offsets.
    /// </summary>
    [TestMethod]
    public void Generate_SecretWithoutPlaceholders_IgnoresOffsets()
    {
        // Arrange (Given)
        FakeTimeProvider timeProvider = new FakeTimeProvider(StartInstant);
        PinService first = new PinService(
            new SecurityPinOptions { SecretKeyWithPatternSupport = "JBSWY3DPEHPK3PXP", OffsetDay = 1000 },
            timeProvider);
        PinService second = new PinService(
            new SecurityPinOptions { SecretKeyWithPatternSupport = "JBSWY3DPEHPK3PXP", OffsetDay = -42 },
            timeProvider);

        // Act (When)
        int firstPin = first.Generate();
        int secondPin = second.Generate();

        // Assert (Then)
        Assert.AreEqual(
            firstPin,
            secondPin,
            "Offsets should have no effect when the secret has no placeholders.");
    }

    /// <summary>
    /// Unit test to verify that negative offsets are formatted with the invariant
    /// culture, so a server whose culture writes another minus sign builds the same key.
    /// </summary>
    [TestMethod]
    public void Generate_CultureWithOtherMinusSign_ReturnsInvariantPin()
    {
        // Arrange (Given)
        SecurityPinOptions options = new SecurityPinOptions
        {
            OffsetYear = -9_000_000_000_000_000_000,
            OffsetMonth = -1,
            OffsetDay = -2,
            OffsetHour = -3,
        };
        FakeTimeProvider timeProvider = new FakeTimeProvider(StartInstant);
        PinService service = new PinService(options, timeProvider);
        CultureInfo otherMinusSign = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        otherMinusSign.NumberFormat.NegativeSign = "−";
        CultureInfo original = CultureInfo.CurrentCulture;
        int invariantPin;
        int otherCulturePin;

        // Act (When)
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            invariantPin = service.Generate();
            CultureInfo.CurrentCulture = otherMinusSign;
            otherCulturePin = service.Generate();
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }

        // Assert (Then)
        Assert.AreEqual(
            invariantPin,
            otherCulturePin,
            "The PIN should not depend on the current culture's minus sign.");
    }

    #endregion Public Methods
}
