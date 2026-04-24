# Roadbed.Data.MySql Architecture

Roadbed.Data.MySql provides the concrete MySQL implementation of the Roadbed.Data abstractions. It includes a connection factory and a Dapper-based query executor with built-in retry logic for transient MySQL errors.

It is built on the [MySqlConnector](https://mysqlconnector.net/) ADO.NET driver, which supports `System.Transactions.Transaction` enlistment via the `AutoEnlist=true` connection-string flag (the default). This is the primary reason MySqlConnector was chosen over `MySql.Data` — `MySql.Data` only supports regular database transactions and does not enlist in `TransactionScope`.

---

## For AI Assistants

This document is the authoritative reference for the Roadbed.Data.MySql NuGet package. When a developer asks you to create a repository or data access layer that uses MySQL, use this document together with the [Roadbed.Data Architecture](architecture-roadbed-data.md) to scaffold the correct factory, executor calls, and retry configuration.

**Key rules to follow:**

1. **Always use `this.`** when accessing instance members (fields, properties, methods).
2. **Use `ArgumentNullException.ThrowIfNull()`** for null validation.
3. **Use `ArgumentException.ThrowIfNullOrWhiteSpace()`** for string validation.
4. **Never inject `IDataConnectionFactory` directly** — create a database-specific marker interface (e.g., `IFooDatabaseFactory : IDataConnectionFactory`).
5. **Use `MySqlExecutor` static methods** for all query execution — they handle connection lifecycle, retry logic, and logging.
6. **Always pass `CancellationToken`** as the last parameter with `= default`.
7. **Connections are returned open** — callers must dispose them with `using`.
8. **Use `ConfigureAwait(false)`** in library code (the executor already does this).
9. **Use `DataExecutorRequest`** to configure queries — never build retry loops manually.
10. **Use MySQL-style parameter placeholders** (`@ParamName`) — Dapper handles the translation to MySqlConnector parameters.
11. **Use `TransactionScope`** for multi-statement atomicity. With `AutoEnlist=true` (the template default), each open connection automatically enlists in the ambient transaction.

---

## Table of Contents

1. [For AI Assistants](architecture-roadbed-mysql.md#for-ai-assistants)
2. [Type Catalog](architecture-roadbed-mysql.md#type-catalog)
3. [Package Relationship](architecture-roadbed-mysql.md#package-relationship)
4. [MySqlConnectionFactory](architecture-roadbed-mysql.md#mysqlconnectionfactory)
5. [MySqlExecutor](architecture-roadbed-mysql.md#mysqlexecutor)
    - [Public Methods](architecture-roadbed-mysql.md#public-methods)
    - [Method Signatures](architecture-roadbed-mysql.md#method-signatures)
    - [Parameter Ordering Convention](architecture-roadbed-mysql.md#parameter-ordering-convention)
    - [Retry Logic](architecture-roadbed-mysql.md#retry-logic)
    - [Transient Error Detection](architecture-roadbed-mysql.md#transient-error-detection)
    - [Logging Behavior](architecture-roadbed-mysql.md#logging-behavior)
    - [Usage Examples](architecture-roadbed-mysql.md#usage-examples)
6. [Distributed Transactions](architecture-roadbed-mysql.md#distributed-transactions)
7. [Implementation Walkthrough](architecture-roadbed-mysql.md#implementation-walkthrough)
8. [Common Pitfalls](architecture-roadbed-mysql.md#common-pitfalls)

---

## Type Catalog

Roadbed.Data.MySql contains **2 public types**.

| Type                     | Kind         | Purpose                                                                              |
| ------------------------ | ------------ | ------------------------------------------------------------------------------------ |
| `MySqlConnectionFactory` | Class        | Concrete `IDataConnectionFactory` that creates `MySqlConnector.MySqlConnection` instances |
| `MySqlExecutor`          | Static class | Dapper-based query execution with built-in retry logic for transient MySQL errors    |

---

## Package Relationship

```
┌──────────────────────────────────────────────────────────────────┐
│ Your Repository                                                  │
│                                                                  │
│   Uses: MySqlExecutor.QueryAsync<T>(request, factory, ...)       │
│   Uses: IFooDatabaseFactory (marker interface)                   │
└──────────┬───────────────────────────────────────────────────────┘
           │
┌──────────▼───────────────────────────────────────────────────────┐
│ Roadbed.Data.MySql                                               │
│                                                                  │
│   MySqlConnectionFactory  → creates MySqlConnection              │
│   MySqlExecutor           → Dapper + retry logic                 │
└──────────┬───────────────────────────────────────────────────────┘
           │ depends on
┌──────────▼───────────────────────────────────────────────────────┐
│ Roadbed.Data                                                     │
│                                                                  │
│   IDataConnectionFactory                                         │
│   DataConnecionString                                            │
│   DataExecutorRequest                                            │
└──────────┬───────────────────────────────────────────────────────┘
           │ depends on
┌──────────▼───────────────────────────────────────────────────────┐
│ External Dependencies                                            │
│                                                                  │
│   MySqlConnector                                                 │
│   Dapper                                                         │
│   Microsoft.Extensions.Logging                                   │
└──────────────────────────────────────────────────────────────────┘
```

---

## MySqlConnectionFactory

Creates and opens `MySqlConnection` instances. See the [Roadbed.Data Architecture](architecture-roadbed-data.md) for the full factory pattern (marker interface, DI registration, consuming examples).

```csharp
namespace Roadbed.Data.MySql;

public class MySqlConnectionFactory : IDataConnectionFactory
{
    public MySqlConnectionFactory(DataConnecionString connectionString);

    public DataConnecionString Connecion { get; init; }

    // Both return an already-open connection. Caller must dispose.
    public IDbConnection CreateOpenConnection();
    public async Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken);
}
```

**Key behaviors:**

- Validates `connectionString` is not null
- Creates `MySqlConnector.MySqlConnection` instances
- Opens the connection before returning
- Uses `ConfigureAwait(false)` in async path
- Throws `MySqlException` if the connection cannot be opened

### Connection String Configuration

MySQL connection strings can be configured using the template or a custom string:

#### Using the Template

```csharp
var connectionString = new DataConnecionString(DataConnectionStringType.MySQL)
{
    ServerName = "localhost",
    DatabaseSource = "mydb",
    Username = "admin",
    Password = "secret",
    TimeoutInSeconds = 30,
};

// Produces: Server=localhost;Database=mydb;User ID=admin;Password=secret;Connection Timeout=30;AutoEnlist=true;
```

The template sets `AutoEnlist=true` explicitly to signal intent, even though it is also the MySqlConnector default.

#### Using a Custom Connection String

```csharp
var connectionString = new DataConnecionString(
    DataConnectionStringType.MySQL,
    "Server=localhost;Port=3306;Database=mydb;User ID=admin;Password=secret;Connection Timeout=30;SslMode=Required;AutoEnlist=true");
```

Use the custom connection string approach when you need parameters not covered by the template, such as `Port`, `SslMode`, `Pooling`, `Maximum Pool Size`, or `UseXaTransactions`.

---

## MySqlExecutor

The primary API for executing SQL against a MySQL database. All methods are static, accept a `DataExecutorRequest` and an `IDataConnectionFactory`, and handle the full connection lifecycle (open, execute, dispose) and optional retry logic internally.

### Public Methods

| Method                         | Returns                     | Dapper Method                  | Use Case                                           |
| ------------------------------ | --------------------------- | ------------------------------ | -------------------------------------------------- |
| `ExecuteAsync`                 | `Task<int>` (rows affected) | `ExecuteAsync`                 | INSERT, UPDATE, DELETE, DDL                        |
| `QueryAsync<T>`                | `Task<IEnumerable<T>>`      | `QueryAsync<T>`                | SELECT returning multiple rows                     |
| `QuerySingleOrDefaultAsync<T>` | `Task<T?>`                  | `QuerySingleOrDefaultAsync<T>` | SELECT returning zero or one row                   |
| `ExecuteScalarAsync<T>`        | `Task<T?>`                  | `ExecuteScalarAsync<T>`        | SELECT returning a single value (COUNT, MAX, etc.) |

### Method Signatures

All four methods share the same parameter signature:

```csharp
public static async Task<TResult> MethodAsync<T>(
    DataExecutorRequest request,
    IDataConnectionFactory connectionFactory,
    ILogger? logger = null,
    CancellationToken cancellationToken = default);
```

| Parameter           | Required | Description                                     |
| ------------------- | -------- | ----------------------------------------------- |
| `request`           | Yes      | Query, parameters, and retry configuration      |
| `connectionFactory` | Yes      | Database-specific factory (marker interface)    |
| `logger`            | No       | Falls back to `NullLogger.Instance` if null     |
| `cancellationToken` | No       | Cancellation support (always last, `= default`) |

Both `request` and `connectionFactory` are validated with `ArgumentNullException.ThrowIfNull()`.

### Parameter Ordering Convention

The parameter order follows a consistent pattern across all four methods:

1. `DataExecutorRequest request` — the query and its configuration
2. `IDataConnectionFactory connectionFactory` — how to get a connection
3. `ILogger? logger = null` — optional diagnostics
4. `CancellationToken cancellationToken = default` — always last

### Retry Logic

When `request.RetriesEnabled` is `true` (the default), the executor catches transient `MySqlException` errors and retries automatically.

**Retry flow:**

```
Execute query
    │
    ├── Success → return result
    │
    ├── MySqlException (transient) AND attempts remaining
    │       │
    │       ├── Log warning with error number and delay
    │       ├── Wait (delay × attempt if multiplier enabled)
    │       └── Retry with new connection
    │
    ├── MySqlException (non-transient) → throw immediately
    │
    └── All retries exhausted
            │
            ├── Log error with last exception
            └── Throw InvalidOperationException (wraps last MySqlException)
```

**Key behaviors:**

- Each retry creates a **new connection** from the factory
- The delay is calculated as: `DelayBetweenRetries.TotalMilliseconds × attempt` (when `DelayMultiplierEnabled` is `true`)
- Non-transient errors are **not caught** — they propagate immediately
- After all retries are exhausted, throws `InvalidOperationException` with the last `MySqlException` as inner exception

**Default retry configuration (from `DataExecutorRequest`):**

| Property                 | Default |
| ------------------------ | ------- |
| `RetriesEnabled`         | `true`  |
| `MaxRetries`             | `3`     |
| `DelayBetweenRetries`    | `100ms` |
| `DelayMultiplierEnabled` | `true`  |

**Default backoff schedule:**

| Attempt   | Delay                             |
| --------- | --------------------------------- |
| 1         | 100ms                             |
| 2         | 200ms                             |
| 3         | 300ms                             |
| Exhausted | Throw `InvalidOperationException` |

### Transient Error Detection

The executor retries only specific MySQL error numbers that represent temporary conditions. The `MySqlException.Number` property exposes the underlying MySQL error code.

#### Server-side Connection / Resource Errors (`ER_*`)

| Number | Constant                      | Meaning                                          | Retry? |
| ------ | ----------------------------- | ------------------------------------------------ | ------ |
| `1040` | `ER_CON_COUNT_ERROR`          | Too many connections                             | Yes    |
| `1042` | `ER_BAD_HOST_ERROR`           | Cannot resolve hostname                          | Yes    |
| `1043` | `ER_HANDSHAKE_ERROR`          | Bad handshake                                    | Yes    |
| `1077` | `ER_NORMAL_SHUTDOWN`          | Server is shutting down                          | Yes    |
| `1129` | `ER_HOST_IS_BLOCKED`          | Host blocked due to many connection errors      | Yes    |
| `1158` | `ER_NET_READ_ERROR_FROM_PIPE` | Network read error from pipe                     | Yes    |
| `1159` | `ER_NET_READ_INTERRUPTED`     | Network read interrupted                         | Yes    |
| `1160` | `ER_NET_ERROR_ON_WRITE`       | Network error on write                           | Yes    |
| `1161` | `ER_NET_WRITE_INTERRUPTED`    | Network write interrupted                        | Yes    |
| `1184` | `ER_NEW_ABORTING_CONNECTION`  | Aborted connection                               | Yes    |

#### Lock / Deadlock

| Number | Constant               | Meaning                                                | Retry? |
| ------ | ---------------------- | ------------------------------------------------------ | ------ |
| `1205` | `ER_LOCK_WAIT_TIMEOUT` | Lock wait timeout exceeded                             | Yes    |
| `1213` | `ER_LOCK_DEADLOCK`     | Deadlock found, transaction was rolled back           | Yes    |

#### Client-side Connection Errors (`CR_*`)

| Number | Constant                | Meaning                                  | Retry? |
| ------ | ----------------------- | ---------------------------------------- | ------ |
| `2002` | `CR_CONNECTION_ERROR`   | Cannot connect through socket            | Yes    |
| `2003` | `CR_CONN_HOST_ERROR`    | Cannot connect to MySQL server           | Yes    |
| `2006` | `CR_SERVER_GONE_ERROR`  | Server has gone away                     | Yes    |
| `2013` | `CR_SERVER_LOST`        | Lost connection to server during query   | Yes    |

#### Non-Transient (Never Retried)

All MySQL error numbers not listed above are considered non-transient and propagate immediately. Common examples:

| Number | Constant                  | Meaning                |
| ------ | ------------------------- | ---------------------- |
| `1062` | `ER_DUP_ENTRY`            | Duplicate key          |
| `1452` | `ER_NO_REFERENCED_ROW_2`  | Foreign key violation  |
| `1146` | `ER_NO_SUCH_TABLE`        | Table does not exist   |
| `1064` | `ER_PARSE_ERROR`          | SQL syntax error       |

### Logging Behavior

The executor uses level-checked logging throughout:

| Event                     | Level   | Message Pattern                                                                              |
| ------------------------- | ------- | -------------------------------------------------------------------------------------------- |
| Query execution start     | Debug   | `"Executing command: {Query}"` (truncated to 200 chars)                                      |
| Transient error, retrying | Debug   | `"Transient error on attempt {Attempt}: {ErrorCode} - {Message}. Retrying in {DelayMs}ms..."` |
| Successful after retry    | Debug   | `"Command succeeded on attempt {Attempt}. Rows affected: {Rows}"`                            |
| All retries exhausted     | Error   | `"Command failed after {Attempts} attempts"` (includes exception)                            |

All log messages check `logger.IsEnabled()` before formatting to avoid unnecessary string allocation.

### Usage Examples

#### Non-Query (INSERT, UPDATE, DELETE)

```csharp
var request = new DataExecutorRequest(
    @"INSERT INTO foo (name, description) VALUES (@Name, @Description)")
{
    Parameters = new { Name = "Bar", Description = "A bar item" },
};

int rowsAffected = await MySqlExecutor.ExecuteAsync(
    request,
    this._connectionFactory,
    this._logger,
    cancellationToken);
```

#### Query Multiple Rows

```csharp
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
    RetriesEnabled = false,  // Read-only, consider disabling retries
};

IEnumerable<FooDto> results = await MySqlExecutor.QueryAsync<FooDto>(
    request,
    this._connectionFactory,
    this._logger,
    cancellationToken);
```

#### Query Single Row

```csharp
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
};

FooDto? result = await MySqlExecutor.QuerySingleOrDefaultAsync<FooDto>(
    request,
    this._connectionFactory,
    this._logger,
    cancellationToken);
```

#### Scalar Query

```csharp
var request = new DataExecutorRequest(
    @"SELECT COUNT(*) FROM foo AS f WHERE f.is_active = 1");

int? count = await MySqlExecutor.ExecuteScalarAsync<int>(
    request,
    this._connectionFactory,
    this._logger,
    cancellationToken);
```

#### Custom Retry Configuration

```csharp
var request = new DataExecutorRequest(
    @"UPDATE foo SET name = @Name WHERE id = @Id")
{
    Parameters = new { Id = id, Name = newName },
    MaxRetries = 5,
    DelayBetweenRetries = TimeSpan.FromMilliseconds(200),
    DelayMultiplierEnabled = true,  // 200ms, 400ms, 600ms, 800ms, 1000ms
};

int rowsAffected = await MySqlExecutor.ExecuteAsync(
    request,
    this._connectionFactory,
    this._logger,
    cancellationToken);
```

#### INSERT with LAST_INSERT_ID

MySQL does not support `RETURNING`. To get the auto-generated key, use `LAST_INSERT_ID()` in a follow-up SELECT (Dapper auto-collapses the multi-statement command):

```csharp
var request = new DataExecutorRequest(
    @"INSERT INTO foo (name, description)
      VALUES (@Name, @Description);
      SELECT
           id
          ,name
          ,description
      FROM
          foo
      WHERE
          id = LAST_INSERT_ID()
      ;")
{
    Parameters = new { entity.Name, entity.Description },
};

FooDto? result = await MySqlExecutor.QuerySingleOrDefaultAsync<FooDto>(
    request,
    this._connectionFactory,
    this._logger,
    cancellationToken);
```

#### UPSERT with ON DUPLICATE KEY UPDATE

MySQL supports `ON DUPLICATE KEY UPDATE` for atomic upsert operations:

```csharp
var request = new DataExecutorRequest(
    @"INSERT INTO foo (external_id, name, description)
      VALUES (@ExternalId, @Name, @Description)
      ON DUPLICATE KEY UPDATE
           name = VALUES(name)
          ,description = VALUES(description)
      ;")
{
    Parameters = new { entity.ExternalId, entity.Name, entity.Description },
};

await MySqlExecutor.ExecuteAsync(
    request,
    this._connectionFactory,
    this._logger,
    cancellationToken);
```

---

## Distributed Transactions

The Roadbed.Data.MySql template enables `AutoEnlist=true` so that any open connection joins the ambient `System.Transactions.Transaction`. This provides reliable single-resource-manager distributed-transaction semantics for code that uses `TransactionScope`:

```csharp
using var scope = new TransactionScope(
    TransactionScopeOption.Required,
    new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
    TransactionScopeAsyncFlowOption.Enabled);

await MySqlExecutor.ExecuteAsync(insertRequest, this._connectionFactory, this._logger, cancellationToken);
await MySqlExecutor.ExecuteAsync(updateRequest, this._connectionFactory, this._logger, cancellationToken);

scope.Complete();
```

**Important behaviors:**

- Each `MySqlExecutor` call opens a new connection. Because `AutoEnlist=true`, every connection enlists in the ambient `TransactionScope`.
- Use `TransactionScopeAsyncFlowOption.Enabled` so the ambient transaction flows across `await` boundaries.
- If `scope.Complete()` is not called before disposal, all enlisted operations roll back.
- Inside the scope, a `MySqlException` from a failed retry will dispose the scope without `Complete()`, rolling everything back.

### Why `AutoEnlist`, not `UseXaTransactions`

`UseXaTransactions=true` would add true XA two-phase commit semantics needed when MySQL participates with **other resource managers** (a second database, MSMQ, etc.) in the same `TransactionScope`. The template does not enable it because:

- Most application code only enlists a single MySQL connection per scope, where `AutoEnlist` is sufficient.
- XA recovery requires the `XA_RECOVER_ADMIN` privilege on the MySQL server, which is rarely granted in shared-hosting environments.
- XA holds row locks longer and adds operational complexity (orphaned prepared transactions need manual cleanup).

If you need true multi-resource-manager XA, supply a custom connection string with `UseXaTransactions=true`:

```csharp
var connectionString = new DataConnecionString(
    DataConnectionStringType.MySQL,
    "Server=...;Database=...;User ID=...;Password=...;AutoEnlist=true;UseXaTransactions=true");
```

### Why Not `MySql.Data`

The `MySql.Data` package (the official Oracle driver) does **not** support `System.Transactions.Transaction` enlistment — it only supports regular `IDbTransaction`-style transactions started via `IDbConnection.BeginTransaction()`. Code using `TransactionScope` will silently bypass MySQL operations performed through `MySql.Data`, leading to data integrity bugs that are easy to miss in development. MySqlConnector is the correct choice whenever distributed transactions matter.

---

## Implementation Walkthrough

This walkthrough shows how to create a repository that uses `MySqlExecutor` for all database operations.

### Step 1: Define the DTO

```csharp
namespace Foo.Sdk;

using Newtonsoft.Json;

public sealed record FooDto
{
    [JsonProperty("id")]
    public long Id { get; set; }

    [JsonProperty("name")]
    required public string Name { get; set; }

    [JsonProperty("description")]
    public string? Description { get; set; }
}
```

### Step 2: Define the Database Factory

See [Roadbed.Data Architecture](architecture-roadbed-data.md) for the full factory pattern. Summary:

```csharp
// Marker interface
public interface IFooDatabaseFactory : IDataConnectionFactory { }

// Implementation
public class FooDatabaseFactory(DataConnecionString connection)
    : MySqlConnectionFactory(connection), IFooDatabaseFactory { }
```

### Step 3: Implement the Repository

```csharp
namespace Foo.Sdk;

using Microsoft.Extensions.Logging;
using Roadbed;
using Roadbed.Data;
using Roadbed.Data.MySql;
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
              VALUES (@Name, @Description);
              SELECT
                   id
                  ,name
                  ,description
              FROM
                  foo
              WHERE
                  id = LAST_INSERT_ID()
              ;")
        {
            Parameters = new { entity.Name, entity.Description },
        };

        var result = await MySqlExecutor.QuerySingleOrDefaultAsync<FooDto>(
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

        return await MySqlExecutor.QuerySingleOrDefaultAsync<FooDto>(
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

        await MySqlExecutor.ExecuteAsync(
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

        await MySqlExecutor.ExecuteAsync(
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

        var results = await MySqlExecutor.QueryAsync<FooDto>(
            request,
            this._connectionFactory,
            this._logger,
            cancellationToken);

        return results.ToList();
    }
}
```

### Step 4: Register Services

```csharp
public class InstallFooModule : IServiceCollectionInstaller
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("FooDatabase")
            ?? throw new InvalidOperationException("FooDatabase connection string is required.");

        var dataConnectionString = new DataConnecionString(
            DataConnectionStringType.MySQL,
            connectionString);

        services.AddSingleton(dataConnectionString);
        services.AddSingleton<IFooDatabaseFactory, FooDatabaseFactory>();
        services.AddScoped<FooRepository>();
    }
}
```

---

## Common Pitfalls

### 1. Using `MySql.Data` Instead of `MySqlConnector`

```csharp
// ❌ Wrong — MySql.Data does not enlist in TransactionScope
// Operations inside `using var scope = new TransactionScope(...)` are
// not part of the ambient transaction. Roll back will silently miss them.

// ✅ Correct — Roadbed.Data.MySql uses MySqlConnector, which auto-enlists
```

### 2. Forgetting `TransactionScopeAsyncFlowOption.Enabled`

```csharp
// ❌ Wrong — ambient transaction does not flow across await
using var scope = new TransactionScope();
await MySqlExecutor.ExecuteAsync(request, factory, logger, cancellationToken);
scope.Complete();

// ✅ Correct — explicitly enable async flow
using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
await MySqlExecutor.ExecuteAsync(request, factory, logger, cancellationToken);
scope.Complete();
```

### 3. Building Retry Loops Manually

```csharp
// ❌ Wrong — duplicates what MySqlExecutor already does
for (int attempt = 0; attempt < 3; attempt++)
{
    try
    {
        using var conn = await this._connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await conn.QueryAsync<FooDto>(sql, parameters);
    }
    catch (MySqlException)
    {
        await Task.Delay(100 * attempt);
    }
}

// ✅ Correct — let MySqlExecutor handle it
var request = new DataExecutorRequest(sql)
{
    Parameters = parameters,
};

return await MySqlExecutor.QueryAsync<FooDto>(
    request,
    this._connectionFactory,
    this._logger,
    cancellationToken);
```

### 4. Using PostgreSQL Boolean Syntax

```csharp
// ❌ Wrong — MySQL's BOOLEAN is an alias for TINYINT, but the literals true/false work in newer versions.
// For maximum portability and clarity, use 1/0:
var request = new DataExecutorRequest(
    "SELECT f.id FROM foo AS f WHERE f.is_active = true");

// ✅ Correct — MySQL idiom uses 1/0 for boolean columns
var request = new DataExecutorRequest(
    "SELECT f.id FROM foo AS f WHERE f.is_active = 1");
```

### 5. Using `RETURNING` Clause

```csharp
// ❌ Wrong — MySQL does not support RETURNING (PostgreSQL syntax)
var request = new DataExecutorRequest(
    @"INSERT INTO foo (name) VALUES (@Name) RETURNING id, name");

// ✅ Correct — use multi-statement with LAST_INSERT_ID()
var request = new DataExecutorRequest(
    @"INSERT INTO foo (name) VALUES (@Name);
      SELECT id, name FROM foo WHERE id = LAST_INSERT_ID();");
```

### 6. Using PostgreSQL `ON CONFLICT` Instead of `ON DUPLICATE KEY UPDATE`

```csharp
// ❌ Wrong — ON CONFLICT is PostgreSQL syntax
var request = new DataExecutorRequest(
    @"INSERT INTO foo (id, name) VALUES (@Id, @Name)
      ON CONFLICT (id) DO UPDATE SET name = EXCLUDED.name");

// ✅ Correct — MySQL uses ON DUPLICATE KEY UPDATE with VALUES()
var request = new DataExecutorRequest(
    @"INSERT INTO foo (id, name) VALUES (@Id, @Name)
      ON DUPLICATE KEY UPDATE name = VALUES(name)");
```

### 7. Hardcoding Connection Strings

```csharp
// ❌ Wrong — hardcoded credentials in source code
var connectionString = new DataConnecionString(
    DataConnectionStringType.MySQL,
    "Server=prod-server;Database=mydb;User ID=admin;Password=secret");

// ✅ Correct — read from configuration
string connectionString = configuration.GetConnectionString("FooDatabase")
    ?? throw new InvalidOperationException("FooDatabase connection string is required.");

var dataConnectionString = new DataConnecionString(
    DataConnectionStringType.MySQL,
    connectionString);
```

---

## Differences from Roadbed.Data.Postgresql

When migrating from PostgreSQL to MySQL, be aware of these key differences:

| Feature                | PostgreSQL (`Roadbed.Data.Postgresql`)     | MySQL (`Roadbed.Data.MySql`)                    |
| ---------------------- | ------------------------------------------ | ----------------------------------------------- |
| Connection type        | `NpgsqlConnection`                         | `MySqlConnection`                               |
| Exception type         | `PostgresException`                        | `MySqlException`                                |
| Error codes            | SQLSTATE strings (`"08006"`, `"40001"`)    | Integer (`1213`, `1205`)                        |
| Error code property    | `ex.SqlState`                              | `ex.Number`                                     |
| Boolean type           | `BOOLEAN` (`true`/`false`)                 | `TINYINT(1)` (`1`/`0`)                          |
| Auto-increment         | `GENERATED ALWAYS AS IDENTITY` or `SERIAL` | `AUTO_INCREMENT`                                |
| Get inserted ID        | `RETURNING` clause                         | `LAST_INSERT_ID()` follow-up SELECT             |
| Upsert syntax          | `INSERT ... ON CONFLICT ... DO UPDATE`     | `INSERT ... ON DUPLICATE KEY UPDATE ...`        |
| Transient error count  | 16 SQLSTATE codes across 5 error classes   | 16 error numbers across 3 categories            |
| Connection string type | `DataConnectionStringType.PostgreSQL`      | `DataConnectionStringType.MySQL`                |
| TransactionScope flag  | `Enlist=true` (Npgsql default)             | `AutoEnlist=true` (MySqlConnector default)      |
| NuGet dependency       | `Npgsql`                                   | `MySqlConnector`                                |

---

## Quick Reference

### MySqlExecutor Method Selection

```
What are you doing?
    │
    ├── INSERT / UPDATE / DELETE / DDL → MySqlExecutor.ExecuteAsync()
    │                                     Returns: int (rows affected)
    │
    ├── INSERT then read inserted row  → MySqlExecutor.QuerySingleOrDefaultAsync<T>()
    │                                     (use multi-statement with LAST_INSERT_ID())
    │
    ├── SELECT multiple rows           → MySqlExecutor.QueryAsync<T>()
    │                                     Returns: IEnumerable<T>
    │
    ├── SELECT single row              → MySqlExecutor.QuerySingleOrDefaultAsync<T>()
    │                                     Returns: T? (null if not found)
    │
    └── SELECT single value            → MySqlExecutor.ExecuteScalarAsync<T>()
                                           Returns: T? (COUNT, MAX, etc.)
```

### Transient MySQL Error Numbers

| Category                          | Numbers                                                                        |
| --------------------------------- | ------------------------------------------------------------------------------ |
| Server connection / resource      | `1040`, `1042`, `1043`, `1077`, `1129`, `1158`, `1159`, `1160`, `1161`, `1184` |
| Lock / deadlock                   | `1205`, `1213`                                                                 |
| Client-side connection            | `2002`, `2003`, `2006`, `2013`                                                 |

### Integration Testing Checklist

Like PostgreSQL, MySQL has no in-memory mode. Integration tests require a running database instance.

- [ ] Use a dedicated test database (e.g., `testdb` or `foodb_test`)
- [ ] Configure connection string via environment variable or `appsettings.Test.json`
- [ ] Run schema migrations in `[TestInitialize]` or `[ClassInitialize]`
- [ ] Use `TransactionScope` with rollback for test isolation (relies on `AutoEnlist=true`)
- [ ] Consider Docker (`docker run -d -p 3306:3306 -e MYSQL_ROOT_PASSWORD=test mysql:8`) for CI/CD
- [ ] Consider GitHub Actions `services:` block for automated pipelines
