namespace Roadbed.Test.Unit.Security.Pin;

using System;
using System.Globalization;
using Microsoft.Extensions.Time.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Security.Pin;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="PinService.Validate(int)"/>
/// and <see cref="PinService.Validate(string)"/> methods.
/// </summary>
[TestClass]
public sealed class PinServiceValidateTests
{
    #region Private Fields

    /// <summary>
    /// The length of one window under the default options, in seconds.
    /// </summary>
    private const int WindowInSeconds = 600;

    /// <summary>
    /// An instant whose PIN under the default options is below 100000, so its
    /// six-digit form starts with a zero (found once and fixed here).
    /// </summary>
    private static readonly DateTimeOffset LeadingZeroInstant =
        new DateTimeOffset(2026, 11, 10, 1, 14, 30, TimeSpan.Zero);

    private static readonly DateTimeOffset StartInstant =
        new DateTimeOffset(2026, 11, 10, 14, 23, 17, TimeSpan.Zero);

    #endregion Private Fields

    #region Public Methods

    /// <summary>
    /// Unit test to verify that a PIN is accepted inside the window it was made in.
    /// </summary>
    [TestMethod]
    public void Validate_PinFromCurrentWindow_ReturnsTrue()
    {
        // Arrange (Given)
        FakeTimeProvider timeProvider = new FakeTimeProvider(StartInstant);
        PinService service = new PinService(new SecurityPinOptions(), timeProvider);
        int pin = service.Generate();

        // Act (When)
        bool isValid = service.Validate(pin);

        // Assert (Then)
        Assert.IsTrue(
            isValid,
            "A PIN should be accepted in the window it was generated in.");
    }

    /// <summary>
    /// Unit test to verify that a PIN made in the previous window is accepted by default.
    /// </summary>
    [TestMethod]
    public void Validate_PinFromPreviousWindow_ReturnsTrue()
    {
        // Arrange (Given)
        int pin = GenerateAt(new SecurityPinOptions(), StartInstant);
        PinService consumer = CreateServiceAt(new SecurityPinOptions(), StartInstant.AddSeconds(WindowInSeconds));

        // Act (When)
        bool isValid = consumer.Validate(pin);

        // Assert (Then)
        Assert.IsTrue(
            isValid,
            "A PIN from the previous window should be accepted when adjacent windows are allowed.");
    }

    /// <summary>
    /// Unit test to verify that a PIN made in the next window, by a producer whose
    /// clock runs ahead, is accepted by default.
    /// </summary>
    [TestMethod]
    public void Validate_PinFromNextWindow_ReturnsTrue()
    {
        // Arrange (Given)
        int pin = GenerateAt(new SecurityPinOptions(), StartInstant.AddSeconds(WindowInSeconds));
        PinService consumer = CreateServiceAt(new SecurityPinOptions(), StartInstant);

        // Act (When)
        bool isValid = consumer.Validate(pin);

        // Assert (Then)
        Assert.IsTrue(
            isValid,
            "A PIN from the next window should be accepted when adjacent windows are allowed.");
    }

    /// <summary>
    /// Unit test to verify that a PIN two windows old is rejected.
    /// </summary>
    [TestMethod]
    public void Validate_PinTwoWindowsOld_ReturnsFalse()
    {
        // Arrange (Given)
        int pin = GenerateAt(new SecurityPinOptions(), StartInstant);
        PinService consumer = CreateServiceAt(new SecurityPinOptions(), StartInstant.AddSeconds(2 * WindowInSeconds));

        // Act (When)
        bool isValid = consumer.Validate(pin);

        // Assert (Then)
        Assert.IsFalse(
            isValid,
            "A PIN two windows old should be rejected.");
    }

    /// <summary>
    /// Unit test to verify that a PIN from the previous window is rejected when
    /// adjacent windows are turned off.
    /// </summary>
    [TestMethod]
    public void Validate_AdjacentWindowsOffAndPreviousWindow_ReturnsFalse()
    {
        // Arrange (Given)
        SecurityPinOptions options = new SecurityPinOptions { AcceptAdjacentWindows = false };
        int pin = GenerateAt(options, StartInstant);
        PinService consumer = CreateServiceAt(options, StartInstant.AddSeconds(WindowInSeconds));

        // Act (When)
        bool isValid = consumer.Validate(pin);

        // Assert (Then)
        Assert.IsFalse(
            isValid,
            "A PIN from the previous window should be rejected when adjacent windows are off.");
    }

    /// <summary>
    /// Unit test to verify that a PIN from the next window is rejected when
    /// adjacent windows are turned off.
    /// </summary>
    [TestMethod]
    public void Validate_AdjacentWindowsOffAndNextWindow_ReturnsFalse()
    {
        // Arrange (Given)
        SecurityPinOptions options = new SecurityPinOptions { AcceptAdjacentWindows = false };
        int pin = GenerateAt(options, StartInstant.AddSeconds(WindowInSeconds));
        PinService consumer = CreateServiceAt(options, StartInstant);

        // Act (When)
        bool isValid = consumer.Validate(pin);

        // Assert (Then)
        Assert.IsFalse(
            isValid,
            "A PIN from the next window should be rejected when adjacent windows are off.");
    }

    /// <summary>
    /// Unit test to verify that a PIN made seconds before midnight on the last
    /// day of the year is accepted seconds after it, when every placeholder
    /// (year, month, day and hour) has changed.
    /// </summary>
    [TestMethod]
    public void Validate_PinAcrossYearEndMidnight_ReturnsTrue()
    {
        // Arrange (Given)
        int pin = GenerateAt(new SecurityPinOptions(), new DateTimeOffset(2026, 12, 31, 23, 59, 59, TimeSpan.Zero));
        PinService consumer = CreateServiceAt(
            new SecurityPinOptions(),
            new DateTimeOffset(2027, 1, 1, 0, 0, 5, TimeSpan.Zero));

        // Act (When)
        bool isValid = consumer.Validate(pin);

        // Assert (Then)
        Assert.IsTrue(
            isValid,
            "A PIN should survive midnight at year end because placeholders come from the window start.");
    }

    /// <summary>
    /// Unit test to verify that a PIN that does not match any accepted window is rejected.
    /// </summary>
    [TestMethod]
    public void Validate_WrongPin_ReturnsFalse()
    {
        // Arrange (Given)
        FakeTimeProvider timeProvider = new FakeTimeProvider(StartInstant);
        PinService service = new PinService(new SecurityPinOptions(), timeProvider);
        int wrongPin = (service.Generate() + 1) % 1_000_000;

        // Act (When)
        bool isValid = service.Validate(wrongPin);

        // Assert (Then)
        Assert.IsFalse(
            isValid,
            "A PIN that differs from the generated one should be rejected.");
    }

    /// <summary>
    /// Unit test to verify that a number outside the range a PIN can take is rejected.
    /// </summary>
    /// <param name="pin">A number no six-digit PIN can equal.</param>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(1_000_000)]
    [DataRow(int.MaxValue)]
    public void Validate_NumberOutsidePinRange_ReturnsFalse(int pin)
    {
        // Arrange (Given)
        PinService service = CreateServiceAt(new SecurityPinOptions(), StartInstant);

        // Act (When)
        bool isValid = service.Validate(pin);

        // Assert (Then)
        Assert.IsFalse(
            isValid,
            "A number outside 0 to 10^PinLength - 1 should be rejected.");
    }

    /// <summary>
    /// Unit test to verify that the generated PIN is accepted as text.
    /// </summary>
    [TestMethod]
    public void Validate_GeneratedPinAsText_ReturnsTrue()
    {
        // Arrange (Given)
        PinService service = CreateServiceAt(new SecurityPinOptions(), StartInstant);
        string pin = service.Generate().ToString(CultureInfo.InvariantCulture);

        // Act (When)
        bool isValid = service.Validate(pin);

        // Assert (Then)
        Assert.IsTrue(
            isValid,
            "The generated PIN written as text should be accepted.");
    }

    /// <summary>
    /// Unit test to verify that a PIN below 100000 is accepted both padded to six
    /// digits with a leading zero and written without it.
    /// </summary>
    [TestMethod]
    public void Validate_LeadingZeroPinPaddedAndUnpadded_ReturnsTrueForBoth()
    {
        // Arrange (Given)
        PinService service = CreateServiceAt(new SecurityPinOptions(), LeadingZeroInstant);
        int pin = service.Generate();
        Assert.IsLessThan(
            100_000,
            pin,
            "The fixed instant should produce a PIN with a leading zero; the defaults may have changed.");
        string padded = pin.ToString("D6", CultureInfo.InvariantCulture);
        string unpadded = pin.ToString(CultureInfo.InvariantCulture);

        // Act (When)
        bool paddedIsValid = service.Validate(padded);
        bool unpaddedIsValid = service.Validate(unpadded);

        // Assert (Then)
        Assert.IsTrue(
            paddedIsValid,
            "A PIN padded with a leading zero should be accepted.");
        Assert.IsTrue(
            unpaddedIsValid,
            "A PIN written without its leading zero should be accepted.");
    }

    /// <summary>
    /// Unit test to verify that text that is not a plain run of at most six ASCII
    /// digits is rejected without throwing.
    /// </summary>
    /// <param name="pin">The malformed PIN text.</param>
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(" 12345")]
    [DataRow("12345 ")]
    [DataRow("+12345")]
    [DataRow("-12345")]
    [DataRow("12a456")]
    [DataRow("1234567")]
    [DataRow("1,234")]
    public void Validate_MalformedText_ReturnsFalse(string? pin)
    {
        // Arrange (Given)
        PinService service = CreateServiceAt(new SecurityPinOptions(), StartInstant);

        // Act (When)
        bool isValid = service.Validate(pin);

        // Assert (Then)
        Assert.IsFalse(
            isValid,
            "Malformed PIN text should be rejected rather than throw.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates a service whose clock stands at the given instant.
    /// </summary>
    /// <param name="options">The options the service uses.</param>
    /// <param name="instant">The UTC instant the clock reads.</param>
    /// <returns>The service.</returns>
    private static PinService CreateServiceAt(SecurityPinOptions options, DateTimeOffset instant)
    {
        return new PinService(options, new FakeTimeProvider(instant));
    }

    /// <summary>
    /// Generates the PIN a producer would send at the given instant.
    /// </summary>
    /// <param name="options">The options the producer uses.</param>
    /// <param name="instant">The UTC instant the producer's clock reads.</param>
    /// <returns>The PIN.</returns>
    private static int GenerateAt(SecurityPinOptions options, DateTimeOffset instant)
    {
        return CreateServiceAt(options, instant).Generate();
    }

    #endregion Private Methods
}
