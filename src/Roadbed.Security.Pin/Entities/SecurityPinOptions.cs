/*
 * The namespace Roadbed.Security.Pin.Entities was removed on purpose and replaced with Roadbed.Security.Pin so that no additional using statements are required.
 */
namespace Roadbed.Security.Pin;

/// <summary>
/// Options POCO that tells <see cref="PinService"/> how to calculate and
/// validate a PIN. The producer and the consumer of a PIN must hold the same
/// values.
/// </summary>
/// <remarks>
/// <para>
/// This type is populated by the hosting application at start-up and
/// registered as a singleton in DI. Roadbed.Security.Pin never reads
/// configuration directly: the host decides where each value comes from
/// (appsettings, environment variables, a KeePass entry, or a mix of them).
/// </para>
/// <para>
/// <b>Every default below is published in a public repository.</b> A host
/// that keeps the defaults produces PINs that anyone with this library can
/// calculate. Override <see cref="SecretKeyWithPatternSupport"/> and the four
/// offsets in every host.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In the host application's PinInstaller:
/// services.AddSingleton(new SecurityPinOptions
/// {
///     SecretKeyWithPatternSupport = templateFromKeePass,
///     OffsetYear = long.Parse(configuration["Pin:OffsetYear"]!, CultureInfo.InvariantCulture),
///     OffsetMonth = long.Parse(configuration["Pin:OffsetMonth"]!, CultureInfo.InvariantCulture),
///     OffsetDay = long.Parse(configuration["Pin:OffsetDay"]!, CultureInfo.InvariantCulture),
///     OffsetHour = long.Parse(configuration["Pin:OffsetHour"]!, CultureInfo.InvariantCulture),
/// });
/// </code>
/// </example>
public sealed class SecurityPinOptions
{
    #region Public Properties

    /// <summary>
    /// Gets the shared secret, optionally carrying date placeholders.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Four placeholders are recognized, each replaced with a number calculated
    /// from the UTC start of the window being computed:
    /// <c>{YO}</c> is the year plus <see cref="OffsetYear"/>,
    /// <c>{MO}</c> is the month plus <see cref="OffsetMonth"/>,
    /// <c>{DO}</c> is the day of the month plus <see cref="OffsetDay"/>, and
    /// <c>{HO}</c> is the hour plus <see cref="OffsetHour"/>.
    /// Any other brace in the value is rejected when <see cref="PinService"/>
    /// is constructed. A value with no placeholders is used as it is.
    /// </para>
    /// <para>
    /// Defaults to <c>"IA{YO}SD{MO}WY{DO}CO{HO}KS"</c>, which is public.
    /// </para>
    /// </remarks>
    public string SecretKeyWithPatternSupport { get; init; } = "IA{YO}SD{MO}WY{DO}CO{HO}KS";

    /// <summary>
    /// Gets the number added to the UTC year to produce <c>{YO}</c>.
    /// </summary>
    /// <remarks>
    /// May be negative. The default is a random value published in a public
    /// repository; override it.
    /// </remarks>
    public long OffsetYear { get; init; } = 9085031473001827058;

    /// <summary>
    /// Gets the number added to the UTC month (1 to 12) to produce <c>{MO}</c>.
    /// </summary>
    /// <remarks>
    /// May be negative. The default is a random value published in a public
    /// repository; override it.
    /// </remarks>
    public long OffsetMonth { get; init; } = -6709113040557976202;

    /// <summary>
    /// Gets the number added to the UTC day of the month (1 to 31) to produce <c>{DO}</c>.
    /// </summary>
    /// <remarks>
    /// May be negative. The default is a random value published in a public
    /// repository; override it.
    /// </remarks>
    public long OffsetDay { get; init; } = 3167112204274793981;

    /// <summary>
    /// Gets the number added to the UTC hour (0 to 23) to produce <c>{HO}</c>.
    /// </summary>
    /// <remarks>
    /// May be negative. The default is a random value published in a public
    /// repository; override it.
    /// </remarks>
    public long OffsetHour { get; init; } = -7134370028420314626;

    /// <summary>
    /// Gets the length of one window, in seconds: how often the PIN changes.
    /// </summary>
    /// <remarks>
    /// Windows are counted from the Unix epoch in UTC, so every server agrees
    /// on them whatever its own time zone. Must be at least 1. Defaults to
    /// 600 (10 minutes).
    /// </remarks>
    public int WindowInSeconds { get; init; } = 600;

    /// <summary>
    /// Gets the number of digits in a PIN.
    /// </summary>
    /// <remarks>
    /// Must be between 4 and 9. Defaults to 6. Fewer digits are easier to
    /// guess: a consumer should limit failed attempts whatever the length.
    /// </remarks>
    public int PinLength { get; init; } = 6;

    /// <summary>
    /// Gets a value indicating whether a PIN from the window before or after
    /// the current one is also accepted.
    /// </summary>
    /// <remarks>
    /// Covers the time a request takes to arrive and small differences
    /// between server clocks. With the default window of 600 seconds, a PIN
    /// is then accepted for 10 to 20 minutes after it is made. Defaults to
    /// <see langword="true"/>.
    /// </remarks>
    public bool AcceptAdjacentWindows { get; init; } = true;

    #endregion Public Properties
}
