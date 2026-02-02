# Roadbed.Common Architecture

Roadbed.Common is the foundation library for all Roadbed packages. It provides performance-optimized logging base classes, auto-discovery module registration, and shared utility types used across the entire Roadbed ecosystem.

---

## For AI Assistants

This document is the authoritative reference for the Roadbed.Common NuGet package. When a developer asks you to create a class that uses Roadbed.Common — or any Roadbed package that depends on it — use this document to select the correct base class, register services properly, and follow the established conventions.

**Key rules to follow:**

1. **Always use `this.`** when accessing instance members (fields, properties, methods).
2. **Inherit from `BaseClassWithLogging`** (non-generic, takes `ILogger`) for most classes — repositories, services, domain objects.
3. **Inherit from `BaseClassWithLoggingFactory<T>`** (generic, takes `ILoggerFactory`) only when you need to create additional loggers from the factory.
4. **Use `this.LogDebug()`, `this.LogInformation()`, etc.** — never call `this._logger.LogDebug()` directly. The base class methods check `IsEnabled()` before formatting, preventing unnecessary string allocation.
5. **Use `ILogger<T>`** in constructors, not `ILoggerFactory`, unless the class genuinely needs to create loggers for other categories.
6. **Use `ArgumentNullException.ThrowIfNull()`** for null validation — not `?? throw new ArgumentNullException(...)`.
7. **Use `ArgumentException.ThrowIfNullOrWhiteSpace()`** for string validation.
8. **Flatten namespaces** — remove `.Dtos`, `.Entities`, `.Extensions` suffixes so consumers don't need extra `using` statements.
9. **Use Newtonsoft.Json** for serialization, not System.Text.Json.
10. **Register modules via `IServiceCollectionInstaller`** — never wire services manually in `Program.cs`.

---

## Table of Contents

1. [For AI Assistants](architecture-roadbed-common.md#for-ai-assistants)
2. [Type Catalog](architecture-roadbed-common.md#type-catalog)
3. [Namespace Convention](architecture-roadbed-common.md#namespace-convention)
4. [Logging Architecture](architecture-roadbed-common.md#logging-architecture)
    - [BaseClassWithLogging](architecture-roadbed-common.md#baseclasswithlogging)
    - [BaseClassWithLoggingFactory\<T\>](architecture-roadbed-common.md#baseclasswithloggingfactoryt)
    - [When to Use Which](architecture-roadbed-common.md#when-to-use-which)
    - [Logger Extension Methods](architecture-roadbed-common.md#logger-extension-methods)
5. [Module Auto-Discovery](architecture-roadbed-common.md#module-auto-discovery)
    - [IServiceCollectionInstaller](architecture-roadbed-common.md#iservicecollectioninstaller)
    - [ServiceCollectionExtensions](architecture-roadbed-common.md#servicecollectionextensions)
    - [ServiceLocator](architecture-roadbed-common.md#servicelocator)
    - [Built-in Installer: InstallExtensionsLogging](architecture-roadbed-common.md#built-in-installer-installextensionslogging)
6. [Value Objects](architecture-roadbed-common.md#value-objects)
    - [CommonBusinessKey](architecture-roadbed-common.md#commonbusinesskey)
    - [CommonKeyValuePair\<TKey, TValue\>](architecture-roadbed-common.md#commonkeyvaluepairtkey-tvalue)
    - [CommonKeyValuePairListConverter\<TKey, TValue\>](architecture-roadbed-common.md#commonkeyvaluepairlistconvertertkey-tvalue)
    - [CommonEmbeddedResourceResponse](architecture-roadbed-common.md#commonembeddedresourceresponse)
7. [Environment Utilities](architecture-roadbed-common.md#environment-utilities)
8. [Assembly Utilities](architecture-roadbed-common.md#assembly-utilities)
9. [Implementation Walkthrough](architecture-roadbed-common.md#implementation-walkthrough)
10. [Common Pitfalls](architecture-roadbed-common.md#common-pitfalls)

---

## Type Catalog

Roadbed.Common contains **16 public types** organized into four groups.

### Logging (4 types)

| Type                             | Kind           | Namespace                   | Purpose                                                                                      |
| -------------------------------- | -------------- | --------------------------- | -------------------------------------------------------------------------------------------- |
| `BaseClassWithLogging`           | Abstract class | `Roadbed`                   | Base class with level-checked logging convenience methods. Takes `ILogger`.                  |
| `BaseClassWithLoggingFactory<T>` | Abstract class | `Roadbed`                   | Extends `BaseClassWithLogging` with `ILoggerFactory` and typed `ILogger<T>`.                 |
| `CommonLoggerExtension`          | Static class   | `Roadbed`                   | Extension methods on `ILogger`: `BeginScope(key, value)` and `LogWithCheck(level, message)`. |
| `InstallExtensionsLogging`       | Class          | `Roadbed.Common.Installers` | Auto-discovered installer that registers `ILoggerFactory` in DI.                             |

### Module Auto-Discovery (3 types)

| Type                          | Kind         | Namespace | Purpose                                                                                     |
| ----------------------------- | ------------ | --------- | ------------------------------------------------------------------------------------------- |
| `IServiceCollectionInstaller` | Interface    | `Roadbed` | Contract for module registration. Implement once per assembly.                              |
| `ServiceCollectionExtensions` | Static class | `Roadbed` | `InstallModulesInAppDomain()` and `InstallFromAssembly<T>()` extension methods.             |
| `ServiceLocator`              | Static class | `Roadbed` | Anti-pattern used for NuGet self-containment. `GetService<T>()` and `SetLocatorProvider()`. |

### Value Objects (4 types)

|Type|Kind|Namespace|Purpose|
|---|---|---|---|
|`CommonBusinessKey`|Partial record|`Roadbed.Common`|Validated uppercase business key value object with regex enforcement.|
|`CommonKeyValuePair<TKey, TValue>`|Sealed class|`Roadbed.Common`|Non-unique key/value pair with full equality semantics.|
|`CommonKeyValuePairListConverter<TKey, TValue>`|Class|`Roadbed.Common.Converters`|Newtonsoft.Json converter for `IList<CommonKeyValuePair<TKey, TValue>>`.|
|`CommonEmbeddedResourceResponse`|Class|`Roadbed.Common`|Result wrapper for embedded resource read operations.|

### Utilities (5 types)

|Type|Kind|Namespace|Purpose|
|---|---|---|---|
|`CommonEnvironmentType`|Enum|`Roadbed`|Application environment: Unknown, Local, Development, Qa, Staging, Production.|
|`CommonEnvironment`|Static class|`Roadbed.Common`|Converts strings to `CommonEnvironmentType`.|
|`CommonStringExtension`|Static class|`Roadbed`|`string.GetCommonEnvironment()` extension method.|
|`CommonAssembly`|Static class|`Roadbed.Common`|Reads embedded resources from assemblies.|
|`CommonAssemblyExtension`|Static class|`Roadbed`|Extension methods: `Assembly.ReadTextResource()`, `IsAssemblyLoaded()`.|

---

## Namespace Convention

Roadbed.Common flattens namespaces so that consuming code needs minimal `using` statements. The original folder-based namespaces are replaced:

|Original Namespace|Flattened To|Reason|
|---|---|---|
|`Roadbed.Common.Dependencies`|`Roadbed`|`IServiceCollectionInstaller` should not require `using Roadbed.Common.Dependencies`|
|`Roadbed.Common.Extensions`|`Roadbed`|Extension methods should be available with `using Roadbed`|
|`Roadbed.Common.Entities`|`Roadbed.Common`|Utility classes available with `using Roadbed.Common`|
|`Roadbed.Common.Dtos`|`Roadbed.Common`|Value objects available with `using Roadbed.Common`|

**Result:** Most consuming code only needs:

```csharp
using Roadbed;
using Roadbed.Common;
```

Each file includes a comment explaining the namespace change:

```csharp
/*
 * The namespace Roadbed.Common.Extensions was removed on purpose and replaced
 * with Roadbed so that no additional using statements are required.
 */
namespace Roadbed;
```

---

## Logging Architecture

### Inheritance Hierarchy

```
BaseClassWithLogging                    ← Takes ILogger (non-generic)
    │
    ├── BaseClassWithLoggingFactory<T>  ← Adds ILoggerFactory + ILogger<T>
    │
    ├── BaseAsyncCrudlRepository<…>     ← Roadbed.Crud repositories
    │
    └── BaseAsyncCrudlService<…>        ← Roadbed.Crud services
```

Every Roadbed base class inherits from `BaseClassWithLogging`. This means the level-checked convenience methods (`this.LogDebug()`, `this.LogInformation()`, etc.) are available everywhere in the framework.

### BaseClassWithLogging

The non-generic base class. Takes an `ILogger` via constructor. All logging convenience methods delegate to `CommonLoggerExtension.LogWithCheck()`, which checks `ILogger.IsEnabled()` before formatting.

```csharp
namespace Roadbed;

public abstract class BaseClassWithLogging
{
    private readonly ILogger _logger;

    protected BaseClassWithLogging();                // Uses NullLogger.Instance
    protected BaseClassWithLogging(ILogger logger);  // Falls back to NullLogger if null

    // Convenience methods (level-checked):
    public void LogTrace(string message);
    public void LogTrace(string message, params object[] param);
    public void LogDebug(string message);
    public void LogDebug(string message, params object[] param);
    public void LogInformation(string message);
    public void LogInformation(string message, params object[] param);
    public void LogWarning(string message);
    public void LogWarning(string message, params object[] param);
    public void LogWarning(Exception exception, string message);
    public void LogWarning(Exception exception, string message, params object[] param);
    public void LogError(string message);
    public void LogError(string message, params object[] param);
    public void LogError(Exception exception, string message);
    public void LogError(Exception exception, string message, params object[] param);
    public void LogCritical(string message);
    public void LogCritical(string message, params object[] param);
    public void LogCritical(Exception exception, string message);
    public void LogCritical(Exception exception, string message, params object[] param);

    // Scope support:
    public IDisposable? BeginScope(string key, object value);
}
```

**Constructor pattern for classes inheriting from `BaseClassWithLogging`:**

```csharp
public sealed class FooRepository : BaseClassWithLogging
{
    private readonly IDataConnectionFactory _connectionFactory;

    public FooRepository(
        IDataConnectionFactory connectionFactory,
        ILogger<FooRepository> logger)
        : base(logger)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        this._connectionFactory = connectionFactory;
    }

    public async Task<Foo> CreateAsync(
        Foo entity,
        CancellationToken cancellationToken = default)
    {
        this.LogDebug("Creating foo: {Name}", entity.Name);
        // Implementation
    }
}
```

### BaseClassWithLoggingFactory\<T\>

The generic base class. Extends `BaseClassWithLogging` with `ILoggerFactory` and a typed `ILogger<T>` property. Use this when you need to create additional loggers from the factory.

```csharp
namespace Roadbed;

public abstract class BaseClassWithLoggingFactory<TCategoryName>
    : BaseClassWithLogging
{
    protected BaseClassWithLoggingFactory();                          // Uses NullLoggerFactory
    protected BaseClassWithLoggingFactory(ILogger logger);           // ILogger, no factory
    protected BaseClassWithLoggingFactory(ILoggerFactory factory);   // Creates ILogger<T> from factory

    public ILogger<TCategoryName> Logger { get; }
    public ILoggerFactory LoggerFactory { get; }

    // Inherits all convenience methods from BaseClassWithLogging
}
```

**Constructor pattern for classes inheriting from `BaseClassWithLoggingFactory<T>`:**

```csharp
public sealed class FooService : BaseClassWithLoggingFactory<FooService>
{
    private readonly IFooRepository _repository;

    public FooService(
        IFooRepository repository,
        ILoggerFactory loggerFactory)
        : base(loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(repository);
        this._repository = repository;
    }

    public async Task ProcessAsync(CancellationToken cancellationToken = default)
    {
        // Level-checked convenience methods (preferred):
        this.LogInformation("Processing started");

        // Direct logger access (only when needed):
        this.Logger.LogInformation("Direct logger access");

        // Create a logger for another category:
        var childLogger = this.LoggerFactory.CreateLogger<ChildProcessor>();
    }
}
```

### When to Use Which

|Scenario|Base Class|Constructor Parameter|
|---|---|---|
|Repositories (Roadbed.Crud)|`BaseClassWithLogging`|`ILogger<T>`|
|Services (Roadbed.Crud)|`BaseClassWithLogging`|`ILogger<T>`|
|Domain classes with simple logging|`BaseClassWithLogging`|`ILogger<T>`|
|Classes that create child loggers|`BaseClassWithLoggingFactory<T>`|`ILoggerFactory`|
|Classes that need `ILogger<T>` property|`BaseClassWithLoggingFactory<T>`|`ILoggerFactory`|
|Scheduled jobs (Roadbed.Scheduling)|`BaseClassWithLoggingFactory<T>`|`ILoggerFactory`|
|HTTP client wrappers (Roadbed.Net)|`BaseClassWithLoggingFactory<T>`|`ILoggerFactory`|

**Default choice:** `BaseClassWithLogging` with `ILogger<T>`. Only upgrade to `BaseClassWithLoggingFactory<T>` when you genuinely need the factory or typed logger property.

### Logger Extension Methods

`CommonLoggerExtension` provides two extension methods on `ILogger` that are used internally by `BaseClassWithLogging`:

#### LogWithCheck

Checks `ILogger.IsEnabled()` before formatting the message. This is the performance optimization at the core of the logging architecture.

```csharp
// Signature
public static void LogWithCheck(this ILogger logger, LogLevel level, string message);
public static void LogWithCheck(this ILogger logger, LogLevel level, string message, params object[] param);

// What it does internally:
if (logger.IsEnabled(level))
{
    logger.Log(level, message, param);
}
```

**You should not call `LogWithCheck` directly.** Use the base class convenience methods (`this.LogDebug()`, etc.) which call it for you.

#### BeginScope (key/value overload)

Adds a single key/value pair to the logging scope:

```csharp
// Signature
public static IDisposable? BeginScope(this ILogger logger, string key, object value);

// Usage through base class:
using (this.BeginScope("transactionId", transaction.Id))
{
    this.LogInformation("Transaction completed");
}
// Log output includes: transactionId=12345
```

---

## Module Auto-Discovery

Roadbed uses an auto-discovery pattern so that NuGet packages can register their own services without requiring the consuming application to know about internal dependencies.

### How It Works

```
Application calls:
    services.InstallModulesInAppDomain(configuration);

        │
        ▼
    BFS traversal of all loaded assemblies
    (skips System.* and Microsoft.*)
        │
        ▼
    For each assembly:
        Find classes implementing IServiceCollectionInstaller
        Instantiate via Activator.CreateInstance()
        Call ConfigureServices(services, configuration)
        │
        ▼
    Walk referenced assemblies (recursive BFS)
```

### IServiceCollectionInstaller

The contract every module implements to register its services:

```csharp
namespace Roadbed;

public interface IServiceCollectionInstaller
{
    void ConfigureServices(IServiceCollection services, IConfiguration configuration);
}
```

**One installer per assembly.** Each class library in Roadbed has exactly one installer that registers all of its internal services.

#### Example: Creating an Installer

```csharp
namespace Foo.Sdk.Installers;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public class InstallFooSdk : IServiceCollectionInstaller
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IFooRepository, FooRepository>();
        services.AddSingleton<IFooService, FooService>();

        // Capture point-in-time snapshot for ServiceLocator
        ServiceLocator.SetLocatorProvider(services.BuildServiceProvider());
    }
}
```

### ServiceCollectionExtensions

Two extension methods for triggering auto-discovery:

|Method|Use When|
|---|---|
|`InstallModulesInAppDomain(configuration)`|Application startup — discovers all modules across all loaded assemblies via BFS|
|`InstallFromAssembly<T>(configuration)`|Register a single assembly's modules explicitly|

**Application startup pattern:**

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.InstallModulesInAppDomain(builder.Configuration);
var app = builder.Build();
app.Run();
```

**`InstallModulesInAppDomain` algorithm:**

1. Enqueue all loaded assemblies (excluding `System.*` and `Microsoft.*`)
2. Enqueue the entry assembly
3. BFS loop: dequeue, skip if visited, invoke installers, enqueue referenced assemblies
4. Silently catches exceptions from `GetReferencedAssemblies()` and continues

### ServiceLocator

A static service provider for NuGet self-containment. **This is an acknowledged anti-pattern.** It exists so that NuGet packages can resolve their own internal dependencies without requiring the consuming application to register them manually.

```csharp
namespace Roadbed;

public static class ServiceLocator
{
    // Set during installer execution
    public static void SetLocatorProvider(IServiceProvider serviceProvider);

    // Resolve a required service (returns default! if not initialized)
    public static T GetService<T>() where T : notnull;

    // Resolve an optional service (returns null if not found)
    public static T? TryGetService<T>() where T : class;

    // For testing only
    internal static void Reset();
}
```

**Rules:**

- Call `SetLocatorProvider()` inside your `IServiceCollectionInstaller` after `services.BuildServiceProvider()`.
- Thread-safe: uses `Lock` internally.
- Prefer constructor injection. Use `ServiceLocator` only when constructor injection is not available (e.g., inside NuGet package internals).

### Built-in Installer: InstallExtensionsLogging

Roadbed.Common includes one built-in installer that registers `Microsoft.Extensions.Logging`:

```csharp
namespace Roadbed.Common.Installers;

public class InstallExtensionsLogging : IServiceCollectionInstaller
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddLogging();

        IServiceProvider serviceProvider = services.BuildServiceProvider();
        ILoggerFactory loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        services.AddSingleton<ILoggerFactory>(loggerFactory);

        ServiceLocator.SetLocatorProvider(serviceProvider);
    }
}
```

This installer is auto-discovered by `InstallModulesInAppDomain()`. Consuming applications do not need to register logging manually.

---

## Value Objects

### CommonBusinessKey

A validated value object for business identifiers. Uses source-generated regex for validation.

**Valid characters:** Uppercase A–Z, digits 0–9, period (`.`), forward slash (`/`), underscore (`_`), hyphen (`-`).

**Regex pattern:** `^[A-Z0-9./_-]+$`

```csharp
namespace Roadbed.Common;

public partial record CommonBusinessKey
{
    public string Key { get; set; }  // Validates on set, throws ArgumentException

    // Factory methods:
    public static CommonBusinessKey FromString(string key);
    public static CommonBusinessKey FromString(string key, bool cleanAndFormat);
}
```

**Usage:**

```csharp
// Direct creation (value must already be valid):
var key = CommonBusinessKey.FromString("FOO-SERVICE/BAR");

// With cleaning (trims, uppercases, replaces spaces with underscores):
var key = CommonBusinessKey.FromString("foo service", cleanAndFormat: true);
// Result: Key = "FOO_SERVICE"
```

**Validation behavior:**

- Null or whitespace → `ArgumentException`
- Lowercase letters → `ArgumentException` ("Business Keys must be uppercase.")
- Invalid characters → `ArgumentException` (lists allowed characters)

### CommonKeyValuePair<TKey, TValue>

A non-unique key/value pair with full equality semantics. Used throughout the Roadbed messaging system.

```csharp
namespace Roadbed.Common;

public sealed class CommonKeyValuePair<TKey, TValue>
    : IEquatable<CommonKeyValuePair<TKey, TValue>>
{
    [JsonProperty("key", NullValueHandling = NullValueHandling.Ignore)]
    public TKey? Key { get; set; }

    [JsonProperty("value", NullValueHandling = NullValueHandling.Ignore)]
    public TValue? Value { get; set; }

    public CommonKeyValuePair();
    public CommonKeyValuePair(TKey key, TValue value);

    // Full equality: ==, !=, Equals(), GetHashCode()
}
```

**Why not `KeyValuePair<TKey, TValue>`?**

- `CommonKeyValuePair` is a class (reference type), not a struct
- Supports `null` keys and values
- Non-unique (multiple pairs can share the same key)
- JSON-serializable with Newtonsoft.Json attributes
- Full equality semantics

### CommonKeyValuePairListConverter<TKey, TValue>

A Newtonsoft.Json converter that serializes `IList<CommonKeyValuePair<TKey, TValue>>` as a JSON object (keys become property names) instead of a JSON array.

```csharp
// Without converter (default array serialization):
[{"key": "color", "value": "red"}, {"key": "size", "value": "large"}]

// With converter (object serialization):
{"color": "red", "size": "large"}
```

**Usage:**

```csharp
[JsonConverter(typeof(CommonKeyValuePairListConverter<string, string>))]
public IList<CommonKeyValuePair<string, string>>? Attributes { get; set; }
```

### CommonEmbeddedResourceResponse

A result wrapper for embedded resource read operations. Constructed only through internal factory methods.

```csharp
namespace Roadbed.Common;

public class CommonEmbeddedResourceResponse
{
    public bool IsReadSuccessful { get; }
    public string Data { get; }
    public string? ErrorMessage { get; }

    // Internal factory methods:
    internal static CommonEmbeddedResourceResponse Success(string value);
    internal static CommonEmbeddedResourceResponse Failure(string errorMessage);
}
```

**Usage (through extension method):**

```csharp
var response = this.GetType().Assembly.ReadTextResource(
    "Foo.Sdk.Resources.Schema.sql");

if (response.IsReadSuccessful)
{
    string sql = response.Data;
}
```

---

## Environment Utilities

### CommonEnvironmentType

An enum representing application deployment environments:

|Value|Int|Recognized Strings|
|---|---|---|
|`Unknown`|0|(default for unrecognized input)|
|`Local`|1|`"local"`|
|`Development`|3|`"dev"`, `"development"`|
|`Qa`|5|`"qa"`, `"test"`|
|`Staging`|7|`"staging"`|
|`Production`|9|`"pro"`, `"production"`|

### String-to-Environment Conversion

Two equivalent ways to convert a string:

```csharp
// Extension method (preferred):
CommonEnvironmentType env = "development".GetCommonEnvironment();
// Result: CommonEnvironmentType.Development

// Static method:
CommonEnvironmentType env = CommonEnvironment.GetCommonEnvironment("production");
// Result: CommonEnvironmentType.Production
```

The conversion trims whitespace and is case-insensitive.

---

## Assembly Utilities

### Reading Embedded Resources

Two equivalent APIs:

```csharp
// Extension method (preferred):
CommonEmbeddedResourceResponse response = assembly.ReadTextResource(
    "Foo.Sdk.Resources.Schema.sql");

// Static method:
CommonEmbeddedResourceResponse response = CommonAssembly.ReadTextResource(
    assembly,
    "Foo.Sdk.Resources.Schema.sql");
```

The resource name is matched case-insensitively against `Assembly.GetManifestResourceNames()`.

### Checking Assembly Loading

```csharp
bool loaded = CommonAssemblyExtension.IsAssemblyLoaded("Foo.Sdk");
```

Case-insensitive comparison against `AppDomain.CurrentDomain.GetAssemblies()`.

---

## Implementation Walkthrough

This walkthrough shows how to create a new class library that depends on Roadbed.Common.

### Step 1: Create the Class

Choose the appropriate base class:

```csharp
namespace Foo.Sdk;

using Microsoft.Extensions.Logging;
using Roadbed;

public sealed class FooProcessor : BaseClassWithLogging
{
    private readonly IBarService _barService;

    public FooProcessor(
        IBarService barService,
        ILogger<FooProcessor> logger)
        : base(logger)
    {
        ArgumentNullException.ThrowIfNull(barService);
        this._barService = barService;
    }

    public async Task ProcessAsync(CancellationToken cancellationToken = default)
    {
        this.LogInformation("Processing started");

        var items = await this._barService.ListAsync(cancellationToken);
        this.LogDebug("Found {Count} items", items.Count);

        foreach (var item in items)
        {
            using (this.BeginScope("itemId", item.Id))
            {
                this.LogTrace("Processing item");
                // Business logic
            }
        }

        this.LogInformation("Processing completed");
    }
}
```

### Step 2: Create the Module Installer

```csharp
namespace Foo.Sdk.Installers;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public class InstallFooSdk : IServiceCollectionInstaller
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IFooProcessor, FooProcessor>();

        ServiceLocator.SetLocatorProvider(services.BuildServiceProvider());
    }
}
```

### Step 3: Consuming Application

```csharp
var builder = WebApplication.CreateBuilder(args);

// This single call discovers InstallExtensionsLogging (from Roadbed.Common)
// AND InstallFooSdk (from Foo.Sdk) automatically.
builder.Services.InstallModulesInAppDomain(builder.Configuration);

var app = builder.Build();
app.Run();
```

---

## Common Pitfalls

### 1. Using `ILoggerFactory` When `ILogger` Suffices

```csharp
// ❌ Wrong — unnecessary factory dependency
public sealed class FooRepository : BaseClassWithLogging
{
    public FooRepository(ILoggerFactory loggerFactory)
        : base(loggerFactory.CreateLogger<FooRepository>())
    {
    }
}

// ✅ Correct — inject ILogger<T> directly
public sealed class FooRepository : BaseClassWithLogging
{
    public FooRepository(ILogger<FooRepository> logger)
        : base(logger)
    {
    }
}
```

### 2. Calling the Logger Directly Instead of Convenience Methods

```csharp
// ❌ Wrong — bypasses level check, formats string even when Debug is disabled
this._logger.LogDebug("Processing {Count} items with {Size} bytes", items.Count, totalSize);

// ✅ Correct — checks IsEnabled(LogLevel.Debug) before formatting
this.LogDebug("Processing {Count} items with {Size} bytes", items.Count, totalSize);
```

### 3. Missing `this.` on Instance Members

```csharp
// ❌ Wrong
public sealed class FooService : BaseClassWithLogging
{
    private readonly IFooRepository _repository;

    public FooService(IFooRepository repository, ILogger<FooService> logger)
        : base(logger)
    {
        _repository = repository;  // Missing this.
    }

    public async Task ProcessAsync()
    {
        LogInformation("Starting");       // Missing this.
        var items = _repository.List();   // Missing this.
    }
}

// ✅ Correct
public sealed class FooService : BaseClassWithLogging
{
    private readonly IFooRepository _repository;

    public FooService(IFooRepository repository, ILogger<FooService> logger)
        : base(logger)
    {
        this._repository = repository;
    }

    public async Task ProcessAsync()
    {
        this.LogInformation("Starting");
        var items = this._repository.List();
    }
}
```

### 4. Wrong Null Validation Pattern

```csharp
// ❌ Wrong — old pattern
public FooService(IFooRepository repository, ILogger<FooService> logger)
    : base(logger)
{
    this._repository = repository ?? throw new ArgumentNullException(nameof(repository));
}

// ✅ Correct — modern throw helper
public FooService(IFooRepository repository, ILogger<FooService> logger)
    : base(logger)
{
    ArgumentNullException.ThrowIfNull(repository);
    this._repository = repository;
}
```

### 5. Inheriting from the Generic Base When Not Needed

```csharp
// ❌ Wrong — does not need ILoggerFactory or ILogger<T> property
public sealed class FooRepository : BaseClassWithLoggingFactory<FooRepository>
{
    public FooRepository(ILoggerFactory loggerFactory)
        : base(loggerFactory)
    {
    }
}

// ✅ Correct — simple logging is all that's needed
public sealed class FooRepository : BaseClassWithLogging
{
    public FooRepository(ILogger<FooRepository> logger)
        : base(logger)
    {
    }
}
```

### 6. Registering Services in Program.cs Instead of an Installer

```csharp
// ❌ Wrong — manual registration in Program.cs
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IFooRepository, FooRepository>();
builder.Services.AddSingleton<IFooService, FooService>();
builder.Services.AddLogging();

// ✅ Correct — auto-discovery handles everything
var builder = WebApplication.CreateBuilder(args);
builder.Services.InstallModulesInAppDomain(builder.Configuration);
```

### 7. Forgetting to Call SetLocatorProvider in Installer

```csharp
// ❌ Wrong — ServiceLocator won't be able to resolve services from this assembly
public class InstallFooSdk : IServiceCollectionInstaller
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IFooService, FooService>();
        // Missing: ServiceLocator.SetLocatorProvider(services.BuildServiceProvider());
    }
}

// ✅ Correct
public class InstallFooSdk : IServiceCollectionInstaller
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IFooService, FooService>();
        ServiceLocator.SetLocatorProvider(services.BuildServiceProvider());
    }
}
```

### 8. Using System.Text.Json Instead of Newtonsoft.Json

```csharp
// ❌ Wrong
using System.Text.Json.Serialization;

[JsonPropertyName("key")]
public string? Key { get; set; }

// ✅ Correct
using Newtonsoft.Json;

[JsonProperty("key")]
public string? Key { get; set; }
```

### 9. CommonBusinessKey with Lowercase Input

```csharp
// ❌ Wrong — throws ArgumentException
var key = CommonBusinessKey.FromString("my-service");

// ✅ Correct — pre-validated uppercase
var key = CommonBusinessKey.FromString("MY-SERVICE");

// ✅ Correct — let the factory clean it
var key = CommonBusinessKey.FromString("my service", cleanAndFormat: true);
// Result: Key = "MY_SERVICE"
```

### 10. Using String Interpolation in Log Messages

```csharp
// ❌ Wrong — string is always allocated, even when level is disabled
this.LogDebug($"Processing item {item.Id} of type {item.Type}");

// ✅ Correct — structured logging with deferred formatting
this.LogDebug("Processing item {Id} of type {Type}", item.Id, item.Type);
```

---

## Quick Reference

### Using Statements

```csharp
using Roadbed;           // Base classes, extensions, enums, DI
using Roadbed.Common;    // Value objects (CommonBusinessKey, CommonKeyValuePair, etc.)
```

### Base Class Decision

```
Do you need ILoggerFactory or ILogger<T> property?
    ├── Yes → BaseClassWithLoggingFactory<T> with ILoggerFactory
    └── No  → BaseClassWithLogging with ILogger<T>  (default choice)
```

### Logging Method Signatures

|Method|Parameters|
|---|---|
|`this.LogTrace(message)`|`string`|
|`this.LogTrace(message, param)`|`string, params object[]`|
|`this.LogDebug(message)`|`string`|
|`this.LogDebug(message, param)`|`string, params object[]`|
|`this.LogInformation(message)`|`string`|
|`this.LogInformation(message, param)`|`string, params object[]`|
|`this.LogWarning(message)`|`string`|
|`this.LogWarning(message, param)`|`string, params object[]`|
|`this.LogWarning(exception, message)`|`Exception, string`|
|`this.LogWarning(exception, message, param)`|`Exception, string, params object[]`|
|`this.LogError(message)`|`string`|
|`this.LogError(message, param)`|`string, params object[]`|
|`this.LogError(exception, message)`|`Exception, string`|
|`this.LogError(exception, message, param)`|`Exception, string, params object[]`|
|`this.LogCritical(message)`|`string`|
|`this.LogCritical(message, param)`|`string, params object[]`|
|`this.LogCritical(exception, message)`|`Exception, string`|
|`this.LogCritical(exception, message, param)`|`Exception, string, params object[]`|
|`this.BeginScope(key, value)`|`string, object` → `IDisposable?`|