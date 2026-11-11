# Roadbed.Security.Pin

A small library that lets two applications sharing one configuration prove it to each other with a short numeric PIN, without a token service. The producer calculates the PIN for the current UTC window and sends it; the consumer calculates the same PIN and compares.

## Overview

`PinService` implements TOTP (RFC 6238) with HMAC-SHA256 and the RFC 4226 truncation:

1. Time is divided into windows of `WindowInSeconds`, counted from the Unix epoch in UTC, so servers in different time zones agree on them.
2. The HMAC key is `SecretKeyWithPatternSupport` as UTF-8. Before it is used, each placeholder is replaced with a number calculated from the UTC start of the window: `{YO}` is the year plus `OffsetYear`, `{MO}` the month plus `OffsetMonth`, `{DO}` the day of the month plus `OffsetDay`, and `{HO}` the hour plus `OffsetHour`. A secret with no placeholders is used as it is.
3. The window number is the HMAC message; the result is truncated to `PinLength` digits.

Nothing is stored, issued or refreshed. Both sides need only the same `SecurityPinOptions` and a clock kept in step by NTP.

## Installation

```bash
dotnet add package Roadbed.Security.Pin
```

The library depends on `Roadbed.Common` only; the cryptography comes from the .NET base library.

## Key Features

- **No infrastructure.** The PIN is calculated on each side from shared options; no token server, database or refresh step.
- **Split secrets.** The placeholders and offsets let a host keep part of the secret in one store (a KeePass entry) and part in another (appsettings). Where each value lives is the host's decision.
- **Midnight-safe.** Placeholders are filled from the start of the window being computed, not from the moment of the call, so a PIN made at 23:59:59 still validates at 00:00:05.
- **Clock drift tolerated.** By default the window before and after the current one are accepted too.
- **Testable.** The clock is an injected `TimeProvider`; tests use `FakeTimeProvider`.

## Quick Start

### 1. Set the options at start-up

Every default in `SecurityPinOptions` is published in this public repository, so a host overrides them. Each host has an installer that populates the options and registers them:

```csharp
namespace Foo.Installers;

using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Roadbed;
using Roadbed.Secrets.KeePass;
using Roadbed.Security.Pin;

public sealed class PinInstaller : IServiceCollectionInstaller
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(provider => new SecurityPinOptions
        {
            SecretKeyWithPatternSupport = provider.GetRequiredService<KeePassReader>().Read("Foo PIN").Password,
            OffsetYear = long.Parse(configuration["Pin:OffsetYear"]!, CultureInfo.InvariantCulture),
            OffsetMonth = long.Parse(configuration["Pin:OffsetMonth"]!, CultureInfo.InvariantCulture),
            OffsetDay = long.Parse(configuration["Pin:OffsetDay"]!, CultureInfo.InvariantCulture),
            OffsetHour = long.Parse(configuration["Pin:OffsetHour"]!, CultureInfo.InvariantCulture),
        });
    }
}
```

`InstallSecurityPin` (found by `InstallModulesInAppDomain`, or run with `services.InstallModule<InstallSecurityPin>(configuration)`) registers `PinService` as a singleton built from those options, whichever installer runs first.

### 2. Producer: send the PIN

```csharp
int pin = this._pinService.Generate();
NetHttpRequest request = new NetHttpRequest
{
    HttpEndPoint = new Uri("https://consumer.example.com/api/orders"),
    Method = HttpMethod.Get,
    Authentication = new NetHttpAuthentication
    {
        AuthenticationType = NetHttpAuthenticationType.Bearer,
        Value = pin.ToString(CultureInfo.InvariantCulture),
    },
};
```

### 3. Consumer: validate it

```csharp
// authorizationHeader is the request's Authorization header value, or null.
string? presented = null;
if ((authorizationHeader is not null) && authorizationHeader.StartsWith("Bearer ", StringComparison.Ordinal))
{
    presented = authorizationHeader.Substring("Bearer ".Length);
}

if (!this._pinService.Validate(presented))
{
    return Results.Unauthorized();
}
```

## Several purposes in one application

One `PinService` serves one set of options. A second purpose is a second instance with its own options, for example a keyed singleton:

```csharp
services.AddKeyedSingleton("partner-a", (provider, _) => new PinService(partnerAOptions, provider.GetRequiredService<TimeProvider>()));
services.AddKeyedSingleton("partner-b", (provider, _) => new PinService(partnerBOptions, provider.GetRequiredService<TimeProvider>()));
```

## API Surface

| Type                 | Kind         | Purpose                                                                                     |
| -------------------- | ------------ | ------------------------------------------------------------------------------------------- |
| `SecurityPinOptions` | Sealed class | The shared values: secret, offsets, window, PIN length, adjacent windows. `init` properties. |
| `PinService`         | Sealed class | `int Generate()`, `bool Validate(int)`, `bool Validate(string?)`.                           |
| `InstallSecurityPin` | Installer    | Registers `PinService` (singleton) and `TimeProvider.System` when no clock is registered.     |

## Behavior notes

- **PIN lifetime.** With the defaults (600-second window, adjacent windows on), a PIN is accepted for 10 to 20 minutes after it is made. With `AcceptAdjacentWindows = false` it is accepted only until its window ends, which can be a second later.
- **Leading zeros.** `Generate()` returns an `int`, so `049593` is `49593`. `Validate(string)` accepts the text with or without the leading zeros, up to `PinLength` digits.
- **Malformed text.** `Validate(string)` returns `false` for null, empty, signed, spaced, non-digit or over-long text; it never throws for bad input.
- **Bad options throw.** The `PinService` constructor rejects a blank secret, a brace that is not one of the four placeholders, a window under one second, and a PIN length outside 4 to 9.
- **Guessing.** A 6-digit PIN has a million values and, with adjacent windows on, three are valid at any moment. A consumer should limit failed attempts per caller; at 4 digits that is essential.
- **Public defaults.** A host that keeps the defaults produces PINs anyone with this library can calculate.
