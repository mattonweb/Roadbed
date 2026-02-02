# Roadbed.Common

Core library providing foundational abstractions, dependency injection utilities, logging base classes, and environment management.

## Overview

This is the foundation package for the Roadbed framework. It provides automatic module discovery and registration, base classes with built-in performance-optimized logging, and shared utility types used across all Roadbed packages.

For the full type catalog, API signatures, and design rationale, see the [Architecture Document](/docs/architectural-design/architecture-roadbed-common.md).

## Installation

```bash
dotnet add package Roadbed.Common
```

## Key Features

### Auto-Discovery Module Registration

Automatically find and register all `IServiceCollectionInstaller` implementations in your application domain.

#### Application Startup

```csharp
using Roadbed;

var builder = WebApplication.CreateBuilder(args);

// Automatically discovers and invokes all IServiceCollectionInstaller implementations
builder.Services.InstallModulesInAppDomain(builder.Configuration);

var app = builder.Build();
app.Run();
```

This single call scans all loaded assemblies via BFS, finds every `IServiceCollectionInstaller`, and calls `ConfigureServices()` on each one. No manual wiring needed.

#### Create Module Installers

Each class library implements one installer:

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public class InstallFooSdk : IServiceCollectionInstaller
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IFooRepository, FooRepository>();
        services.AddSingleton<IFooService, FooService>();

        // Capture snapshot for ServiceLocator (for NuGet self-containment)
        ServiceLocator.SetLocatorProvider(services.BuildServiceProvider());
    }
}
```

The installer is discovered and executed automatically — no manual registration needed.

### BaseClassWithLogging

Base class providing convenient logging methods with automatic log level checking for performance. All logging methods check `IsEnabled()` before formatting messages, preventing unnecessary string allocation.

```csharp
using Microsoft.Extensions.Logging;
using Roadbed;

public sealed class FooService : BaseClassWithLogging
{
    private readonly IBarRepository _repository;

    public FooService(
        IBarRepository repository,
        ILogger<FooService> logger)
        : base(logger)
    {
        ArgumentNullException.ThrowIfNull(repository);
        this._repository = repository;
    }

    public async Task ProcessAsync(CancellationToken cancellationToken = default)
    {
        this.LogInformation("Starting process");

        var items = await this._repository.ListAsync(cancellationToken);
        this.LogDebug("Found {Count} items", items.Count);

        foreach (var item in items)
        {
            using (this.BeginScope("itemId", item.Id))
            {
                this.LogTrace("Processing item");
                // Business logic
            }
        }
    }
}
```

#### Available Log Methods

```csharp
// Simple messages
this.LogTrace("message");
this.LogDebug("message");
this.LogInformation("message");
this.LogWarning("message");
this.LogError("message");
this.LogCritical("message");

// With parameters (structured logging)
this.LogInformation("User {UserId} logged in", userId);

// With exceptions
this.LogError(exception, "Operation failed for {Id}", itemId);
this.LogWarning(exception, "Retry attempt {Count}", retryCount);
this.LogCritical(exception, "Fatal error in {Service}", serviceName);

// Scoped logging
using (this.BeginScope("transactionId", transactionId))
{
    this.LogInformation("All logs in this block include transactionId");
}
```

#### Performance Benefits

```csharp
// ✅ Efficient — only formats if Debug logging is enabled
this.LogDebug("Processing {Count} items", items.Count);

// ❌ Wasteful — always formats even if Debug is disabled
this._logger.LogDebug("Processing {Count} items", items.Count);
```

#### When You Need ILoggerFactory

For classes that need to create child loggers, use `BaseClassWithLoggingFactory<T>` instead:

```csharp
public sealed class FooOrchestrator : BaseClassWithLoggingFactory<FooOrchestrator>
{
    public FooOrchestrator(ILoggerFactory loggerFactory)
        : base(loggerFactory)
    {
    }
}
```

Most classes should use `BaseClassWithLogging` with `ILogger<T>`. See the [Architecture Document](/docs/architectural-design/architecture-roadbed-common.md) for the decision guide.

### Environment Configuration

Standardized environment detection with a string extension method:

```csharp
using Roadbed;

string envString = configuration["Environment"];
CommonEnvironmentType environment = envString.GetCommonEnvironment();
```

Recognized strings (case-insensitive):

| Input                    | Result        |
| ------------------------ | ------------- |
| `"local"`                | `Local`       |
| `"dev"`, `"development"` | `Development` |
| `"qa"`, `"test"`         | `Qa`          |
| `"staging"`              | `Staging`     |
| `"pro"`, `"production"`  | `Production`  |
| anything else            | `Unknown`     |

### ServiceLocator

While generally an anti-pattern, `ServiceLocator` enables NuGet packages to operate self-contained without requiring consumers to manually register internal dependencies.

```csharp
// Inside your installer
ServiceLocator.SetLocatorProvider(services.BuildServiceProvider());

// Internal NuGet package usage (you typically won't call this directly)
var logger = ServiceLocator.GetService<ILoggerFactory>();
```

Application code should use constructor injection, not `ServiceLocator`.

## Requirements

- .NET 10.0+
- Microsoft.Extensions.DependencyInjection
- Microsoft.Extensions.Logging
- Microsoft.Extensions.Configuration

