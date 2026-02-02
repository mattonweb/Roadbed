# Roadbed.Data

Core data access abstractions and utilities for building database-agnostic data layers.

## Overview

This library provides foundational types for working with databases in a consistent, testable way. It includes connection string management, connection factory contracts, and query execution configuration with retry support.

For the full type catalog, connection string templates, and design rationale, see the [Architecture Document](/docs/architectural-design/architecture-roadbed-data.md).

## Installation

```bash
dotnet add package Roadbed.Data
```

## Key Classes

### IDataConnectionFactory

Interface for creating database connections. Each consuming application creates a database-specific marker interface that extends this, then injects the marker — never `IDataConnectionFactory` directly.

```csharp
public interface IDataConnectionFactory
{
    DataConnecionString Connecion { get; }

    IDbConnection CreateOpenConnection();

    Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken);
}
```

Both methods return an **already-open** connection. Callers are responsible for disposing it.

```csharp
// Define a marker interface for your database
public interface IFooDatabaseFactory : IDataConnectionFactory { }

// Inject the marker in your repository
public sealed class FooRepository : BaseClassWithLogging
{
    private readonly IFooDatabaseFactory _connectionFactory;

    public FooRepository(
        IFooDatabaseFactory connectionFactory,
        ILogger<FooRepository> logger)
        : base(logger)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        this._connectionFactory = connectionFactory;
    }

    public async Task<FooDto?> ReadAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        using var connection = await this._connectionFactory
            .CreateOpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<FooDto>(
            "SELECT f.id, f.name FROM foo AS f WHERE f.id = @Id",
            new { Id = id });
    }
}
```

See the [Architecture Document](https://claude.ai/chat/docs/architecture.md) for the full factory pattern walkthrough including DI registration and in-memory testing.

### DataConnecionString

Type-safe connection string builder supporting multiple database types.

```csharp
using Roadbed.Data;

// SQLite file database
var connectionString = new DataConnecionString(DataConnectionStringType.Sqlite)
{
    DatabaseSource = @"C:\Data\foo.db",
};

// SQLite in-memory database (shared cache)
var connectionString = new DataConnecionString(DataConnectionStringType.SqliteInMemory)
{
    DatabaseSource = "FooTestDb",
};

// Raw connection string (used as-is)
var connectionString = new DataConnecionString(
    DataConnectionStringType.Sqlite,
    "Data Source=/data/foo.db;Journal Mode=WAL;");
```

### DataExecutorRequest

Encapsulates a database command with parameters and retry configuration.

#### Basic Usage

```csharp
using Roadbed.Data;

// Query with parameters
var request = new DataExecutorRequest(
    "SELECT f.id, f.name FROM foo AS f WHERE f.id = @Id")
{
    Parameters = new { Id = 123 },
};

// Insert with parameters
var request = new DataExecutorRequest(
    "INSERT INTO foo (name) VALUES (@Name)")
{
    Parameters = new { Name = "Bar" },
};
```

#### With Retry Configuration

```csharp
// Custom retry settings
var request = new DataExecutorRequest(
    "INSERT INTO foo (name) VALUES (@Name)")
{
    Parameters = new { Name = "Bar" },
    MaxRetries = 5,
    DelayBetweenRetries = TimeSpan.FromMilliseconds(200),
    DelayMultiplierEnabled = true,  // 200ms, 400ms, 600ms, 800ms, 1000ms
};

// Disable retries
var request = new DataExecutorRequest(
    "SELECT COUNT(*) FROM foo")
{
    RetriesEnabled = false,
};
```

#### Properties

| Property                 | Type       | Default    | Description                             |
| ------------------------ | ---------- | ---------- | --------------------------------------- |
| `Query`                  | `string`   | (required) | SQL query or command to execute         |
| `Parameters`             | `object?`  | `null`     | Parameters for the query (Dapper-style) |
| `RetriesEnabled`         | `bool`     | `true`     | Enable automatic retry logic            |
| `MaxRetries`             | `int`      | `3`        | Maximum number of retry attempts        |
| `DelayBetweenRetries`    | `TimeSpan` | `100ms`    | Base delay between retry attempts       |
| `DelayMultiplierEnabled` | `bool`     | `true`     | Linear backoff (delay × attempt number) |

## Requirements

- .NET 10.0+
- System.Data.Common

## Related Packages

- **Roadbed.Data.Sqlite** - SQLite-specific implementations
- **Roadbed.Data.Dapper** - Dapper configuration utilities

