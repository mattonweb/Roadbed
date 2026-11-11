# Roadbed.Security.Pin Reference

A rotating numeric PIN that two applications sharing one configuration use to prove it to each other on a request, with no token service. The producer calls `PinService.Generate()` and sends the PIN; the consumer calls `PinService.Validate(...)` with what it received. Nothing is stored, issued or refreshed: both sides calculate the same PIN from the same `SecurityPinOptions` and a clock kept in step by NTP.

Typical use case: one Pebble & Silver service calling another's endpoint, or a scheduled job calling a webhook, where a static API key would work forever once leaked and a PIN stops working within minutes.

## Type catalog (3 types)

| Type                 | Kind         | Purpose                                                                                                   |
| -------------------- | ------------ | --------------------------------------------------------------------------------------------------------- |
| `SecurityPinOptions` | Sealed POCO  | The shared values: secret template, four offsets, window length, PIN length, adjacent windows. `init`-only. |
| `PinService`         | Sealed class | `int Generate()`, `bool Validate(int)`, `bool Validate(string?)`. Stateless; register as a singleton.       |
| `InstallSecurityPin` | Installer    | Registers `PinService` from the host's `SecurityPinOptions` (or the defaults) and `TimeProvider.System`.    |

Namespaces: `Roadbed.Security.Pin` (options and service) and `Roadbed.Security.Pin.Installers`.

## How the PIN is calculated

You need this to reason about compatibility, never to reimplement it.

1. **Window.** `floor(unixSeconds / WindowInSeconds)`, counted in UTC, so servers in different time zones agree.
2. **Key.** `SecretKeyWithPatternSupport` as UTF-8, after each placeholder is replaced with a number taken from the UTC **start of the window**: `{YO}` = year + `OffsetYear`, `{MO}` = month + `OffsetMonth`, `{DO}` = day of month + `OffsetDay`, `{HO}` = hour + `OffsetHour`. Taking the parts from the window start, not the call time, is why a PIN made at 23:59:59 still validates at 00:00:05.
3. **PIN.** HMAC-SHA256 of the window number (8 bytes, big-endian) under that key, then the RFC 4226 truncation to `PinLength` digits. With no placeholders, this is exactly TOTP (RFC 6238) with HMAC-SHA256.
4. **Validation.** The consumer recomputes for its current window and, when `AcceptAdjacentWindows` is true, the windows before and after. With the defaults (600 seconds, adjacent on) a PIN is accepted for 10 to 20 minutes after it is made.

## MUST

- **MUST** give the producer and the consumer **identical** `SecurityPinOptions`: the secret, all four offsets, `WindowInSeconds` and `PinLength`. Any difference makes every PIN fail, with nothing in the result saying why.
- **MUST** override `SecretKeyWithPatternSupport` and all four offsets in every host. Every default is published in the public Roadbed repository; a host that keeps them produces PINs anyone with this library can calculate.
- **MUST** create the secret and the offsets **once, at set-up, with a cryptographic random generator** (pattern below), then give the same values to both ends.
- **MUST** keep the secret (or at least its random part) in a secret store such as a KeePass entry read through `KeePassReader`, never in source control or a committed `appsettings.json`. Where each value lives is the host's choice; splitting them (secret in KeePass, offsets in appsettings) means a leak of one store alone is not enough to calculate PINs.
- **MUST** set the options in the host's own installer (`PinInstaller : IServiceCollectionInstaller`), registered as a singleton, and let `InstallSecurityPin` build `PinService`. Inject `PinService` through the constructor; the installer does not snapshot `ServiceLocator`.
- **MUST** fail fast at start-up when a value is missing (pattern below). A missing configuration key otherwise leaves that property at its public default, silently.
- **MUST** limit failed validations per caller on the consumer. A 6-digit PIN has a million values and, with adjacent windows on, three are valid at any moment; at `PinLength = 4` (10,000 values) throttling is essential.
- **MUST** send the PIN over HTTPS, in the `Authorization` header as `Bearer <pin>` wherever the caller can set headers. Use `?pin=` only when it cannot: query strings end up in access logs, proxy logs and `Referer` headers.
- **MUST** keep the servers' clocks in step with NTP. Adjacent windows cover small drift, not minutes.

## MUST NOT

- **MUST NOT** ship with the default secret or offsets.
- **MUST NOT** calculate a PIN yourself or read `DateTime.Now`; call `PinService`, which reads the injected `TimeProvider`.
- **MUST NOT** log `SecurityPinOptions`, the secret, or the filled secret. Do not log PINs above `Debug` either.
- **MUST NOT** put a brace in the secret other than the four placeholders: `{DD}`, `{yo}`, a lone `{` or `}` make the `PinService` constructor throw `ArgumentException`.
- **MUST NOT** pass the whole header to `Validate`. `Validate("Bearer 123456")` is `false`; strip the scheme first.
- **MUST NOT** turn `AcceptAdjacentWindows` off between machines unless a failure at a window boundary is acceptable: a PIN made in the last second of a window is then rejected one second later.
- **MUST NOT** expect a retry to rescue a rejected PIN. `NetHttpClient` retries 408, 503 and 504 only, and resends the same `Authentication.Value`. Build a new request with a fresh `Generate()` if you retry a 401 yourself.
- **MUST NOT** bind `SecurityPinOptions` with the configuration-binding source generator (on under Native AOT or `<EnableConfigurationBindingGenerator>`). It skips `init`-only properties and leaves every public default in place, with no error. The default reflection binder (`configuration.GetSection("Pin").Get<SecurityPinOptions>()`) binds them, but still leaves a missing key at its default.

## Code patterns

### Creating the values once, at set-up

Run this once (a scratch console app or a test) and store the output: the secret in a KeePass entry, the offsets in each host's configuration. Both ends get the same values.

```csharp
using System;
using System.Security.Cryptography;

string randomPart = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));
string secret = randomPart.Substring(0, 16) + "{YO}" + randomPart.Substring(16, 8) + "{DO}" + randomPart.Substring(24) + "{HO}{MO}";
Console.WriteLine($"Secret:      {secret}");
Console.WriteLine($"OffsetYear:  {BitConverter.ToInt64(RandomNumberGenerator.GetBytes(8))}");
Console.WriteLine($"OffsetMonth: {BitConverter.ToInt64(RandomNumberGenerator.GetBytes(8))}");
Console.WriteLine($"OffsetDay:   {BitConverter.ToInt64(RandomNumberGenerator.GetBytes(8))}");
Console.WriteLine($"OffsetHour:  {BitConverter.ToInt64(RandomNumberGenerator.GetBytes(8))}");
```

Base64 never produces a brace, so the random part cannot collide with a placeholder. A secret with no placeholders (for example the 32 random characters alone) is also valid: it is then plain TOTP and the offsets have no effect.

### Setting `SecurityPinOptions` at start-up (the host's `PinInstaller`)

The secret comes from KeePass, the rest from configuration, and every value is required:

```json
{
  "Pin": {
    "OffsetYear": "<OffsetYear from the set-up step>",
    "OffsetMonth": "<OffsetMonth from the set-up step>",
    "OffsetDay": "<OffsetDay from the set-up step>",
    "OffsetHour": "<OffsetHour from the set-up step>",
    "WindowInSeconds": "600",
    "PinLength": "6"
  }
}
```

```csharp
namespace Foo.Installers;

using System;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Roadbed;
using Roadbed.Secrets.KeePass;
using Roadbed.Security.Pin;

/// <summary>
/// Populates the PIN options Foo shares with Bar and registers them.
/// </summary>
public sealed class PinInstaller : IServiceCollectionInstaller
{
    #region Public Methods

    /// <inheritdoc/>
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        IConfigurationSection section = configuration.GetSection("Pin");

        // A factory, so the KeePass reader is resolved when PinService is first
        // built, not while installers are still running.
        services.AddSingleton(provider => new SecurityPinOptions
        {
            SecretKeyWithPatternSupport = provider.GetRequiredService<KeePassReader>().Read("Foo to Bar PIN").Password,
            OffsetYear = ReadRequiredLong(section, "OffsetYear"),
            OffsetMonth = ReadRequiredLong(section, "OffsetMonth"),
            OffsetDay = ReadRequiredLong(section, "OffsetDay"),
            OffsetHour = ReadRequiredLong(section, "OffsetHour"),
            WindowInSeconds = (int)ReadRequiredLong(section, "WindowInSeconds"),
            PinLength = (int)ReadRequiredLong(section, "PinLength"),
        });
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Reads a required whole number, failing at start-up when it is missing.
    /// </summary>
    /// <param name="section">The configuration section.</param>
    /// <param name="key">The key inside the section.</param>
    /// <returns>The value.</returns>
    private static long ReadRequiredLong(IConfigurationSection section, string key)
    {
        string value = section[key]
            ?? throw new InvalidOperationException($"Pin:{key} is not set.");
        return long.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
    }

    #endregion Private Methods
}
```

- `SecurityPinOptions` is `init`-only: build it in one object initializer; it cannot be changed afterwards.
- Offsets are `long` and may be negative. Store them as strings in JSON; a 19-digit number can lose precision in tools that read JSON numbers as doubles.
- `InstallSecurityPin` reads the options when `PinService` is first resolved, so it does not matter which installer runs first. A bad value (blank secret, stray brace, window under one second, length outside 4 to 9) throws from the `PinService` constructor on that first resolve. Resolve `PinService` once during start-up if you want the failure before the first request.

### Several purposes in one application

One `PinService` serves one set of options. `PinService` is sealed, so a second purpose is a keyed singleton with its own options (unlike `KeePassReader`, there is no subclass pattern):

```csharp
services.AddKeyedSingleton("bar", (provider, _) => new PinService(
    barOptions,
    provider.GetRequiredService<TimeProvider>()));
services.AddKeyedSingleton("baz", (provider, _) => new PinService(
    bazOptions,
    provider.GetRequiredService<TimeProvider>()));
```

```csharp
public sealed class BarClient
{
    private readonly PinService _pinService;

    public BarClient([FromKeyedServices("bar")] PinService pinService)
    {
        ArgumentNullException.ThrowIfNull(pinService);

        this._pinService = pinService;
    }
}
```

### Producer: sending the PIN

```csharp
NetHttpRequest request = new NetHttpRequest
{
    HttpEndPoint = new Uri("https://bar.example.com/api/orders"),
    Method = HttpMethod.Get,
    Authentication = new NetHttpAuthentication
    {
        AuthenticationType = NetHttpAuthenticationType.Bearer,
        Value = this._pinService.Generate().ToString(CultureInfo.InvariantCulture),
    },
};

NetHttpResponse<OrderList> response = await this._httpClient.MakeHttpRequestAsync<OrderList>(request, cancellationToken);
```

Generate the PIN when the request is built, not once at start-up: it changes every window.

### Consumer: validating the PIN

```csharp
// authorizationHeader is the request's Authorization header value, or null.
string? presented = null;
if ((authorizationHeader is not null) && authorizationHeader.StartsWith("Bearer ", StringComparison.Ordinal))
{
    presented = authorizationHeader.Substring("Bearer ".Length);
}

if (!this._pinService.Validate(presented))
{
    // Count the failure for this caller before answering.
    return Results.Unauthorized();
}
```

`Validate(string?)` returns `false` for null, empty, signed, spaced, non-digit or over-long text, and accepts the PIN with or without leading zeros (`"049593"` and `"49593"`). It never throws for bad input.

### Testing with a fake clock

```csharp
FakeTimeProvider producerClock = new FakeTimeProvider(new DateTimeOffset(2026, 12, 31, 23, 59, 59, TimeSpan.Zero));
FakeTimeProvider consumerClock = new FakeTimeProvider(new DateTimeOffset(2027, 1, 1, 0, 0, 5, TimeSpan.Zero));
SecurityPinOptions options = new SecurityPinOptions { SecretKeyWithPatternSupport = "test-secret-{DO}" };

int pin = new PinService(options, producerClock).Generate();
bool isValid = new PinService(options, consumerClock).Validate(pin);   // true
```

Register a `FakeTimeProvider` before `InstallSecurityPin` runs and the installed `PinService` uses it.

## Common pitfalls

### Keeping the defaults

```csharp
// ❌ Every value is public: anyone with Roadbed.Security.Pin can produce valid PINs.
services.AddSingleton(new SecurityPinOptions());

// ✅ Every value comes from the host's stores (see the PinInstaller pattern).
services.AddSingleton(provider => new SecurityPinOptions { /* secret from KeePass, offsets from configuration */ });
```

### Binding a section with a key missing

```csharp
// ❌ "OffsetHour" is missing from appsettings: binding leaves the public default,
//    and nothing fails. Under Native AOT the source-generated binder skips every
//    init-only property, so ALL values stay at the public defaults.
SecurityPinOptions options = configuration.GetSection("Pin").Get<SecurityPinOptions>()!;

// ✅ Read each value explicitly and fail at start-up when one is missing.
OffsetHour = ReadRequiredLong(section, "OffsetHour"),
```

### Passing the whole header

```csharp
// ❌ Always false: "Bearer 123456" is not six digits.
bool isValid = this._pinService.Validate(request.Headers.Authorization);

// ✅ Strip the scheme, then validate the PIN text.
bool isValid = this._pinService.Validate(presented);
```

### Generating once and reusing the PIN

```csharp
// ❌ Stops working 10 to 20 minutes after start-up.
private readonly string _pin = pinService.Generate().ToString(CultureInfo.InvariantCulture);

// ✅ Generate per request.
Value = this._pinService.Generate().ToString(CultureInfo.InvariantCulture),
```

## Quick reference

### Using statements

```csharp
using Roadbed.Security.Pin;               // SecurityPinOptions, PinService
using Roadbed.Security.Pin.Installers;    // InstallSecurityPin (only to run it explicitly)
```

### Public surface

| Member                                                         | Purpose                                                                                     |
| -------------------------------------------------------------- | ------------------------------------------------------------------------------------------- |
| `SecurityPinOptions.SecretKeyWithPatternSupport` (`init`)      | The secret; may carry `{YO}` `{MO}` `{DO}` `{HO}`. Default `"IA{YO}SD{MO}WY{DO}CO{HO}KS"` (public). |
| `SecurityPinOptions.OffsetYear / OffsetMonth / OffsetDay / OffsetHour` (`init`, `long`) | Added to the UTC part of the window start. Defaults are random but public. |
| `SecurityPinOptions.WindowInSeconds` (`init`, `int`)           | How often the PIN changes. Default 600. Minimum 1.                                          |
| `SecurityPinOptions.PinLength` (`init`, `int`)                 | Digits in a PIN. Default 6. Allowed 4 to 9.                                                 |
| `SecurityPinOptions.AcceptAdjacentWindows` (`init`, `bool`)    | Also accept the window before and after. Default `true`.                                    |
| `PinService(SecurityPinOptions, TimeProvider)`                 | Checks the options; throws on bad ones.                                                     |
| `PinService.Generate() → int`                                  | The PIN for the current window, `0` to `10^PinLength - 1`.                                  |
| `PinService.Validate(int) → bool`                              | `true` when the PIN matches an accepted window.                                             |
| `PinService.Validate(string?) → bool`                          | Same, for text; `false` for any malformed text.                                             |

### Construction-time exceptions

| Failure mode                                                        | Exception type                |
| ------------------------------------------------------------------- | ----------------------------- |
| `options` or `timeProvider` is `null`                               | `ArgumentNullException`       |
| `SecretKeyWithPatternSupport` is empty or whitespace                | `ArgumentException`           |
| A brace in the secret that is not `{YO}`, `{MO}`, `{DO}` or `{HO}`  | `ArgumentException`           |
| `WindowInSeconds` below 1                                           | `ArgumentOutOfRangeException` |
| `PinLength` below 4 or above 9                                      | `ArgumentOutOfRangeException` |

### Set-up recipe — at a glance

```
1. Create values once        random secret (+ placeholders) and four random long offsets
2. Store them                secret in KeePass, offsets/window/length in each host's configuration
3. Host PinInstaller         services.AddSingleton(provider => new SecurityPinOptions { ... });  fail fast on missing values
4. Installer                 InstallSecurityPin registers PinService (auto-discovered)
5. Producer                  Authorization: Bearer {pinService.Generate()}
6. Consumer                  pinService.Validate(presentedText); throttle failures per caller
```
