# C# Development Standards

## Overview

This document defines coding standards, patterns, and preferences for C# development projects, with special emphasis on unit testing, SDK development, and API integrations.

---

## Table of Contents

1. [Unit Testing Standards](https://claude.ai/chat/9b3d14de-7599-4bdd-96f6-c3d7bd390dc4#unit-testing-standards)
2. [DTO and SDK Patterns](https://claude.ai/chat/9b3d14de-7599-4bdd-96f6-c3d7bd390dc4#dto-and-sdk-patterns)
3. [Code Analysis Rules](https://claude.ai/chat/9b3d14de-7599-4bdd-96f6-c3d7bd390dc4#code-analysis-rules)
4. [IDisposable Implementation](https://claude.ai/chat/9b3d14de-7599-4bdd-96f6-c3d7bd390dc4#idisposable-implementation)
5. [Namespace Conventions](https://claude.ai/chat/9b3d14de-7599-4bdd-96f6-c3d7bd390dc4#namespace-conventions)
6. [General C# Preferences](https://claude.ai/chat/9b3d14de-7599-4bdd-96f6-c3d7bd390dc4#general-c-preferences)

---

## Unit Testing Standards

### Test Naming Convention

All unit tests must follow the Roy Osherove naming pattern:

```
{UnitOfWork}_{StateUnderTest}_{ExpectedBehavior}
```

#### Rules

1. **Alphanumeric Characters**: Use only alphanumeric characters (a-z, A-Z, 0-9) and underscores
2. **Pascal Case**: All alphabetic characters should be Pascal case (Upper Camel Case)
3. **Three Sections**: Separated by underscores

#### Examples

- `Constructor_NoParameters_InitializesWithDefaultValues`
- `MakeRequestAsync_NullRequest_ThrowsArgumentNullException`
- `ConnectionString_SqliteType_GeneratesCorrectConnectionString`

### Test Structure

#### XML Documentation

Every test method must have XML documentation:

```csharp
/// <summary>
/// Unit test to verify that [description of what is being tested].
/// </summary>
[TestMethod]
public void MethodName_Scenario_ExpectedResult()
```

#### Arrange/Act/Assert Pattern

Use the AAA pattern with Given/When/Then comments:

```csharp
[TestMethod]
public void Method_Scenario_Result()
{
    // Arrange (Given)
    var input = "test";
    
    // Act (When)
    var result = SomeMethod(input);
    
    // Assert (Then)
    Assert.AreEqual(
        expectedValue,
        result,
        "Detailed assertion message explaining what should happen.");
}
```

#### Region Directives

Organize code with regions:

```csharp
[TestClass]
public class MyClassTests
{
    #region Public Methods
    
    [TestMethod]
    public void Test1() { }
    
    [TestMethod]
    public void Test2() { }
    
    #endregion Public Methods
    
    #region Private Methods
    
    private void HelperMethod() { }
    
    #endregion Private Methods
}
```

#### Detailed Assertion Messages

Every Assert must include a descriptive message:

```csharp
Assert.IsNotNull(
    result,
    "Result should not be null when valid input is provided.");
    
Assert.AreEqual(
    expected,
    actual,
    "Property should return the value that was set.");
```

#### Specialized Assert Methods (MSTEST0037)

**IMPORTANT**: MSTest provides specialized assertion methods that should be used instead of `Assert.IsTrue()` or `Assert.IsFalse()` when checking collections, strings, or enumerable conditions.

##### String Contains/StartsWith/EndsWith

```csharp
// ✅ Correct - Use Assert.Contains
Assert.IsNotNull(instance.FullPath, "FullPath should not be null.");
Assert.Contains(
    instance.FullPath,
    expectedFile,
    "FullPath should contain the expected filename.");

// ✅ Correct - Use StringAssert for more complex string checks
StringAssert.Contains(
    instance.FullPath,
    expectedFile,
    "FullPath should contain the expected filename.");

// ❌ Avoid - Triggers MSTEST0037 warning
Assert.IsTrue(
    instance.FullPath.Contains(expectedFile),
    "FullPath should contain the expected filename.");
```

##### Collection Membership

```csharp
// ✅ Correct - Use CollectionAssert.Contains
CollectionAssert.Contains(
    collection,
    expectedItem,
    "Collection should contain the expected item.");

// ❌ Avoid - Triggers MSTEST0037 warning
Assert.IsTrue(
    collection.Contains(expectedItem),
    "Collection should contain the expected item.");
```

##### Collection Count

```csharp
// ✅ Correct - Use Assert.HasCount
Assert.HasCount(
    instance.DataRows,
    2,
    "DataRows should contain the expected number of items.");

// ❌ Avoid - Triggers MSTEST0037 warning
Assert.AreEqual(
    2,
    instance.DataRows.Count,
    "DataRows should contain the expected number of items.");
```

##### LINQ Any/All Conditions

```csharp
// ✅ Correct - Use Assert.IsTrue for LINQ (no specialized alternative)
Assert.IsTrue(
    collection.Any(x => x.Id == expectedId),
    "Collection should contain an item with the expected ID.");

// ✅ Correct - Or restructure to use CollectionAssert
var matchingItem = collection.FirstOrDefault(x => x.Id == expectedId);
Assert.IsNotNull(
    matchingItem,
    "Collection should contain an item with the expected ID.");
```

##### Quick Reference: When to Use What

|Condition|Use This Assert|Example|
|---|---|---|
|String contains|`StringAssert.Contains()` or `Assert.Contains()`|`StringAssert.Contains(actual, substring, message)`|
|String starts with|`StringAssert.StartsWith()`|`StringAssert.StartsWith(actual, prefix, message)`|
|String ends with|`StringAssert.EndsWith()`|`StringAssert.EndsWith(actual, suffix, message)`|
|String matches regex|`StringAssert.Matches()`|`StringAssert.Matches(actual, pattern, message)`|
|Collection contains|`CollectionAssert.Contains()`|`CollectionAssert.Contains(collection, item, message)`|
|Collection count|`Assert.HasCount()`|`Assert.HasCount(collection, expectedCount, message)`|
|Collection subset|`CollectionAssert.IsSubsetOf()`|`CollectionAssert.IsSubsetOf(subset, superset, message)`|
|LINQ Any/All|`Assert.IsTrue()` (acceptable)|`Assert.IsTrue(col.Any(x => condition), message)`|

##### Common Pattern for Path/Filename Validation

```csharp
/// <summary>
/// Unit test to verify that FullPath property contains expected filename.
/// </summary>
[TestMethod]
public void FullPath_SetValidValue_ContainsExpectedFilename()
{
    // Arrange (Given)
    var instance = new IoFileInfo();
    string expectedFile = "TestFile.txt";
    string testPath = Path.Combine(@"C:\TestFolder", expectedFile);
    
    // Act (When)
    instance.FullPath = testPath;
    
    // Assert (Then)
    Assert.IsNotNull(
        instance.FullPath,
        "FullPath should not be null after being set.");
    StringAssert.Contains(
        instance.FullPath,
        expectedFile,
        "FullPath should contain the expected filename.");
}
```

##### Common Pattern for Collection Count Validation

```csharp
/// <summary>
/// Unit test to verify that DataRows property returns the collection that was set.
/// </summary>
[TestMethod]
public void DataRows_SetValidValue_ReturnsSetValue()
{
    // Arrange (Given)
    var instance = new TestIoCsvFile(new TestCsvEntityMapper());
    var newDataRows = new List<TestDto>
    {
        new TestDto { Id = 1, Name = "Test1" },
        new TestDto { Id = 2, Name = "Test2" },
    };
    
    // Act (When)
    instance.DataRows = newDataRows;
    
    // Assert (Then)
    Assert.AreSame(
        newDataRows,
        instance.DataRows,
        "DataRows should return the same collection that was set.");
    Assert.HasCount(
        instance.DataRows,
        2,
        "DataRows should contain the expected number of items.");
}
```

### Exception Testing

**IMPORTANT**: Use try/catch blocks. `Assert.ThrowsException` does not exist in this MSTest version.

#### Synchronous Exception Testing

```csharp
/// <summary>
/// Unit test to verify that method throws exception when parameter is null.
/// </summary>
[TestMethod]
public void Method_NullParameter_ThrowsArgumentNullException()
{
    // Arrange (Given)
    object? nullParam = null;
    bool exceptionThrown = false;

    // Act (When)
    try
    {
        SomeMethod(nullParam!);
    }
    catch (ArgumentNullException)
    {
        exceptionThrown = true;
    }

    // Assert (Then)
    Assert.IsTrue(
        exceptionThrown,
        "Method should throw ArgumentNullException when parameter is null.");
}
```

#### Alternative with Parameter Validation

```csharp
[TestMethod]
public void Method_NullParameter_ThrowsArgumentNullException()
{
    // Arrange (Given)
    object? nullParam = null;
    ArgumentNullException? caughtException = null;

    // Act (When)
    try
    {
        SomeMethod(nullParam!);
    }
    catch (ArgumentNullException ex)
    {
        caughtException = ex;
    }

    // Assert (Then)
    Assert.IsNotNull(
        caughtException,
        "Method should throw ArgumentNullException when parameter is null.");
    Assert.AreEqual(
        "parameterName",
        caughtException.ParamName,
        "Exception should indicate correct parameter name.");
}
```

#### Async Exception Testing

```csharp
/// <summary>
/// Unit test to verify that async method throws exception when parameter is null.
/// </summary>
[TestMethod]
public async Task MethodAsync_NullParameter_ThrowsArgumentNullException()
{
    // Arrange (Given)
    object? nullParam = null;
    bool exceptionThrown = false;

    // Act (When)
    try
    {
        await SomeMethodAsync(nullParam!);
    }
    catch (ArgumentNullException)
    {
        exceptionThrown = true;
    }

    // Assert (Then)
    Assert.IsTrue(
        exceptionThrown,
        "MethodAsync should throw ArgumentNullException when parameter is null.");
}
```

### Record Type Testing

When creating new instances of records, use **object initializer syntax** (not `with` expressions):

#### Correct Approach

```csharp
[TestMethod]
public void CreateNewInstance_ModifyProperty_CreatesNewRecord()
{
    // Arrange (Given)
    var original = new MyRecord("Name", "Value");
    string newValue = "NewValue";

    // Act (When)
    var modified = new MyRecord
    {
        Name = original.Name,
        Value = newValue
    };

    // Assert (Then)
    Assert.AreEqual(
        original.Name,
        modified.Name,
        "Name should remain the same.");
    Assert.AreEqual(
        newValue,
        modified.Value,
        "Value should have the new value.");
}
```

#### Avoid This

```csharp
// Don't use 'with' expressions in tests
var modified = original with { Value = newValue };
```

### Test Class Structure Example

```csharp
namespace Roadbed.Test.Unit.SomeNamespace;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.SomeNamespace;
using System;

/// <summary>
/// Contains unit tests for verifying the behavior of the MyClass class.
/// </summary>
[TestClass]
public class MyClassTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that constructor initializes properties correctly.
    /// </summary>
    [TestMethod]
    public void Constructor_NoParameters_InitializesWithDefaultValues()
    {
        // Arrange (Given)

        // Act (When)
        var instance = new MyClass();

        // Assert (Then)
        Assert.IsNotNull(
            instance,
            "Instance should be created successfully.");
    }

    /// <summary>
    /// Unit test to verify that property can be set and retrieved.
    /// </summary>
    [TestMethod]
    public void Property_SetValidValue_ReturnsSetValue()
    {
        // Arrange (Given)
        var instance = new MyClass();
        string expectedValue = "test";

        // Act (When)
        instance.Property = expectedValue;

        // Assert (Then)
        Assert.AreEqual(
            expectedValue,
            instance.Property,
            "Property should return the value that was set.");
    }

    #endregion Public Methods
}
```

### Testing Key Principles

1. **One assertion concept per test** (but multiple Assert statements are OK if testing the same concept)
2. **Test behavior, not implementation**
3. **Tests should be independent** - no shared state between tests
4. **Clear, descriptive names** - the test name should explain what's being tested
5. **Comprehensive coverage** - test happy paths, edge cases, and error conditions
6. **Consistent formatting** - follow the patterns above exactly

### Framework Information

- **Test Framework**: MSTest
- **Assertion Library**: MSTest Assert class
- **.NET Version**: Modern .NET (supports nullable reference types, records, etc.)

---

## DTO and SDK Patterns

### JSON Serialization

**Always use Newtonsoft.Json**, not System.Text.Json:

```csharp
using Newtonsoft.Json;

[JsonProperty("propertyName")]
public string PropertyName { get; set; }
```

### Namespace Conventions

Remove `.Dtos` or `.Entities` suffixes from namespaces to avoid requiring additional using statements:

```csharp
/*
 * The namespace Roadbed.Sdk.NationalWeatherService.Dtos was removed on purpose 
 * and replaced with Roadbed.Sdk.NationalWeatherService so that no additional 
 * using statements are required.
 */
namespace Roadbed.Sdk.NationalWeatherService;

using Newtonsoft.Json;
using Roadbed.Crud;
```

### DTO Structure Standards

#### Root Response DTOs

```csharp
/// <summary>
/// [Description] from the National Weather Service.
/// </summary>
/// <remarks>
/// Detailed explanation of what this response represents.
/// </remarks>
public sealed record SomeResponse
    : BaseDataTransferObject<string>
{
    [JsonProperty("propertyName")]
    required public string PropertyName { get; set; }
}
```

#### Nested DTOs

```csharp
/// <summary>
/// [Description] from the National Weather Service.
/// </summary>
/// <remarks>
/// Detailed explanation.
/// </remarks>
public record NestedObject
{
    [JsonProperty("propertyName")]
    required public string PropertyName { get; set; }
}
```

### Collection Types

Use arrays, not IList</T>, for DTO collections:

```csharp
// ✅ Correct
[JsonProperty("features")]
required public ObservationStationFeature[] Features { get; set; }

// ❌ Avoid
[JsonProperty("features")]
required public IList<ObservationStationFeature> Features { get; set; }
```

**Rationale**: Arrays signal immutability intent for data received from APIs, have better performance, and match JSON array semantics directly.

### GeoJSON Coordinate Order

**CRITICAL**: GeoJSON uses [longitude, latitude] order (opposite of typical):

```csharp
/// <summary>
/// Gets or sets the coordinates [longitude, latitude].
/// </summary>
/// <remarks>
/// GeoJSON uses [longitude, latitude] order, which is opposite of typical latitude/longitude order.
/// Index 0 = Longitude, Index 1 = Latitude.
/// </remarks>
[JsonProperty("coordinates")]
required public double[] Coordinates { get; set; }

// Usage:
Longitude = coordinates[0];  // ✅ Correct
Latitude = coordinates[1];   // ✅ Correct
```

### Extracting URL Segments

When extracting the last segment from API URLs:

```csharp
// ✅ Recommended: Use Uri.Segments
Uri uri = new Uri("https://api.weather.gov/zones/county/NEC109");
string identifier = uri.Segments[^1];  // "NEC109"

// ✅ Alternative: Use Path.GetFileName
string identifier = Path.GetFileName(uri.LocalPath);  // "NEC109"
```

---

## Code Analysis Rules

### CA1513: ObjectDisposedException Helper

**Always resolve** CA1513 warnings by using the modern throw helper (.NET 7+):

```csharp
// ✅ Modern approach (preferred)
private void ThrowIfDisposed()
{
    ObjectDisposedException.ThrowIf(this._disposed, this);
}

// Or inline:
public string? Name
{
    get
    {
        ObjectDisposedException.ThrowIf(this._disposed, this);
        this.LoadEntity();
        return this._name;
    }
}

// ❌ Old approach (triggers CA1513)
private void ThrowIfDisposed()
{
    if (this._disposed)
    {
        throw new ObjectDisposedException(nameof(MyClass));
    }
}
```

---

## IDisposable Implementation

### Simple Pattern (Recommended for Managed Resources Only)

Use this pattern when you only have managed resources (like SemaphoreSlim):

```csharp
public sealed class MyClass : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private bool _disposed;

    public void Dispose()
    {
        if (this._disposed)
            return;
        
        this._semaphore?.Dispose();
        this._disposed = true;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(this._disposed, this);
    }

    public void SomeMethod()
    {
        this.ThrowIfDisposed();
        // ... method implementation
    }
}
```

### Key Points

1. **Mark classes as `sealed`** when implementing IDisposable
    
2. **Add `_disposed` field** to track disposal state
    
3. **Call `ThrowIfDisposed()`** in all public methods/properties
    
4. **Don't use finalizers** unless you have unmanaged resources
    
5. **Consumers should use `using` statements**:
    
    ```csharp
    using var instance = new MyClass();// ... use instance
    ```
    

---

## Namespace Conventions

### Standard Format

All production code files should include the namespace removal comment:

```csharp
/*
 * The namespace [Original.Namespace.Dtos] was removed on purpose and replaced 
 * with [Simplified.Namespace] so that no additional using statements are required.
 */
namespace Simplified.Namespace;

using System;
using Newtonsoft.Json;
```

### Examples

- `Roadbed.Net.Entities` → `Roadbed.Net`
- `Roadbed.Sdk.NationalWeatherService.Dtos` → `Roadbed.Sdk.NationalWeatherService`
- `Roadbed.Sdk.NationalWeatherService.Entities` → `Roadbed.Sdk.NationalWeatherService`

---

## General C# Preferences

### this. Keyword Usage

**CRITICAL**: Always use the `this.` keyword when accessing instance members (fields, properties, methods).

This applies to:

- Instance field access
- Instance property access (both get and set)
- Instance method calls
- Nested member access through properties

#### Examples

```csharp
// ✅ Correct - Always use this.
public class MyClass
{
    private readonly ILogger<MyClass> _logger;
    private string _name;
    
    public ILogger<MyClass> Logger => this._logger;
    
    public string Name
    {
        get => this._name;
        set => this._name = value;
    }
    
    public void DoWork()
    {
        this.Logger.LogWithCheck(LogLevel.Trace, "Starting work");
        this._logger.LogInformation("Processing {Name}", this._name);
        this.HelperMethod();
    }
    
    private void HelperMethod()
    {
        this._name = "Updated";
    }
}

// ❌ Wrong - Missing this.
public class MyClass
{
    private readonly ILogger<MyClass> _logger;
    private string _name;
    
    public ILogger<MyClass> Logger => _logger;  // Missing this.
    
    public void DoWork()
    {
        Logger.LogWithCheck(LogLevel.Trace, "Starting work");  // Missing this.
        _logger.LogInformation("Processing {Name}", _name);    // Missing this.
        HelperMethod();                                         // Missing this.
    }
}
```

#### When this. is Required

1. **All instance field access**: `this._fieldName`
2. **All instance property access**: `this.PropertyName`
3. **All instance method calls**: `this.MethodName()`
4. **Property-backed expressions**: `public Type Prop => this._field;`
5. **Inside property getters/setters**:
    
    ```csharp
    get => this._name;set => this._name = value;
    ```
    

#### Exceptions

The `this.` keyword is NOT required for:

- Static members
- Local variables
- Method parameters
- Constructor calls with `this()` or `base()`

```csharp
public MyClass(string name)  // 'name' is parameter, no this.
{
    string localVar = "test";  // 'localVar' is local, no this.
    this._name = name;         // Instance field requires this.
}
```

### Property and Immutability Patterns

Choose between readonly fields with properties vs init-only properties based on the class purpose and initialization needs.

#### Readonly Field with Property (For Classes with Behavior)

Use this pattern for domain classes, services, and classes with dependencies injected via constructor:

```csharp
// ✅ Recommended for classes with behavior/dependencies
public sealed class MonthlyBillingJob : IScheduledJob
{
    private readonly SchedulingSchedule _schedule;
    private readonly ILogger<MonthlyBillingJob> _logger;

    public SchedulingSchedule Schedule => this._schedule;

    public MonthlyBillingJob(
        SchedulingSchedule schedule,
        ILogger<MonthlyBillingJob> logger)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        ArgumentNullException.ThrowIfNull(logger);
        
        this._schedule = schedule;
        this._logger = logger;
    }

    public async Task ExecuteAsync(
        IJobExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        this._logger.LogInformation("Executing monthly billing job");
        // Implementation
    }
}
```

**Advantages:**

- Truly immutable (compiler-enforced single assignment)
- Supports constructor validation
- Better for dependency injection patterns
- Stronger immutability guarantees

**Use when:**

- Class has behavior (methods that use the fields)
- Dependencies are injected via constructor
- Input validation is required
- Working with services, repositories, or domain objects

#### Init-Only Property (For DTOs and Configuration)

Use this pattern for data transfer objects, configuration classes, and simple data containers:

```csharp
// ✅ Recommended for DTOs and configuration objects
/// <summary>
/// Configuration for a scheduled job.
/// </summary>
public sealed record JobConfiguration
{
    [JsonProperty("jobId")]
    required public string JobId { get; init; }
    
    [JsonProperty("jobName")]
    required public string JobName { get; init; }
    
    [JsonProperty("schedule")]
    required public SchedulingSchedule Schedule { get; init; }
    
    [JsonProperty("description")]
    public string? Description { get; init; }
    
    [JsonProperty("enabled")]
    public bool Enabled { get; init; } = true;
}
```

**Advantages:**

- Simpler syntax (no backing field needed)
- Supports object initializer syntax
- Works with `required` keyword for compiler-enforced initialization
- Better for serialization scenarios

**Use when:**

- Creating DTOs or data transfer records
- Creating configuration objects
- Class is primarily a data container
- Object initializer syntax is beneficial
- Serialization/deserialization is involved

#### Quick Decision Guide

|Scenario|Use Pattern|Example|
|---|---|---|
|Service with DI|Readonly field + property|`public class EmailService`|
|Domain entity with behavior|Readonly field + property|`public class Order`|
|Repository implementation|Readonly field + property|`public class UserRepository`|
|Job implementation|Readonly field + property|`public class MonthlyBillingJob`|
|DTO from API|Init-only property|`public record UserResponse`|
|Configuration class|Init-only property|`public record AppSettings`|
|Request/Response models|Init-only property|`public record CreateOrderRequest`|

### Input Validation

Always validate constructor parameters:

```csharp
public MyClass(string id)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(id);
    this.Id = id;
}
```

### Null Checking

Use modern null checking where appropriate:

```csharp
// ✅ For factory pattern
ArgumentNullException.ThrowIfNull(factory);

// ✅ For string validation
ArgumentException.ThrowIfNullOrWhiteSpace(value);

// ✅ For conditional checks
if (string.IsNullOrWhiteSpace(value))
    return;
```

### Async/Await Patterns

#### CancellationToken Parameter Placement

**CRITICAL**: `CancellationToken` parameters must always be the last parameter in async method signatures, after all required and optional parameters.

```csharp
// ✅ Correct - CancellationToken is last with default value
public static async Task<int> ExecuteAsync(
    DataExecutorRequest request,
    IDataConnectionFactory connectionFactory,
    ILogger? logger = null,
    CancellationToken cancellationToken = default)
{
    // Implementation
}

// ✅ Correct - Multiple optional parameters, CancellationToken still last
public async Task ProcessAsync(
    string id,
    int timeout = 30,
    bool validateInput = true,
    CancellationToken cancellationToken = default)
{
    // Implementation
}

// ❌ Wrong - CancellationToken not at end
public async Task ProcessAsync(
    string id,
    CancellationToken cancellationToken = default,
    ILogger? logger = null)
{
    // Implementation
}
```

**Parameter Ordering Rule:**

1. Required parameters (in logical order)
2. Optional parameters with defaults
3. `CancellationToken` with `= default` (always last)

**Rationale**: This follows Microsoft's .NET API design guidelines and matches patterns used throughout the BCL (Base Class Library).

#### HttpRequestMessage Retry Pattern

For retry patterns with `HttpRequestMessage`:

**CRITICAL**: `HttpRequestMessage` can only be sent once. Always create a new instance for each retry:

```csharp
// ✅ Correct: Create new message for each attempt
for (int attempt = 0; attempt <= maxAttempts; attempt++)
{
    using (HttpRequestMessage message = this.CreateHttpRequestMessage(request))
    {
        // Send request
    }
}

// ❌ Wrong: Reusing same message
using (HttpRequestMessage message = this.CreateHttpRequestMessage(request))
{
    for (int attempt = 0; attempt <= maxAttempts; attempt++)
    {
        // This will fail on second attempt!
    }
}
```

### Lazy Loading Pattern

For entities with lazy-loaded properties:

```csharp
public string? Name
{
    get
    {
        ObjectDisposedException.ThrowIf(this._disposed, this);
        this.LoadEntity();
        return this._name;
    }
    internal set => this._name = value;
}

private void LoadEntity()
{
    if (this._entityLoaded || string.IsNullOrWhiteSpace(this.Id))
        return;
    
    this._semaphore.Wait();
    try
    {
        if (this._entityLoaded)  // Double-check after lock
            return;
        
        // Load from repository
        this._entityLoaded = true;
    }
    finally
    {
        this._semaphore.Release();
    }
}
```

### Logging Patterns

#### Logging in Jobs

When creating scheduled jobs that inherit from `BaseSchedulingJob<T>`:

**CRITICAL**: Always use the logging extension methods from `BaseClassWithLogging` instead of calling `ILogger` methods directly. The base class methods check if logging is enabled before formatting messages, preventing unnecessary string allocation and formatting overhead.

```csharp
// ✅ Correct - Uses base class method that checks log level first
public sealed class MonthlyBillingJob : BaseSchedulingJob<MonthlyBillingJob>
{
    public override async Task ExecuteAsync(
        IJobExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        this.LogDebug("Starting job with parameter: {Param}", context.JobData["param"]);
        this.LogInformation("Processing monthly billing for {Month}", DateTime.Now.Month);
        
        try
        {
            // Job implementation
            this.LogInformation("Monthly billing completed successfully");
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Monthly billing failed");
            throw;
        }
    }
}

// ❌ Avoid - Always formats string even if debug logging is disabled
public sealed class MonthlyBillingJob : BaseSchedulingJob<MonthlyBillingJob>
{
    public override async Task ExecuteAsync(
        IJobExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        // This will allocate and format the string even if Debug level is disabled
        this.Logger.LogDebug("Starting job with parameter: {Param}", context.JobData["param"]);
    }
}
```

**Available Methods from BaseClassWithLogging:**

- `this.LogTrace(string message, params object[] args)`
- `this.LogDebug(string message, params object[] args)`
- `this.LogInformation(string message, params object[] args)`
- `this.LogWarning(string message, params object[] args)`
- `this.LogError(Exception exception, string message, params object[] args)`
- `this.LogCritical(Exception exception, string message, params object[] args)`

**Performance Impact:**

```csharp
// When Debug logging is disabled:

// ✅ Good - No string formatting occurs
this.LogDebug("Processing {Count} items with {Size} bytes", items.Count, totalSize);
// The method checks IsEnabled(LogLevel.Debug) and returns early

// ❌ Bad - String formatting always occurs
this.Logger.LogDebug("Processing {Count} items with {Size} bytes", items.Count, totalSize);
// String interpolation and parameter boxing happen before the log level check
```

**When to Use Each Approach:**

|Scenario|Use This|Rationale|
|---|---|---|
|Job classes inheriting from `BaseSchedulingJob<T>`|`this.LogDebug()`|Performance optimization built into base class|
|Classes inheriting from `BaseClassWithLogging`|`this.LogDebug()`|Consistent with base class pattern|
|Classes with injected `ILogger<T>` (no base class)|`this.Logger.LogDebug()`|Standard ILogger usage|
|Static methods or utilities|Direct `ILogger` parameter|No base class available|

---

## Development Environment

### Tools

- **IDE**: Visual Studio
- **Note-taking**: Obsidian vault for "second brain" in markdown format
- **Version Control**: Git

### Project Structure

- Test projects: `Roadbed.Test.Unit.*`
- SDK projects: `Roadbed.Sdk.*`
- Core libraries: `Roadbed.*`

---

## Additional Notes

### API Integration Best Practices

1. **Always handle pagination** in API responses
2. **Validate JSON structure** matches DTOs before processing
3. **Use retry patterns** with exponential backoff for resilience
4. **Log errors** appropriately for debugging
5. **Don't trust API URLs** - validate and sanitize before use

### Geospatial Data

When working with geographic coordinates:

- **Latitude range**: -90 to 90
- **Longitude range**: -180 to 180
- **GeoJSON order**: [longitude, latitude] (NOT [latitude, longitude])
- **Always validate** coordinate ranges in constructors/setters

---

## Quick Reference Checklist

### For DTOs:

- [ ] Namespace comment present
- [ ] Using Newtonsoft.Json
- [ ] Root inherits from `BaseDataTransferObject<string>`
- [ ] Arrays used (not IList)
- [ ] `required` keyword on non-nullable properties
- [ ] JsonProperty attributes on all properties
- [ ] Comprehensive XML documentation
- [ ] Init-only properties used (not readonly fields)

### For Classes with Behavior:

- [ ] Readonly fields with expression-bodied properties
- [ ] Constructor-based initialization with validation
- [ ] Dependencies injected via constructor
- [ ] Sealed when implementing IDisposable
- [ ] Uses base class logging methods (`this.LogDebug()`) instead of `this.Logger.LogDebug()` when inheriting from `BaseClassWithLogging`

### For Tests:

- [ ] Roy Osherove naming convention
- [ ] XML summary documentation
- [ ] AAA pattern with Given/When/Then comments
- [ ] Regions for organization
- [ ] Detailed assertion messages
- [ ] Try/catch for exception testing
- [ ] Specialized Assert methods used (StringAssert.Contains, CollectionAssert.Contains, etc.) instead of Assert.IsTrue with .Contains()

### For IDisposable:

- [ ] Class is sealed
- [ ] `_disposed` field present
- [ ] `Dispose()` method implemented
- [ ] `ThrowIfDisposed()` called in public members
- [ ] Uses `ObjectDisposedException.ThrowIf()` (.NET 7+)

### For Code Style:

- [ ] `this.` keyword used for all instance member access
- [ ] Input validation on all constructor parameters
- [ ] Modern null-checking patterns used
- [ ] Proper async/await patterns followed
- [ ] Correct property pattern chosen (readonly field for behavior classes, init-only for DTOs)

---

**Last Updated**: Based on conversation through January 2026