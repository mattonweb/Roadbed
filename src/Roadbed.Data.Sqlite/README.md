# Roadbed.Data.Sqlite

SQLite-specific implementations for the Roadbed data access framework, including connection management and query execution with automatic retry logic.

For the full type catalog, retry internals, and in-memory testing patterns, see the [Architecture Document](/docs/architectural-design/architecture-roadbed-sqlite.md).

## Installation

```bash
dotnet add package Roadbed.Data.Sqlite
```

## Key Classes

### SqliteConnectionFactory

Creates and manages SQLite database connections. Implements `IDataConnectionFactory` from Roadbed.Data.

```csharp
using Roadbed.Data;
using Roadbed.Data.Sqlite;

// File-based database
var connectionString = new DataConnecionString(DataConnectionStringType.Sqlite)
{
    DatabaseSource = @"C:\Data\foo.db",
};
var factory = new SqliteConnectionFactory(connectionString);

// Connections are returned already open. Always dispose with 'using'.
using var connection = await factory.CreateOpenConnectionAsync(cancellationToken);
```

### SqliteExecutor

Executes SQLite commands via Dapper with built-in retry logic for transient errors.

#### Available Methods

| Method                         | Returns               | Use For                                            |
| ------------------------------ | --------------------- | -------------------------------------------------- |
| `ExecuteAsync`                 | `int` (rows affected) | INSERT, UPDATE, DELETE, DDL                        |
| `QueryAsync<T>`                | `IEnumerable<T>`      | SELECT returning multiple rows                     |
| `QuerySingleOrDefaultAsync<T>` | `T?`                  | SELECT returning zero or one row                   |
| `ExecuteScalarAsync<T>`        | `T?`                  | SELECT returning a single value (COUNT, MAX, etc.) |

All methods share the same parameter signature:

```csharp
SqliteExecutor.MethodAsync(
    DataExecutorRequest request,
    IDataConnectionFactory connectionFactory,
    ILogger? logger = null,
    CancellationToken cancellationToken = default);
```

#### Transient Errors Handled Automatically

When retries are enabled (the default), these SQLite error codes are retried:

| Code | Constant        | Meaning            |
| ---- | --------------- | ------------------ |
| 5    | `SQLITE_BUSY`   | Database is locked |
| 6    | `SQLITE_LOCKED` | Table is locked    |
| 10   | `SQLITE_IOERR`  | Disk I/O error     |
| 13   | `SQLITE_FULL`   | Disk full          |

### SqliteConnectionExtensions

`KeepAlive()` extension method for in-memory database testing. Holds a connection open to prevent the in-memory database from being destroyed.

```csharp
var connection = (SqliteConnection)factory.CreateOpenConnection();
using var keepAlive = connection.KeepAlive();
// Database persists until keepAlive is disposed
```

See the [Architecture Document](/docs/architectural-design/architecture-roadbed-sqlite.md) for full testing patterns.

## Complete Repository Example

```csharp
using Microsoft.Extensions.Logging;
using Roadbed;
using Roadbed.Data;
using Roadbed.Data.Sqlite;
using Foo.Database;

internal sealed class FooRepository : BaseClassWithLogging
{
    private readonly IFooDatabaseFactory _connectionFactory;
    private readonly ILogger<FooRepository> _logger;

    public FooRepository(
        IFooDatabaseFactory connectionFactory,
        ILogger<FooRepository> logger)
        : base(logger)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        this._connectionFactory = connectionFactory;
        this._logger = logger;
    }

    public async Task<FooDto> CreateAsync(
        FooDto entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        this.LogDebug("Creating foo: {Name}", entity.Name);

        var request = new DataExecutorRequest(
            @"INSERT INTO foo (name, description)
              VALUES (@Name, @Description)
              RETURNING id, name, description")
        {
            Parameters = new { entity.Name, entity.Description },
        };

        var result = await SqliteExecutor.QuerySingleOrDefaultAsync<FooDto>(
            request,
            this._connectionFactory,
            this._logger,
            cancellationToken);

        return result!;
    }

    public async Task<FooDto?> ReadAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var request = new DataExecutorRequest(
            @"SELECT
                 f.id
                ,f.name
                ,f.description
             FROM
                 foo AS f
             WHERE
                 f.id = @Id
             ;")
        {
            Parameters = new { Id = id },
            RetriesEnabled = false,
        };

        return await SqliteExecutor.QuerySingleOrDefaultAsync<FooDto>(
            request,
            this._connectionFactory,
            this._logger,
            cancellationToken);
    }

    public async Task<FooDto> UpdateAsync(
        FooDto entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var request = new DataExecutorRequest(
            @"UPDATE foo
              SET
                   name = @Name
                  ,description = @Description
              WHERE
                  id = @Id
              ;")
        {
            Parameters = new { entity.Id, entity.Name, entity.Description },
        };

        await SqliteExecutor.ExecuteAsync(
            request,
            this._connectionFactory,
            this._logger,
            cancellationToken);

        return entity;
    }

    public async Task DeleteAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var request = new DataExecutorRequest(
            @"DELETE FROM foo WHERE id = @Id;")
        {
            Parameters = new { Id = id },
        };

        await SqliteExecutor.ExecuteAsync(
            request,
            this._connectionFactory,
            this._logger,
            cancellationToken);
    }

    public async Task<IList<FooDto>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var request = new DataExecutorRequest(
            @"SELECT
                 f.id
                ,f.name
                ,f.description
             FROM
                 foo AS f
             ORDER BY
                 f.name ASC
             ;")
        {
            RetriesEnabled = false,
        };

        var results = await SqliteExecutor.QueryAsync<FooDto>(
            request,
            this._connectionFactory,
            this._logger,
            cancellationToken);

        return results.ToList();
    }
}
```

## Requirements

- .NET 10.0+
- Roadbed.Data
- Microsoft.Data.Sqlite
- Dapper

## Related Packages

- **Roadbed.Data** - Core data abstractions
- **Roadbed.Data.Dapper** - Dapper configuration utilities

