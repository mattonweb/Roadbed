/*
 * The namespace Roadbed.Security.Pin.Services was removed on purpose and replaced with Roadbed.Security.Pin so that no additional using statements are required.
 */
namespace Roadbed.Security.Pin;

using System;
using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Calculates a short numeric PIN from a shared secret and the current UTC
/// time, and validates a PIN calculated the same way on another server.
/// </summary>
/// <remarks>
/// <para>
/// The calculation is TOTP (RFC 6238) with HMAC-SHA256 and the RFC 4226
/// truncation. Time is divided into windows of
/// <see cref="SecurityPinOptions.WindowInSeconds"/> counted from the Unix
/// epoch; the window number is the HMAC message. The HMAC key is
/// <see cref="SecurityPinOptions.SecretKeyWithPatternSupport"/> as UTF-8, after
/// its placeholders are filled from the UTC start of that window plus the
/// configured offsets. Because the placeholders come from the window and not
/// from the moment of the call, a producer and a consumer that compute the
/// same window always build the same key, even on opposite sides of
/// midnight.
/// </para>
/// <para>
/// The service holds no mutable state, so one instance can be shared as a
/// singleton. A second purpose in the same application is a second instance
/// with its own <see cref="SecurityPinOptions"/>.
/// </para>
/// </remarks>
public sealed class PinService
{
    #region Private Fields

    private const string YearPlaceholder = "{YO}";
    private const string MonthPlaceholder = "{MO}";
    private const string DayPlaceholder = "{DO}";
    private const string HourPlaceholder = "{HO}";
    private const int MinimumPinLength = 4;
    private const int MaximumPinLength = 9;

    private static readonly int[] PowersOfTen = new int[]
    {
        1, 10, 100, 1_000, 10_000, 100_000, 1_000_000, 10_000_000, 100_000_000, 1_000_000_000,
    };

    private readonly SecurityPinOptions _options;
    private readonly TimeProvider _timeProvider;

    #endregion Private Fields

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="PinService"/> class.
    /// </summary>
    /// <param name="options">How the PIN is calculated; must match on the producer and the consumer.</param>
    /// <param name="timeProvider">Clock the current window is read from; a test supplies a fake one.</param>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> or <paramref name="timeProvider"/> is null.</exception>
    /// <exception cref="ArgumentException">The secret is blank or contains a brace that is not one of the four placeholders.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The window is shorter than one second, or the PIN length is outside 4 to 9.</exception>
    public PinService(SecurityPinOptions options, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ValidateOptions(options);

        this._options = options;
        this._timeProvider = timeProvider;
    }

    #endregion Public Constructors

    #region Public Methods

    /// <summary>
    /// Calculates the PIN for the current window.
    /// </summary>
    /// <returns>
    /// The PIN, between 0 and 10^<see cref="SecurityPinOptions.PinLength"/> - 1.
    /// It may have fewer digits than the PIN length; pad it with leading zeros
    /// if the receiver expects a fixed width.
    /// </returns>
    public int Generate()
    {
        return this.ComputePin(this.CurrentWindow());
    }

    /// <summary>
    /// Checks a PIN against the current window and, when
    /// <see cref="SecurityPinOptions.AcceptAdjacentWindows"/> is set, the
    /// windows before and after it.
    /// </summary>
    /// <param name="pin">The PIN received from the producer.</param>
    /// <returns><see langword="true"/> when the PIN matches; otherwise <see langword="false"/>.</returns>
    public bool Validate(int pin)
    {
        if ((pin < 0) || (pin >= PowersOfTen[this._options.PinLength]))
        {
            return false;
        }

        long window = this.CurrentWindow();
        bool matchesCurrent = this.ComputePin(window) == pin;
        if (matchesCurrent || !this._options.AcceptAdjacentWindows)
        {
            return matchesCurrent;
        }

        return (this.ComputePin(window - 1) == pin) || (this.ComputePin(window + 1) == pin);
    }

    /// <summary>
    /// Checks a PIN received as text, such as an <c>Authorization</c> header
    /// value or a query-string parameter.
    /// </summary>
    /// <param name="pin">The PIN as text: ASCII digits only, at most <see cref="SecurityPinOptions.PinLength"/> of them, leading zeros allowed.</param>
    /// <returns>
    /// <see langword="true"/> when the PIN matches; otherwise
    /// <see langword="false"/>, including for null, empty, over-long or
    /// non-numeric text.
    /// </returns>
    /// <remarks>
    /// The text comes from an untrusted caller, so a malformed value is an
    /// expected outcome and returns <see langword="false"/> rather than
    /// throwing.
    /// </remarks>
    public bool Validate(string? pin)
    {
        if (string.IsNullOrEmpty(pin) || (pin.Length > this._options.PinLength))
        {
            return false;
        }

        if (!int.TryParse(pin, NumberStyles.None, CultureInfo.InvariantCulture, out int value))
        {
            return false;
        }

        return this.Validate(value);
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Formats a date part plus its offset with the invariant culture, so a
    /// server whose culture writes another minus sign builds the same key.
    /// </summary>
    /// <param name="part">The UTC year, month, day or hour.</param>
    /// <param name="offset">The configured offset for that part.</param>
    /// <returns>The sum as invariant text; <see cref="Int128"/> keeps any offset from overflowing.</returns>
    private static string FormatPart(int part, long offset)
    {
        Int128 value = (Int128)part + offset;
        return value.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Removes the four recognized placeholders from a secret template.
    /// </summary>
    /// <param name="template">The secret template.</param>
    /// <returns>The template without its placeholders.</returns>
    private static string RemovePlaceholders(string template)
    {
        string result = template.Replace(YearPlaceholder, string.Empty, StringComparison.Ordinal);
        result = result.Replace(MonthPlaceholder, string.Empty, StringComparison.Ordinal);
        result = result.Replace(DayPlaceholder, string.Empty, StringComparison.Ordinal);
        return result.Replace(HourPlaceholder, string.Empty, StringComparison.Ordinal);
    }

    /// <summary>
    /// Applies the RFC 4226 section 5.3 dynamic truncation to an HMAC.
    /// </summary>
    /// <param name="hash">The HMAC-SHA256 output.</param>
    /// <returns>A 31-bit non-negative number taken from the HMAC.</returns>
    private static int Truncate(byte[] hash)
    {
        int offset = hash[hash.Length - 1] & 0x0F;
        return BinaryPrimitives.ReadInt32BigEndian(hash.AsSpan(offset, sizeof(int))) & 0x7FFFFFFF;
    }

    /// <summary>
    /// Rejects options no PIN can be calculated from.
    /// </summary>
    /// <param name="options">The options to check.</param>
    private static void ValidateOptions(SecurityPinOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(options.SecretKeyWithPatternSupport);
        ArgumentOutOfRangeException.ThrowIfLessThan(options.WindowInSeconds, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(options.PinLength, MinimumPinLength);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(options.PinLength, MaximumPinLength);

        string remainder = RemovePlaceholders(options.SecretKeyWithPatternSupport);
        if (remainder.Contains('{', StringComparison.Ordinal) || remainder.Contains('}', StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "SecretKeyWithPatternSupport contains a brace that is not part of {YO}, {MO}, {DO} or {HO}.",
                nameof(options));
        }
    }

    /// <summary>
    /// Calculates the PIN for one window.
    /// </summary>
    /// <param name="window">The window number: whole windows since the Unix epoch.</param>
    /// <returns>The PIN for that window.</returns>
    private int ComputePin(long window)
    {
        DateTimeOffset windowStart = DateTimeOffset.FromUnixTimeSeconds(window * this._options.WindowInSeconds);
        byte[] key = Encoding.UTF8.GetBytes(this.FillTemplate(windowStart));
        byte[] counter = new byte[sizeof(long)];
        BinaryPrimitives.WriteInt64BigEndian(counter, window);

        byte[] hash = HMACSHA256.HashData(key, counter);
        return Truncate(hash) % PowersOfTen[this._options.PinLength];
    }

    /// <summary>
    /// Reads the number of the window the current UTC time falls in.
    /// </summary>
    /// <returns>Whole windows since the Unix epoch.</returns>
    private long CurrentWindow()
    {
        DateTimeOffset now = this._timeProvider.GetUtcNow();
        return now.ToUnixTimeSeconds() / this._options.WindowInSeconds;
    }

    /// <summary>
    /// Replaces the placeholders in the secret template with the parts of a
    /// window's UTC start plus their offsets.
    /// </summary>
    /// <param name="windowStart">The UTC start of the window being computed.</param>
    /// <returns>The secret for that window.</returns>
    private string FillTemplate(DateTimeOffset windowStart)
    {
        string secret = this._options.SecretKeyWithPatternSupport;
        secret = secret.Replace(YearPlaceholder, FormatPart(windowStart.Year, this._options.OffsetYear), StringComparison.Ordinal);
        secret = secret.Replace(MonthPlaceholder, FormatPart(windowStart.Month, this._options.OffsetMonth), StringComparison.Ordinal);
        secret = secret.Replace(DayPlaceholder, FormatPart(windowStart.Day, this._options.OffsetDay), StringComparison.Ordinal);
        return secret.Replace(HourPlaceholder, FormatPart(windowStart.Hour, this._options.OffsetHour), StringComparison.Ordinal);
    }

    #endregion Private Methods
}
