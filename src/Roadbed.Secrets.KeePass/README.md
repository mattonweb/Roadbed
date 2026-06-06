# Roadbed.Secrets.KeePass

A small, self-contained library for loading secrets out of a KeePass2 (`.kdbx`) database at application startup. The host application picks a master key and a database path however it wants; this library opens the database once, caches every entry, and serves `Read(title)` calls from memory.

## Overview

`KeePassReader` opens the database file once at construction, decrypts every entry, builds an in-memory dictionary keyed by entry Title, and closes the file. All construction-time errors — missing file, blank options, wrong master key, malformed database — surface in the constructor so the host fails fast at startup rather than at first use.

The class is intentionally **not sealed** so applications managing multiple KeePass databases in DI can declare a one-line subclass per database, paired with a marker subinterface of `IKeePassOptions`. Each subclass gets its own typed logger category for log filtering.

## Installation

```bash
dotnet add package Roadbed.Secrets.KeePass
```

The library depends on `KeePassLib.Standard` (the .NET KeePass library) and `Roadbed.Common`.

## Key Features

- **Synchronous, startup-time API.** No `async` ceremony — the host opens its secret store once before the rest of the world starts up.
- **In-memory cache.** Every `Read` call is a dictionary lookup; the `.kdbx` file is never re-opened.
- **Fail-fast construction.** A wrong master key or missing file throws in the constructor, so the host crashes at startup instead of hiding the failure behind a lazy first use.
- **Multi-database friendly.** `KeePassReader` is unsealed and ships a `protected` ctor that accepts a non-generic `ILogger`, so per-database subclasses keep distinct logger categories.
- **No `IConfiguration` dependency.** The host owns where the master key and path come from (config file, environment variable, command-line arg, hardware token, …). This library only sees the `IKeePassOptions` POCO.

## Quick Start

### 1. Implement `IKeePassOptions`

```csharp
using Roadbed.Secrets.KeePass;

internal sealed class FooKeePassOptions : IKeePassOptions
{
    public required string MasterKey { get; init; }

    public required string DatabasePath { get; init; }
}
```

### 2. Register the options and the reader as singletons

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<IKeePassOptions>(new FooKeePassOptions
{
    MasterKey = Environment.GetEnvironmentVariable("FOO_KEEPASS_KEY") ?? string.Empty,
    DatabasePath = @"C:\secrets\foo.kdbx",
});

builder.Services.AddSingleton<KeePassReader>();

using var host = builder.Build();
await host.RunAsync();
```

### 3. Inject and read

```csharp
using Microsoft.Extensions.Logging;
using Roadbed.Secrets.KeePass;

public sealed class FooApiClient
{
    private readonly KeePassSecret _secret;

    public FooApiClient(KeePassReader keePass, ILogger<FooApiClient> logger)
    {
        ArgumentNullException.ThrowIfNull(keePass);

        this._secret = keePass.Read("FooApi");
    }

    public async Task DoThingAsync(CancellationToken cancellationToken)
    {
        // this._secret.UserName, .Password, .Url, .Notes, .Title
    }
}
```

## Multiple databases

When a single host needs to read from more than one KeePass database, declare a marker subinterface of `IKeePassOptions` and a one-line subclass of `KeePassReader` per database. Each subclass uses the `protected KeePassReader(IKeePassOptions, ILogger)` ctor so it can pass a typed `ILogger<TSubclass>` and keep a distinct logger category.

```csharp
// Per-database marker interfaces
internal interface IFooKeePassOptions : IKeePassOptions { }
internal interface IBarKeePassOptions : IKeePassOptions { }

// Per-database concrete options
internal sealed class FooKeePassOptions : IFooKeePassOptions { /* MasterKey, DatabasePath */ }
internal sealed class BarKeePassOptions : IBarKeePassOptions { /* MasterKey, DatabasePath */ }

// Per-database reader subclasses
public sealed class FooKeePassReader : KeePassReader
{
    public FooKeePassReader(IFooKeePassOptions options, ILogger<FooKeePassReader> logger)
        : base(options, logger)
    {
    }
}

public sealed class BarKeePassReader : KeePassReader
{
    public BarKeePassReader(IBarKeePassOptions options, ILogger<BarKeePassReader> logger)
        : base(options, logger)
    {
    }
}
```

Register each pair as a singleton; consumers inject `FooKeePassReader` or `BarKeePassReader` directly.

## API Surface

| Type                | Kind         | Purpose                                                                                                   |
| ------------------- | ------------ | --------------------------------------------------------------------------------------------------------- |
| `IKeePassOptions`   | Interface    | Supplies `MasterKey` and `DatabasePath`. The host owns how these values are sourced.                       |
| `KeePassReader`     | Class (unsealed) | Opens a `.kdbx` once at construction, caches entries, serves `Read(title)` from memory.                  |
| `KeePassSecret`     | Sealed class | A cached entry: `Title`, `UserName`, `Password`, `Url`, `Notes`.                                          |

## Behavior notes

- **Title is the key.** `Read(title)` returns the first entry with the matching Title; KeePass allows duplicate titles, and the first one wins.
- **All groups are walked.** `RootGroup.GetEntries(true)` recurses into every subgroup. A KeePass UI-created database with the default subgroup hierarchy (General, Windows, Network, …) just works.
- **Missing entries throw.** `Read(title)` throws `InvalidOperationException` rather than returning `null`. Read calls happen at startup; an unknown title is a configuration error worth crashing on.
- **String fields only.** The cached `KeePassSecret` exposes the standard string fields (Title, UserName, Password, URL, Notes). Binary attachments, expiry, history, and custom string fields are not surfaced in v1.
