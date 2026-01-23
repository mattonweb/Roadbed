# Roadbed

Core library providing foundational abstractions, dependency injection utilities, logging base classes, and environment management.

## Overview

This is the foundation package for the Roadbed framework. It provides automatic module discovery and registration, base classes with built-in logging, and environment configuration utilities.

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
```

This single line:
1. Scans all loaded assemblies for `IServiceCollectionInstaller` implementations
2. Instantiates each installer
3. Calls `ConfigureServices()` on each
4. Wires up ServiceLocator for NuGet package self-containment

#### Create Module Installers
```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Roadbed;

public class InstallMyModule : IServiceCollectionInstaller
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Register your services
        services.AddScoped<IFooRepository, FooRepository>();
        services.AddScoped<FooEntity>();
        
        // Configure Dapper, etc.
        DapperMapping.Configure(typeof(FooDto));
        
        // Capture snapshot for ServiceLocator (for NuGet packages)
        ServiceLocator.SetLocatorProvider(services.BuildServiceProvider());
    }
}
```

The installer is discovered and executed automatically - no manual registration needed!

### BaseClassWithLogging\<T\>

Base class providing convenient logging methods with automatic log level checking for performance.
```csharp
using Roadbed;
using Microsoft.Extensions.Logging;

public class FooService : BaseClassWithLogging<FooService>
{
    public FooService(ILoggerFactory factory)
        : base(factory)
    {
    }
    
    public void DoWork()
    {
        this.LogDebug("Starting work");
        this.LogInformation("Processing item {Id}", itemId);
        
        try
        {
            // Business logic
            this.LogTrace("Detailed trace info");
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Work failed for {Id}", itemId);
            throw;
        }
    }
    
    public void ProcessWithScope(string transactionId)
    {
        using (this.BeginScope("transactionId", transactionId))
        {
            this.LogInformation("Processing transaction");
            // All logs within this scope will include transactionId
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

// With parameters (structured logging)
this.LogInformation("User {UserId} logged in", userId);
this.LogWarning("Failed attempt {Count} for {User}", attemptCount, username);

// With exceptions
this.LogError(exception, "Operation failed");
this.LogError(exception, "Failed for {Id}", itemId);

// Scoped logging
using (this.BeginScope("key", value))
{
    // All logs include the scope
}
```

#### Performance Benefits

All logging methods check `IsEnabled()` before formatting messages, preventing unnecessary allocations:
```csharp
// ✅ Efficient - only formats if Debug logging is enabled
this.LogDebug("Processing {Count} items", items.Count);

// ❌ Wasteful - always formats even if Debug is disabled
this.Logger.LogDebug("Processing {Count} items", items.Count);
```

### Environment Configuration

Standardized environment detection and configuration.

#### CommonEnvironmentType Enum
```csharp
public enum CommonEnvironmentType
{
    Unknown = 0,
    Local = 1,
    Development = 3,
    Qa = 5,
    Staging = 7,
    Production = 9
}
```

#### Usage
```csharp
using Roadbed;

// In appsettings.json
{
  "Environment": "Development"
}

// In code
string envString = configuration["Environment"];
CommonEnvironmentType environment = envString.GetCommonEnvironment();

switch (environment)
{
    case CommonEnvironmentType.Production:
        // Production-specific configuration
        break;
    case CommonEnvironmentType.Development:
        // Development-specific configuration
        break;
}
```

#### String Conversion

The `GetCommonEnvironment()` extension method handles various formats:
```csharp
"local".GetCommonEnvironment()        // → Local
"dev".GetCommonEnvironment()          // → Development
"development".GetCommonEnvironment()  // → Development
"qa".GetCommonEnvironment()           // → Qa
"test".GetCommonEnvironment()         // → Qa
"staging".GetCommonEnvironment()      // → Staging
"pro".GetCommonEnvironment()          // → Production
"production".GetCommonEnvironment()   // → Production
"invalid".GetCommonEnvironment()      // → Unknown
```

## Complete Setup Example

### 1. Create Module Installer
```csharp
// MyApp.Data/Installers/InstallDataModule.cs
using Roadbed;

public class InstallDataModule : IServiceCollectionInstaller
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Configure Dapper
        DapperMapping.Configure(typeof(FooDto), typeof(BarDto));
        
        // Register repositories
        services.AddScoped<IFooRepository, FooRepository>();
        services.AddScoped<IBarRepository, BarRepository>();
        
        // Register entities
        services.AddScoped<FooEntity>();
        services.AddScoped<BarEntity>();
        
        // Capture ServiceLocator snapshot
        ServiceLocator.SetLocatorProvider(services.BuildServiceProvider());
    }
}
```

### 2. Application Startup
```csharp
// Program.cs
using Roadbed;

var builder = WebApplication.CreateBuilder(args);

// Single line auto-discovers and registers all modules
builder.Services.InstallModulesInAppDomain(builder.Configuration);

var app = builder.Build();
app.Run();
```

### 3. Use in Services
```csharp
public class FooService : BaseClassWithLogging<FooService>
{
    private readonly FooEntity _fooEntity;

    public FooService(FooEntity fooEntity, ILoggerFactory factory)
        : base(factory)
    {
        _fooEntity = fooEntity;
    }

    public async Task ProcessAsync()
    {
        this.LogInformation("Starting process");
        
        var items = await _fooEntity.ListAsync();
        this.LogDebug("Found {Count} items", items.Count);
        
        // Business logic
    }
}
```

## ServiceLocator Pattern

While generally considered an anti-pattern, ServiceLocator is used internally by NuGet packages to enable self-contained operation without requiring consumers to manually register dependencies.
```csharp
// In your installer
ServiceLocator.SetLocatorProvider(services.BuildServiceProvider());

// Internal NuGet package usage (you typically won't call this directly)
var logger = ServiceLocator.GetService<ILoggerFactory>();
```

**Note**: Application code should use dependency injection, not ServiceLocator. This is only for NuGet package internals.

## Requirements

- .NET 10.0+
- Microsoft.Extensions.DependencyInjection
- Microsoft.Extensions.Logging
- Microsoft.Extensions.Configuration