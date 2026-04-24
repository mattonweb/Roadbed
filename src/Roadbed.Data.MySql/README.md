# Roadbed.Data.MySql

MySQL-specific implementations for the Roadbed data access framework, including connection management and query execution with automatic retry logic.

Built on the [MySqlConnector](https://mysqlconnector.net/) ADO.NET driver, which supports `System.Transactions.Transaction` enlistment via `AutoEnlist=true` (the default). This allows code that uses `TransactionScope` to participate in distributed transactions with a single MySQL resource manager.

For the full type catalog, retry internals, and transaction patterns, see the [Architecture Document](/docs/architectural-design/architecture-roadbed-mysql.md).

## Installation

```bash
dotnet add package Roadbed.Data.MySql
```

## Key Classes

### MySqlConnectionFactory

Creates and manages MySQL database connections. Implements `IDataConnectionFactory` from Roadbed.Data.

```csharp
using Roadbed.Data;
using Roadbed.Data.MySql;

// Using the connection string template
var connectionString = new DataConnecionString(DataConnectionStringType.MySQL)
{
    ServerName = "localhost",
    DatabaseSource = "mydb",
    Username = "admin",
    Password = "secret",
    TimeoutInSeconds = 30,
};
var factory = new MySqlConnectionFactory(connectionString);

// Or using a custom connection string for advanced options
var connectionString = new DataConnecionString(
    DataConnectionStringType.MySQL,
    "Server=localhost;Port=3306;Database=mydb;User ID=admin;Password=secret;Connection Timeout=30;SslMode=Required;AutoEnlist=true");
var factory = new MySqlConnectionFactory(connectionString);

// Connections are returned already open. Always dispose with 'using'.
using var connection = await factory.CreateOpenConnectionAsync(cancellationToken);
```

### MySqlExecutor

Executes MySQL commands via Dapper with built-in retry logic for transient errors.

#### Available Methods

| Method                         | Returns               | Use For                                            |
| ------------------------------ | --------------------- | -------------------------------------------------- |
| `ExecuteAsync`                 | `int` (rows affected) | INSERT, UPDATE, DELETE, DDL                        |
| `QueryAsync<T>`                | `IEnumerable<T>`      | SELECT returning multiple rows                     |
| `QuerySingleOrDefaultAsync<T>` | `T?`                  | SELECT returning zero or one row                   |
| `ExecuteScalarAsync<T>`        | `T?`                  | SELECT returning a single value (COUNT, MAX, etc.) |

All methods share the same parameter signature:

```csharp
MySqlExecutor.MethodAsync(
    DataExecutorRequest request,
    IDataConnectionFactory connectionFactory,
    ILogger? logger = null,
    CancellationToken cancellationToken = default);
```

#### Transient Errors Handled Automatically

When retries are enabled (the default), these MySQL error numbers are retried:

| Category                                  | Numbers                                                             |
| ----------------------------------------- | ------------------------------------------------------------------- |
| Server connection / resource (`ER_*`)     | `1040`, `1042`, `1043`, `1077`, `1129`, `1158`, `1159`, `1160`, `1161`, `1184` |
| Lock / deadlock                           | `1205`, `1213`                                                      |
| Client-side connection (`CR_*`)           | `2002`, `2003`, `2006`, `2013`                                      |

See the [Architecture Document](/docs/architectural-design/architecture-roadbed-mysql.md) for the full list with descriptions.

## Distributed Transactions

MySqlConnector's `AutoEnlist=true` connection-string flag (default) auto-enlists each open connection in the ambient `System.Transactions.Transaction`. This provides reliable single-RM distributed-transaction semantics for code using `TransactionScope`:

```csharp
using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

await MySqlExecutor.ExecuteAsync(insertRequest, this._connectionFactory, this._logger, cancellationToken);
await MySqlExecutor.ExecuteAsync(updateRequest, this._connectionFactory, this._logger, cancellationToken);

scope.Complete();
```

`UseXaTransactions` (true XA two-phase commit across multiple resource managers) is **not** enabled by the template, since it requires `XA_RECOVER_ADMIN` on the MySQL server and is rarely available in shared-hosting environments. If you need true multi-RM XA, supply a custom connection string with `UseXaTransactions=true`.

## Requirements

- .NET 10.0+
- Roadbed.Data
- MySqlConnector
- Dapper

## Related Packages

- **Roadbed.Data** - Core data abstractions
- **Roadbed.Data.Postgresql** - PostgreSQL-specific implementations
- **Roadbed.Data.Sqlite** - SQLite-specific implementations
- **Roadbed.Data.Dapper** - Dapper configuration utilities
