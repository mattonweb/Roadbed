# Roadbed.Logging Architecture

Roadbed.Logging is a self-contained library that persists `Microsoft.Extensions.Logging` (MEL) output to a relational database as structured rows, and tracks the **activities** — run instances of jobs, pipelines, and ad-hoc work — that those log rows tie back to. It is OpenTelemetry-first: logging flows through the OTel logging pipeline and a custom batching exporter, so DB persistence is one exporter and OTLP export (Grafana / Tempo / Jaeger / etc.) is a configuration add, not a rewrite.

Roadbed.Logging is deliberately a **peer** of Roadbed.Crud, not a consumer of it. The high-volume log write is an internal custom bulk insert that stamps each row with its own originating `activity_id` — distinct from the CRUDALBT "B" tier's uniform-`activityId` stamping appropriate to Bronze/Silver loads.

---

## For AI Assistants

This document is the authoritative reference for the Roadbed.Logging NuGet package. When a developer asks you to add structured log persistence, run/activity tracking, or lineage capture to a .NET application, use this document to wire DI, open activities, and shape the schema.

**Key rules to follow:**

1. **Always use `this.`** when accessing instance members (fields, properties, methods).
2. **Use `ArgumentNullException.ThrowIfNull()`** for null validation.
3. **Use `ArgumentException.ThrowIfNullOrWhiteSpace()`** for string validation.
4. **The caller mints the ULID.** Roadbed.Logging does not generate identifiers. Use the same ULID library you use to stamp `IAsyncBulkInsertOperation.BulkInsertAsync` calls during the run.
5. **Register `LoggingOptions` and `ILoggingDatabaseFactory` as singletons** in DI **before** `services.InstallModulesInAppDomain(configuration)` runs. The installer resolves both up-front and throws if either is missing.
6. **Call `builder.Logging.AddRoadbedDbLogging()`** on the host's `ILoggingBuilder` to wire the OpenTelemetry MEL provider, the batch processor, and the database exporter. The `IServiceCollectionInstaller` does not see `ILoggingBuilder`, so this step lives outside `InstallLogging`.
7. **Disposing `LoggingActivityScope` does not record a terminal status.** Always call `CompleteAsync(...)` or `FailAsync(...)` explicitly — the dispose path has no way to choose between Succeeded, Canceled, and Skipped.
8. **`CancellationToken` is always the last parameter** with `= default` on async methods.
9. **Use structured log templates** (`logger.LogInformation("Loaded {RowCount}", count)`). The exporter splits the template (stored as `message_template`) from the named args (stored as `properties` JSON) for downstream aggregation. Interpolated `$"..."` strings produce a single rendered message with no template.
10. **Roadbed.Logging never reads `IConfiguration` directly.** The host populates `LoggingOptions` from whatever source it likes; the library only sees the POCO.
11. **Roadbed.Logging never references `Roadbed.Crud`.** Do not add the dependency.
12. **MySQL/MariaDB and SQLite are the only supported `DataConnectionStringType` values in v1.** Postgres, SQL Server, etc. cause the installer to throw.

---

## Table of Contents

1. [For AI Assistants](architecture-roadbed-logging.md#for-ai-assistants)
2. [Type Catalog](architecture-roadbed-logging.md#type-catalog)
3. [Package Relationship](architecture-roadbed-logging.md#package-relationship)
4. [Namespace Convention](architecture-roadbed-logging.md#namespace-convention)
5. [Two Write Paths](architecture-roadbed-logging.md#two-write-paths)
6. [Entities and Schema](architecture-roadbed-logging.md#entities-and-schema)
    - [activity](architecture-roadbed-logging.md#activity)
    - [activity_input](architecture-roadbed-logging.md#activity_input)
    - [log_entries](architecture-roadbed-logging.md#log_entries)
7. [LoggingActivityService](architecture-roadbed-logging.md#loggingactivityservice)
    - [BeginAsync](architecture-roadbed-logging.md#beginasync)
    - [HeartbeatAsync](architecture-roadbed-logging.md#heartbeatasync)
    - [UpdateAsync](architecture-roadbed-logging.md#updateasync)
    - [CompleteAsync and FailAsync](architecture-roadbed-logging.md#completeasync-and-failasync)
    - [AddInputAsync](architecture-roadbed-logging.md#addinputasync)
    - [LoggingActivityScope](architecture-roadbed-logging.md#loggingactivityscope)
8. [The Log Write Pipeline](architecture-roadbed-logging.md#the-log-write-pipeline)
    - [RoadbedDbLogRecordExporter](architecture-roadbed-logging.md#roadbeddblogrecordexporter)
    - [LoggingChannel](architecture-roadbed-logging.md#loggingchannel)
    - [LogWriterHostedService](architecture-roadbed-logging.md#logwriterhostedservice)
    - [Recursion Safety](architecture-roadbed-logging.md#recursion-safety)
9. [Module Auto-Discovery and Wiring](architecture-roadbed-logging.md#module-auto-discovery-and-wiring)
    - [Multi-Container Channel Sharing](architecture-roadbed-logging.md#multi-container-channel-sharing)
    - [Build-Order Robustness](architecture-roadbed-logging.md#build-order-robustness)
10. [Schema Installation](architecture-roadbed-logging.md#schema-installation)
11. [Implementation Walkthrough](architecture-roadbed-logging.md#implementation-walkthrough)
12. [Common Pitfalls](architecture-roadbed-logging.md#common-pitfalls)
13. [Troubleshooting](architecture-roadbed-logging.md#troubleshooting)
14. [Quick Reference](architecture-roadbed-logging.md#quick-reference)

---

## Type Catalog

Roadbed.Logging contains **17 public types** organized into seven groups.

### Entities (3 types)

| Type                   | Kind         | Purpose                                                                  |
| ---------------------- | ------------ | ------------------------------------------------------------------------ |
| `LoggingActivity`      | Sealed class | One row in `activity`. Mutable — patched as the run progresses.          |
| `LoggingActivityInput` | Sealed class | One edge in `activity_input` (the lineage DAG).                          |
| `LoggingLogEntry`      | Sealed class | One row in `log_entries`. Append-only; populated by the exporter.        |

### Enumerators (3 types)

| Type                          | Kind | Purpose                                                                          |
| ----------------------------- | ---- | -------------------------------------------------------------------------------- |
| `LoggingActivityStatus`       | Enum | `Pending`, `Running`, `Succeeded`, `Failed`, `Canceled`, `Skipped`.              |
| `LoggingActivityType`         | Enum | `Unknown`, `Ingestion`, `Transformation`, `Promotion`, `Maintenance`, `Manual`, `Custom`. |
| `LoggingChannelFullPolicy`    | Enum | `DropOldest` (default) or `BlockBriefly` for backpressure-on-overflow.            |

### Public DTOs (3 types)

| Type                            | Kind         | Purpose                                                                       |
| ------------------------------- | ------------ | ----------------------------------------------------------------------------- |
| `LoggingOptions`                | Sealed class | Host-supplied POCO: schema, application, batch tuning, recursion guard list.  |
| `LoggingActivityBeginRequest`   | Sealed class | Initial values for a new activity row. Carries the caller-supplied ULID.      |
| `LoggingActivityUpdateRequest`  | Sealed class | Patch payload for non-null current-state columns (target, metrics, Quartz). |

### Marker Interface (1 type)

| Type                       | Kind      | Purpose                                                                       |
| -------------------------- | --------- | ----------------------------------------------------------------------------- |
| `ILoggingDatabaseFactory`  | Interface | Extends `IDataConnectionFactory`. Marker so DI can locate the logging schema. |

### Service Surface (2 types)

| Type                       | Kind                | Purpose                                                                       |
| -------------------------- | ------------------- | ----------------------------------------------------------------------------- |
| `LoggingActivityService`   | Public sealed class | Activity lifecycle API. Dual ctor (public takes `ILogger<T>`; internal takes deps for tests). |
| `LoggingActivityScope`     | Public sealed class | `IDisposable` bundling the diagnostic `Activity` + MEL scope frame.           |

### OTel Pipeline (3 types — `internal`)

| Type                              | Kind         | Purpose                                                                          |
| --------------------------------- | ------------ | -------------------------------------------------------------------------------- |
| `RoadbedDbLogRecordExporter`      | Sealed class | `BaseExporter<LogRecord>`. Maps records, enqueues onto the channel.              |
| `LoggingChannel`                  | Sealed class | Bounded `Channel<LoggingLogEntry>` shared by exporter (producer) and writer (consumer). |
| `LogWriterHostedService`          | Sealed class | `BackgroundService`. Drains, batches, flushes, falls back to `Console.Error`.    |

### Wiring (2 types)

| Type                            | Kind                 | Purpose                                                                              |
| ------------------------------- | -------------------- | ------------------------------------------------------------------------------------ |
| `InstallLogging`                | Class                | `IServiceCollectionInstaller`. Wires repositories, service, channel, hosted writer.  |
| `LoggingBuilderExtensions`      | Public static class  | Exposes `builder.Logging.AddRoadbedDbLogging()` for OTel wire-up.                     |

The repository contracts (`ILoggingActivityRepository`, `ILoggingActivityInputRepository`, `ILoggingLogEntryRepository`) and implementations are `internal` to the library; they are not part of the public surface and are split into `RepositoryInterfaces/` and `Repositories/` folders.

---

## Package Relationship

```
                       ┌───────────────────────┐
                       │  Consuming Application │
                       └───────────┬───────────┘
                                   │
                  ┌────────────────┴────────────────┐
                  │                                  │
                  ▼                                  ▼
       ┌─────────────────────┐           ┌────────────────────────┐
       │   Roadbed.Logging   │           │     Roadbed.Scheduling │
       │  (this package)     │           │   (optional — emits    │
       └─────────┬───────────┘           │    BeginAsync calls)   │
                 │                       └────────────────────────┘
   ┌─────────────┼────────────────┐
   │             │                │
   ▼             ▼                ▼
┌──────┐   ┌──────────┐   ┌────────────┐
│Common│   │Data      │   │Data.MySql / │
│      │   │+ Dapper  │   │ Data.Sqlite │
└──────┘   └──────────┘   └────────────┘
              │
              ▼
        OpenTelemetry
        + OTel.Hosting
```

Roadbed.Logging depends on `Roadbed.Common`, `Roadbed.Data`, `Roadbed.Data.Dapper`, `Roadbed.Data.MySql`, and `Roadbed.Data.Sqlite`. It does **not** depend on `Roadbed.Crud`. Roadbed.Scheduling does not depend on Roadbed.Logging; consuming apps that want their Quartz jobs to open activities call `LoggingActivityService.BeginAsync` from their job's `ExecuteAsync` body.

---

## Namespace Convention

All public types live in the top-level `Roadbed.Logging` namespace. The installer lives in `Roadbed.Logging.Installers`. Repository contracts and implementations are `internal` and also live in `Roadbed.Logging`.

The entity class is named `LoggingActivity` rather than `Activity` to avoid colliding with `System.Diagnostics.Activity`, which the library uses heavily. The table is still called `activity` and its primary key column is still `id` — the C# rename is for code clarity only.

---

## Two Write Paths

```
Application code → ILogger (MEL)
        │
        ├─(logs)→ OpenTelemetryLoggerProvider
        │             → BatchLogRecordProcessor              (OTel)
        │             → RoadbedDbLogRecordExporter           (maps LogRecord → LoggingLogEntry)
        │             → LoggingChannel<LoggingLogEntry>      (bounded, non-blocking)
        │             → LogWriterHostedService               (drains, batches, custom bulk insert)
        │             → log_entries                          (per-row activity_id)
        │
        └─(activities)→ LoggingActivityService               (public surface)
                          → ILoggingActivityRepository       (insert + patch + complete)
                          → ILoggingActivityInputRepository  (insert lineage edges)
                          → activity, activity_input         (mutable; small volume)
```

Two distinct paths, deliberately:

- **Activities** are low-volume and **mutable** (insert `running` → heartbeat → curated patches → terminal status). Custom single-row INSERT + UPDATE via `Roadbed.Data`.
- **Log entries** are high-volume and **append-only**. Buffered through a channel and **bulk-inserted in batches** off the hot path by an internal custom writer.

---

## Entities and Schema

The DDL is shipped as install scripts under [src/Roadbed.Logging/Assets/Tables/](../../src/Roadbed.Logging/Assets/Tables/). Each table has both an `install_mysql.txt` and an `install_sqlite.txt`. The scripts create tables **unqualified** — run them after selecting the target database (`USE logging;` on MySQL, or against the right `.db` file on SQLite). Set `LoggingOptions.Schema` in the host to match the database name so every C# repository statement qualifies tables consistently (`logging.activity`, etc.).

### Partitioning and PK rules

All three MySQL tables are RANGE-partitioned monthly. The partitioning column drives every other schema decision:

- **`activity` and `activity_input` partition on `created_on`.** PK is `(id, created_on)` and `(activity_id, input_activity_id, created_on)` respectively. There is no standalone `UNIQUE (id)` on `activity` — MySQL requires every unique key on a partitioned InnoDB table to contain the partition column, and uniqueness of `activity.id` is guaranteed by the caller's ULID instead of by a DB constraint.
- **`log_entries` partitions on `event_time_utc`.** PK is `(id, event_time_utc)`. Same composite-key rule.
- **Partitioned InnoDB tables cannot have foreign keys.** The lineage references in `activity_input` are soft on purpose.
- **120 monthly partitions are pre-created** (2026-01 .. 2035-12) plus `p_min` (catches anything pre-2026) and `pmax` (MAXVALUE catch-all). This avoids the "forward-rollover" half of partition maintenance until late 2035 — only retention drops are needed.
- **Partition pruning requires filtering on the partition column.** Every composite index leads with the fleet filter (`application`, `activity_id`, etc.) and ends with `created_on` / `event_time_utc` so MCP / analytical queries get pruning for free when they filter on date.

### UTC contract

All stored timestamps are UTC. The framework enforces it on three layers:

1. **`created_on` / `recorded_on` defaults** are `(UTC_TIMESTAMP(6))` (MySQL) / `strftime('%Y-%m-%dT%H:%M:%fZ','now')` (SQLite) — connection-time-zone-independent. INSERTs that omit these columns still land in UTC.
2. **`LoggingActivityService.BeginAsync` stamps `created_on` and `last_modified_on` explicitly** with `DateTime.UtcNow` rather than relying on the DEFAULT, so `LoggingActivityScope.CreatedOn` returns the exact same value that landed in the row. Subsequent UPDATE WHEREs match on it for partition pruning.
3. **Every framework UPDATE passes `@LastModifiedOn = DateTime.UtcNow` explicitly.** The DDL's `ON UPDATE CURRENT_TIMESTAMP(6)` clause remains as a safety net for non-framework writers, but the explicit parameter takes precedence and is connection-tz-independent.

### activity

One row per run. Mutable.

- **`id`** — `CHAR(26) ascii_bin` (MySQL) / `TEXT COLLATE BINARY` (SQLite). Caller-supplied ULID. `ascii_bin` collation guarantees lexical order matches chronological order.
- **`parent_activity_id`**, **`root_activity_id`** — soft references for run hierarchy. Roots have `root_activity_id == id`.
- **`trace_id`**, **`span_id`** — captured from `Activity.Current` at `BeginAsync` time. W3C 32-hex and 16-hex respectively.
- **`activity_key`** — logical definition slug (e.g. `"Foo.Ingestion.FullRefresh"`). Groups multiple runs of the same job.
- **`application`**, **`environment`**, **`host`**, **`process_id`** — provenance.
- **`activity_type`** — `ingestion` / `transformation` / `promotion` / `maintenance` / `manual` / custom.
- **`target`** — what the run acted on (`schema.table`, dataset name).
- **`status`** — `pending` / `running` / `succeeded` / `failed` / `canceled` / `skipped`.
- **`started_on`**, **`completed_on`**, **`last_heartbeat_on`** — UTC `DATETIME(6)`. A stale heartbeat combined with `status = 'running'` indicates a crashed or zombie process.
- **`records_impacted`** — headline count. Typically the sum of `BulkInsertAsync` returns during the run.
- **`parameters`**, **`metrics`** — `JSON` (MySQL) / `TEXT` (SQLite); structured input config and structured output metrics respectively.
- **`error`**, **`error_type`** — populated by `FailAsync(exception)`.
- **Quartz block** — `scheduler_instance_id`, `fire_instance_id`, `quartz_job_name`, `quartz_job_group`, `quartz_trigger_name`, `quartz_trigger_group`. Snapshotted at `BeginAsync` time (or patched later via `UpdateAsync`) because `QRTZ_FIRED_TRIGGERS` is transient and is cleared once the trigger completes.
- **`created_on`** — the partition / retention key. Stamped explicitly by `BeginAsync` as `DateTime.UtcNow`. Defaults to `UTC_TIMESTAMP(6)` (MySQL) / `strftime` UTC (SQLite) if a non-framework writer omits the column.
- **`last_modified_on`** — set explicitly by every framework UPDATE to `DateTime.UtcNow`; the `ON UPDATE CURRENT_TIMESTAMP(6)` clause on MySQL is a safety net only.

Composite indexes lead with the fleet filter and end with the partition column so MCP / analytical queries that filter on date prune naturally:

- `idx_activity_app_created (application, created_on)`
- `idx_activity_app_status_created (application, status, created_on)`
- `idx_activity_key_created (activity_key, created_on)`
- `idx_activity_status_created (status, created_on)`
- `idx_activity_type_created (activity_type, created_on)`
- Single-column lookups for parent / root / trace / fire-instance.

### activity_input

The lineage DAG: "this activity consumed the output of those upstream activities."

- Composite primary key `(activity_id, input_activity_id, created_on)` — MySQL needs the partition column in the PK.
- `input_role` — optional free-form label (`"places"`, `"cousubs"`, `"hud-centroid"`).
- One reverse index on `input_activity_id` for impact analysis (which downstream activities consumed *this* upstream output?).
- Duplicate edges are silently coalesced — the repository emits `INSERT ... ON DUPLICATE KEY UPDATE` (MySQL) or `INSERT OR IGNORE` (SQLite).

### log_entries

The high-volume append-only log store.

- **Composite primary key** `(id, event_time_utc)`. MySQL requires the partition key in every unique key.
- **MySQL: RANGE-partitioned monthly** on `TO_DAYS(event_time_utc)`. The install script pre-creates 120 monthly partitions; only retention drops are needed (no forward-rollover until 2035).
- **SQLite: no partitioning.** Retention is a scheduled `DELETE FROM log_entries WHERE event_time_utc < datetime('now', '-90 days')` followed by `VACUUM`.
- **`activity_id`** is **per row**, sampled at log time — not a uniform stamp for the whole batch. This is the core distinction from the CRUDALBT "B" tier.
- **`message_template`** is the unrendered template (the `{OriginalFormat}` attribute on the `LogRecord`); **`properties`** is JSON of the named args. Keeping these separate lets analysts aggregate log events by template across many argument values.
- **Indexes** are composite, leading with the fleet filter and ending with `event_time_utc`: `idx_log_activity (activity_id, event_time_utc)`, `idx_log_app_time (application, event_time_utc)`, `idx_log_app_level_time (application, log_level, event_time_utc)`, `idx_log_level_time (log_level, event_time_utc)`, plus single-column `idx_log_trace` and `idx_log_time`.

---

## LoggingActivityService

`LoggingActivityService` is the only public service surface in the library. It uses the standard Roadbed dual-constructor pattern — the public constructor takes only `ILogger<LoggingActivityService>` and resolves repositories via `ServiceLocator`; the `internal` constructor takes the repositories directly for unit tests via `InternalsVisibleTo`.

### BeginAsync

```csharp
Task<LoggingActivityScope> BeginAsync(
    LoggingActivityBeginRequest request,
    CancellationToken cancellationToken = default);
```

`BeginAsync` does five things in order:

1. **Starts a `System.Diagnostics.Activity`** named `"roadbed.logging.activity"`. The default `ActivityIdFormat` is W3C, so the new Activity gets a 32-hex `TraceId` and a 16-hex `SpanId` automatically. The Activity is tagged with `roadbed.activity_id = {requestId}` so consumers can read it out of `Activity.Current` if they prefer that to the MEL scope.
2. **Pushes a MEL `BeginScope`** carrying `("activity_id", requestId)`. Subsequent `ILogger.Log*` calls inside the scope inherit this key, and the OTel pipeline propagates it into the `LogRecord` scope chain.
3. **Constructs a `LoggingActivity` entity** with `Status = Running`, `StartedOn = LastHeartbeatOn = UtcNow`, `Host` and `ProcessId` snapshotted from `Environment.MachineName` / `Environment.ProcessId`, and `TraceId` / `SpanId` lifted from the freshly-started Activity. Defaults `RootActivityId` to `request.Id` when the caller omits it.
4. **Calls `ILoggingActivityRepository.InsertAsync`** to persist the row.
5. **Returns a `LoggingActivityScope`** wrapping the Activity and the MEL scope handle.

If the repository INSERT throws, the service disposes the MEL scope and stops the Activity before propagating, so the caller is never left with an ambient `activity_id` pointing at a row that does not exist.

### HeartbeatAsync

Stamps `UtcNow` into `last_heartbeat_on` (and `last_modified_on`) via a one-row UPDATE. Two overloads:

```csharp
Task HeartbeatAsync(LoggingActivityScope scope, CancellationToken ct = default);
Task HeartbeatAsync(string activityId, CancellationToken ct = default);
```

The **scope-aware overload is preferred** when the caller still holds the scope. It passes `scope.CreatedOn` into the repository, which adds `AND created_on = @CreatedOn` to the UPDATE's WHERE clause. MySQL then prunes the UPDATE to the single monthly partition that owns the row instead of probing all 120.

The **id-only overload is kept for legacy / external callers** — for example, a watchdog process that found a stale heartbeat and only has the activity id. On MySQL it probes every partition.

Long-running steps should call `Heartbeat` every few seconds — every iteration of a batch loop is a reasonable cadence — so that a stale heartbeat with `status = 'running'` becomes a reliable signal of a crashed process.

### UpdateAsync

```csharp
Task UpdateAsync(
    LoggingActivityUpdateRequest request,
    CancellationToken cancellationToken = default);
```

Patches only the **non-null** properties on the request onto the existing row via a `COALESCE`-driven UPDATE. Properties left at `null` preserve their existing values. Setting `LoggingActivityUpdateRequest.CreatedOn = scope.CreatedOn` enables the same partition-pruning AND clause described above; leaving it null falls back to id-only WHERE.

This is the right surface for "current state" mid-run updates — fields that may not be known at Begin time or that evolve through the run:

- `Target` — sometimes discovered after the run starts.
- `Parameters`, `Metrics` — JSON blobs that grow as work progresses.
- `RecordsImpacted` — running total.
- The Quartz block — for callers that didn't have it at Begin time, or for jobs that re-key partway through.

`UpdateAsync` deliberately excludes `Status`, `StartedOn`, `CompletedOn`, and `LastHeartbeatOn`, which have dedicated methods. Every UPDATE also stamps `last_modified_on = DateTime.UtcNow` explicitly, overriding the DDL's `ON UPDATE CURRENT_TIMESTAMP(6)` safety net.

### CompleteAsync and FailAsync

```csharp
// Preferred: scope-aware overloads (prune to one partition on MySQL).
Task CompleteAsync(LoggingActivityScope scope, LoggingActivityStatus status, long? recordsImpacted = null, string? metricsJson = null, CancellationToken ct = default);
Task FailAsync(LoggingActivityScope scope, Exception error, CancellationToken ct = default);

// Legacy: id-only overloads (probe every partition on MySQL; kept for external callers).
Task CompleteAsync(string activityId, LoggingActivityStatus status, long? recordsImpacted = null, string? metricsJson = null, CancellationToken ct = default);
Task FailAsync(string activityId, Exception error, CancellationToken ct = default);
```

`CompleteAsync` is the terminal call for non-exception outcomes: `Succeeded`, `Canceled`, or `Skipped`. It explicitly **rejects** `Status.Failed` — that path is `FailAsync`, which records the exception message and the fully-qualified type name as well as the status.

Both methods stamp `completed_on = UtcNow` and `last_modified_on = UtcNow` on the row.

### AddInputAsync

```csharp
Task AddInputAsync(
    string activityId,
    string inputActivityId,
    string? inputRole = null,
    CancellationToken cancellationToken = default);
```

Inserts a single edge into `activity_input`. Call it once per upstream input the run consumed — a Silver run that joins three Bronze loads emits three `AddInputAsync` calls. Duplicate edges are silently coalesced.

### LoggingActivityScope

`LoggingActivityScope` is `IDisposable` and bundles three things: the caller-supplied `ActivityId`, the started `System.Diagnostics.Activity` (so consumers can read its `TraceId`/`SpanId`), and the MEL scope handle.

Dispose pops the MEL scope first, then stops the Activity. It is intentionally **not** an async-disposable and does **not** auto-call `CompleteAsync` or `FailAsync`. The terminal status is information the dispose path cannot recover — Succeeded, Canceled, and Skipped are all distinct outcomes.

```csharp
using LoggingActivityScope scope = await activities.BeginAsync(request, cancellationToken);
try
{
    await DoWorkAsync(cancellationToken);
    await activities.CompleteAsync(scope.ActivityId, LoggingActivityStatus.Succeeded, ct);
}
catch (Exception ex)
{
    await activities.FailAsync(scope.ActivityId, ex, CancellationToken.None);
    throw;
}
```

---

## The Log Write Pipeline

```
ILogger.LogInformation(...)
   │
   ▼
OpenTelemetryLoggerProvider             (configured via builder.Logging.AddRoadbedDbLogging)
   │
   ▼
BatchLogRecordProcessor                 (OTel default; default 1024-record buffer)
   │
   ▼
RoadbedDbLogRecordExporter.Export(batch)
   │  ├── recursion guard check (drops Roadbed.Logging/Data/MySqlConnector categories)
   │  ├── MapRecord: pulls activity_id from scope, then Activity.Current tag
   │  ├── pulls message_template from {OriginalFormat}, properties from named args
   │  └── enqueue via LoggingChannel.TryWrite
   │
   ▼
LoggingChannel  (BoundedChannelOptions{ FullMode = DropOldest | Wait })
   │
   ▼
LogWriterHostedService.ExecuteAsync     (BackgroundService)
   │  ├── drain channel into a buffer
   │  ├── when buffer >= BatchSize OR FlushInterval elapsed: FlushAsync
   │  ├── on shutdown: drain remaining + final FlushAsync(CancellationToken.None)
   │  └── on insert failure: WriteFallback(batch) → Console.Error
   │
   ▼
ILoggingLogEntryRepository.BulkInsertAsync
   │  └── chunked multi-row INSERT, sized under MaxPlaceholdersPerStatement
   │
   ▼
log_entries
```

### RoadbedDbLogRecordExporter

The exporter is an OTel `BaseExporter<LogRecord>`. Its `Export(in Batch<LogRecord>)` walks each record:

1. **Recursion guard.** If the record's `CategoryName` starts with any prefix in `LoggingOptions.RecursionGuardCategories`, the record is silently dropped. Defaults cover `Roadbed.Logging`, `Roadbed.Data`, `Roadbed.Data.MySql`, `Roadbed.Data.Sqlite`, and `MySqlConnector`. Hosts can append their own prefixes (for instance to silence a noisy third-party driver).
2. **Map.** Pulls `Timestamp` → `event_time_utc`, `LogLevel` → numeric, `CategoryName`, `EventId.Id` and `EventId.Name`, `FormattedMessage` → `message`, `Exception.Message` and `Exception.GetType().FullName`, `TraceId` and `SpanId` (already hex-encoded by OTel). From `LogRecord.Attributes`: the `{OriginalFormat}` key becomes `message_template`; the rest become `properties` JSON via `Newtonsoft.Json`.
3. **Resolve `activity_id`.** Walks the scope chain via `LogRecord.ForEachScope` looking for the `"activity_id"` key. Falls back to `Activity.Current?.GetTagItem("roadbed.activity_id")` when no scope frame carried one. Both mechanisms are wired by `BeginAsync` so either path resolves.
4. **Enqueue.** `LoggingChannel.TryWrite(entry)`. On the default `DropOldest` policy this always succeeds; on `BlockBriefly` it returns `false` on contention and increments the dropped counter.

The exporter returns `ExportResult.Success` regardless of channel acceptance — the writer is responsible for surfacing back-pressure.

### LoggingChannel

A thin wrapper around `Channel<LoggingLogEntry>`. Constructed from `LoggingOptions.ChannelCapacity` (default 50,000) and `LoggingOptions.ChannelFullPolicy` (default `DropOldest`).

- **`DropOldest`** — `BoundedChannelFullMode.DropOldest`. Producers never block; the channel silently discards the oldest queued entry to make room. Drops are not directly observable through `TryWrite`.
- **`BlockBriefly`** — `BoundedChannelFullMode.Wait`. Producers calling `TryWrite` get `false` on contention, and the wrapper increments an atomic dropped counter. The writer reads and resets the counter on every successful flush and surfaces it as a periodic Warning log.

`SingleReader = true` because only the hosted writer drains.

### LogWriterHostedService

A `BackgroundService` whose `ExecuteAsync` loops on `LoggingChannel.Reader.WaitToReadAsync`. Each iteration:

1. Drains everything immediately available with `TryRead`.
2. Adds each entry to a buffer.
3. **Flushes** when the buffer reaches `LoggingOptions.BatchSize` (default 1000) OR when `LoggingOptions.FlushInterval` (default 5 seconds) elapses since the last flush — whichever comes first.

`FlushAsync` calls `ILoggingLogEntryRepository.BulkInsertAsync`. On success, it reads and zeroes the dropped-counter and surfaces it as a Warning log. On exception, it calls a private `WriteFallback` that writes the batch's pretty-printed contents to `Console.Error` so the lines are never silently lost. The buffer is always cleared in `finally` — a single bad batch must not wedge the writer loop.

`StopAsync` (inherited from `BackgroundService`) cancels `ExecuteAsync`. The `catch (OperationCanceledException)` block falls through to a final drain that pulls the remaining channel content into the buffer and emits one last `FlushAsync(CancellationToken.None)` so the shutdown does not lose work.

### Recursion Safety

Structurally, the channel hand-off decouples produce from consume — `Export` runs on the OTel batch processor thread; `FlushAsync` runs on the hosted-service thread. The recursion-guard filter on the exporter is a backstop against any framework-level diagnostic that would otherwise feed log lines back through the database write path. Internal writer diagnostics (the "Dropped N entries" warning, the bulk-insert retry messages) emit through the writer's `ILogger<LogWriterHostedService>`, whose category matches the guard. Catastrophic-fallback messages (DB errors, dropped batches) go to `Console.Error` directly.

---

## Module Auto-Discovery and Wiring

```csharp
public class InstallLogging : IServiceCollectionInstaller
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // 1. Resolve host registrations up-front; throw with actionable messages if missing.
        LoggingOptions options;
        ILoggingDatabaseFactory factory;
        using (var setupProvider = services.BuildServiceProvider())
        {
            options = setupProvider.GetService<LoggingOptions>()
                ?? throw new InvalidOperationException(...);
            factory = setupProvider.GetService<ILoggingDatabaseFactory>()
                ?? throw new InvalidOperationException(...);
        }

        // 2. Validate the connection-string type is one we support.
        ValidateProvider(factory);

        // 3. Configure Dapper [Column] mapping for the three entities.
        DapperMapping.Configure(
            typeof(LoggingActivity),
            typeof(LoggingActivityInput),
            typeof(LoggingLogEntry));

        // 4. Register repositories and activity service as typed singletons.
        services.TryAddSingleton<ILoggingActivityRepository, LoggingActivityRepository>();
        services.TryAddSingleton<ILoggingActivityInputRepository, LoggingActivityInputRepository>();
        services.TryAddSingleton<ILoggingLogEntryRepository, LoggingLogEntryRepository>();
        services.TryAddSingleton<ILoggingActivityService, LoggingActivityService>();
        services.TryAddSingleton<LoggingActivityService>();

        // 5. LoggingChannel — eagerly constructed and registered as a
        //    concrete-instance singleton so every IServiceProvider built
        //    from this collection (the host's container and every
        //    ServiceLocator snapshot) resolves the SAME object. See the
        //    "Multi-Container Channel Sharing" section below for why this
        //    is load-bearing.
        if (!services.Any(d => d.ServiceType == typeof(LoggingChannel)))
        {
            services.AddSingleton<LoggingChannel>(new LoggingChannel(options));
        }

        services.AddHostedService<LogWriterHostedService>();

        // 6. Snapshot ServiceLocator so LoggingActivityService's public ctor can resolve.
        ServiceLocator.SetLocatorProvider(services.BuildServiceProvider());
    }
}
```

The companion `LoggingBuilderExtensions.AddRoadbedDbLogging` lives outside `InstallLogging` because the MEL configuration extension method needs `ILoggingBuilder`, which an `IServiceCollectionInstaller` does not see. It registers OTel's logger provider with `IncludeScopes = true`, `IncludeFormattedMessage = true`, `ParseStateValues = true`, then wires a `BatchLogRecordExportProcessor` whose factory hands the exporter a **lazy** accessor for `LoggingChannel` rather than a resolved instance — see the "Build-Order Robustness" subsection below.

Host wiring order:

```csharp
builder.Services.AddSingleton(new LoggingOptions { ... });
builder.Services.AddSingleton<ILoggingDatabaseFactory, FooLoggingDatabaseFactory>();

builder.Logging.AddRoadbedDbLogging();                        // OTel + exporter
builder.Services.InstallModulesInAppDomain(builder.Configuration);  // InstallLogging runs

using var host = builder.Build();
await host.RunAsync();
```

### Multi-Container Channel Sharing

Roadbed framework services use a dual-constructor pattern: the `public` constructor resolves dependencies via `ServiceLocator.GetService<T>()`, where `ServiceLocator` holds a point-in-time snapshot of the host's `IServiceCollection`. That snapshot is a **separate `IServiceProvider`** from the host's own container — the host has its own runtime DI graph, and every `ServiceLocator.SetLocatorProvider(services.BuildServiceProvider())` call creates a new one.

When a component resolved via `ServiceLocator` emits a log line, the `ILogger` it uses came from that snapshot's `IServiceProvider`. The snapshot has its own `ILoggerFactory`, which builds its own `OpenTelemetryLoggerProvider`, which builds its own `RoadbedDbLogRecordExporter`. The exporter must enqueue into the **same** `LoggingChannel` the `LogWriterHostedService` (running in the host's container) drains — otherwise the log lines reach an orphan channel that nothing reads, and `log_entries` stays empty.

The fix that makes this work: register `LoggingChannel` as `AddSingleton<LoggingChannel>(eagerlyConstructedInstance)`. A **concrete-instance descriptor** pins one object across every `IServiceProvider` built from the underlying collection. Producers in any container converge on one channel; the single consumer (the host writer) drains it.

The thread-safety primitives this leans on are already correct: `System.Threading.Channels.Channel<T>` is multi-producer safe, and `LoggingChannel._droppedCount` is updated via `Interlocked.Increment` / `Interlocked.Exchange`.

### Build-Order Robustness

`AddRoadbedDbLogging` registers a deferred OTel processor factory — the lambda `sp => new BatchLogRecordExportProcessor(new RoadbedDbLogRecordExporter(...))` only runs when the OTel logger provider is **realized** (the first time an `ILoggerFactory` resolves and constructs the OTel provider). Two distinct things can fail without care:

1. **Premature realization in another installer.** If some other `IServiceCollectionInstaller` calls `services.BuildServiceProvider()` and then resolves `ILoggerFactory` eagerly (a pre-fix `InstallExtensionsLogging` did exactly this), the OTel provider realizes inside the throwaway container. The processor factory runs with the throwaway `sp`. If `InstallLogging` has not yet registered `LoggingChannel`, `sp.GetRequiredService<LoggingChannel>()` throws.
2. **Different exporter resolves different channel.** Even when realization succeeds, the exporter captures the channel from the realizing `sp` — pre-fix that meant each container's exporter held a reference to that container's `LoggingChannel`, severing the producer–consumer link.

The framework defends in depth:

- `InstallExtensionsLogging` no longer eagerly resolves `ILoggerFactory`. Provider construction is cheap and does not realize singletons; the OTel provider stays unrealized until the host fully boots.
- `LoggingChannel` is a concrete-instance singleton (the cause-2 fix above), so even when multiple OTel exporters are constructed in different containers, they all resolve to the same channel object.
- `RoadbedDbLogRecordExporter` takes a `Func<LoggingChannel>` accessor and wraps it in `Lazy<LoggingChannel>`. The channel is resolved on **first** `Export(in Batch<LogRecord>)` call, never at exporter construction. If some other installer still manages to realize the OTel provider early, the processor factory completes successfully and the actual channel resolution happens once the host is built — by which time `InstallLogging` has run and the channel is registered.

These three together make the documented host wiring (`AddRoadbedDbLogging()` before `InstallModulesInAppDomain`) robust to installer-discovery order.

---

## Schema Installation

Roadbed.Logging does **not** run migrations. The DDL ships as install scripts under [src/Roadbed.Logging/Assets/Tables/](../../src/Roadbed.Logging/Assets/Tables/). Copies are also embedded in [the skill's reference](../../skills/code-roadbed-csharp/references/reference-roadbed-logging.md#ddl-install-scripts) so an AI assistant can paste them straight into a host setup script.

The scripts create tables unqualified. Run them with the target database already selected (`USE logging;` on MySQL, against the right `.db` file on SQLite). The default `LoggingOptions.Schema` is the empty string for SQLite-dev frictionlessness; in production, set it to the MySQL database name (e.g. `"logging"`) so the C# repositories qualify every statement.

Retention windows by table:

| Table            | Window     |
| ---------------- | ---------- |
| `log_entries`    | 90 days    |
| `activity`       | 12 months  |
| `activity_input` | 12 months  |

Retention enforcement:

- **MySQL** — schedule a recurring partition-drop routine that, for each table, executes `ALTER TABLE {schema}.<table> DROP PARTITION p_YYYYMM` for every partition whose entire date range falls outside the retention window. Drops are atomic, sub-second on empty partitions, and reclaim disk immediately. Because the install scripts pre-create 120 monthly partitions through 2035-12, **no forward-rollover step is needed until that decade is approaching exhaustion** — only the drop side.
- **Roadbed.Logging does not currently ship the drop routine.** The consuming application provides it (a stored proc invoked by a Roadbed.Scheduling job, or a MySQL EVENT). A future framework sprint may ship a default routine plus a Roadbed.Scheduling job that calls it; until then, the operator owns scheduling.
- **SQLite** — equivalent DELETE statements on the same cadence:
  ```sql
  DELETE FROM {schema}log_entries     WHERE event_time_utc < datetime('now', '-90 days');
  DELETE FROM {schema}activity_input  WHERE created_on     < datetime('now', '-12 months');
  DELETE FROM {schema}activity        WHERE created_on     < datetime('now', '-12 months');
  ```
  Follow with `VACUUM` (or set `PRAGMA auto_vacuum = INCREMENTAL` plus periodic `incremental_vacuum`) to reclaim disk.

## MCP / Analytical Query Advice

The shipped index layout drives the query patterns that prune efficiently:

- **Activity-table queries should filter on `created_on`.** The composite indexes are ordered `(fleet_filter…, created_on)`, so a query like `WHERE application = ? AND status = ? AND created_on BETWEEN ? AND ?` picks the right index AND prunes to the relevant monthly partitions.
- **Log-table queries should filter on `event_time_utc`.** For per-activity log pulls, pass the activity's own `started_on..completed_on` window — the query reads at most a few partitions even when the global log volume is large.
- **`created_on` is the partition / retention timestamp.** Use it in WHERE clauses for pruning. Display `started_on` / `completed_on` for activity timing (those are the wall-clock timestamps the app supplied). Duration is `completed_on - started_on`; `created_on` is close but is the row-insert timestamp and may be slightly later than `started_on` if the INSERT is delayed.
- **Composite-PK lookups by id alone still work** (the PK leads with `id`), but UPDATEs that pass `AND created_on = ?` prune to one partition instead of probing all 120. The framework's `LoggingActivityService` does this automatically when the scope-aware overloads are used.

---

## Implementation Walkthrough

A consuming Quartz job that opens an activity, heartbeats, records lineage, and finalizes:

```csharp
namespace Foo.App.Jobs;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Quartz;
using Roadbed.Logging;
using Roadbed.Scheduling;

public sealed class FooIngestionJob : BaseSchedulingJob<FooIngestionJob>
{
    private readonly LoggingActivityService _activities;
    private readonly IFooLoader _loader;

    public FooIngestionJob(
        ILogger<FooIngestionJob> logger,
        LoggingActivityService activities,
        IFooLoader loader)
        : base(
            name: "FooIngestion",
            description: "Loads the Foo dataset every 15 minutes.",
            schedule: new SchedulingSchedule(TimeSpan.FromMinutes(15)),
            logger: logger)
    {
        ArgumentNullException.ThrowIfNull(activities);
        ArgumentNullException.ThrowIfNull(loader);

        this._activities = activities;
        this._loader = loader;
    }

    public override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        string activityId = Ulid.NewUlid().ToString();   // app owns the ULID dep

        using LoggingActivityScope scope = await this._activities.BeginAsync(
            new LoggingActivityBeginRequest
            {
                Id = activityId,
                ActivityType = LoggingActivityType.Ingestion.ToString().ToLowerInvariant(),
                Target = "ops.foo",
                ActivityKey = "Foo.Ingestion.FullRefresh",
                FireInstanceId = this.Context.FireInstanceId,
                QuartzJobName = this.Context.JobDetail.Key.Name,
                QuartzJobGroup = this.Context.JobDetail.Key.Group,
                QuartzTriggerName = this.Context.Trigger.Key.Name,
                QuartzTriggerGroup = this.Context.Trigger.Key.Group,
                SchedulerInstanceId = this.Context.Scheduler.SchedulerInstanceId,
            },
            cancellationToken);

        try
        {
            int rows = 0;
            await foreach (var batch in this._loader.StreamAsync(activityId, cancellationToken))
            {
                rows += await this._loader.WriteAsync(batch, cancellationToken);

                // Prefer the scope-aware overload — it includes scope.CreatedOn
                // in the UPDATE WHERE clause so MySQL prunes to one monthly
                // partition instead of probing all 120.
                await this._activities.HeartbeatAsync(scope, cancellationToken);
            }

            // Record the Bronze inputs this Silver run consumed.
            foreach (var inputId in this._loader.ConsumedActivityIds)
            {
                await this._activities.AddInputAsync(activityId, inputId, "bronze", cancellationToken);
            }

            await this._activities.CompleteAsync(
                scope,
                LoggingActivityStatus.Succeeded,
                recordsImpacted: rows,
                cancellationToken: cancellationToken);

            this.Context.Result = $"Loaded {rows:N0} foo rows";
        }
        catch (Exception ex)
        {
            await this._activities.FailAsync(scope, ex, CancellationToken.None);
            throw;
        }
    }
}
```

Inside the loader, every `this._logger.LogInformation(...)` call inherits the ambient `activity_id`, `trace_id`, and `span_id` and lands in `log_entries` with those columns populated.

---

## Common Pitfalls

**Disposing the scope without calling `CompleteAsync`.** The row stays in `running` forever. Always finalize explicitly with `CompleteAsync` or `FailAsync`.

**Passing `LoggingActivityStatus.Failed` to `CompleteAsync`.** Throws `ArgumentException`. Use `FailAsync(activityId, exception)` — it also records the message and type.

**Setting `Schema = "ops"` against SQLite without `ATTACH`.** The SQL becomes `INSERT INTO ops.activity ...`, which SQLite rejects. Either leave `Schema` empty for SQLite or ATTACH the file under that alias.

**Generating ULIDs inside Roadbed.Logging.** The library never generates identifiers — the caller mints the ULID and passes it on `BeginAsync`, and the same identifier is what `IAsyncBulkInsertOperation.BulkInsertAsync` should receive for the run's Bronze/Silver writes. Mints inside the library would orphan the lineage.

**Adding `Roadbed.Crud` as a dependency.** The "B" tier and the log writer look superficially similar but stamp `activity_id` differently. Roadbed.Crud's `BulkInsertAsync(string activityId, IList<TEntity>, ct)` stamps every row with one uniform identifier — correct for Bronze/Silver loads where the load's activity *is* each row's lineage. The log writer stamps each row with its **own originating** `activity_id` because a batch of log rows mixes entries from different scopes.

**Logging from a category that overlaps `RecursionGuardCategories`.** The exporter silently drops those records to prevent the database write path from logging through itself. Either accept the drop or use a different category for genuine operator-visible diagnostics.

**Treating logs as a substitute for the activity row.** Logs are per-event narrative; the activity row is the run record. Always `BeginAsync` at the start of a meaningful unit of work — heartbeats, lineage, and terminal status all hang off the activity row.

**Expecting `LoggingActivityScope.Dispose` to be async.** It is not. The dispose path stops the diagnostic Activity and pops the MEL scope synchronously; persistence is the caller's job via the explicit terminal methods.

**Re-registering `LoggingChannel` in `Program.cs`.** `InstallLogging` eagerly constructs the channel from `LoggingOptions` and registers it as a concrete-instance singleton. Overwriting that registration after the installer runs (`services.AddSingleton<LoggingChannel>(new LoggingChannel(...))`) replaces the shared instance and severs the producer–consumer link.

---

## Troubleshooting

### `activity` rows write but `log_entries` stays empty

The producer-side exporter is enqueueing into a different `LoggingChannel` instance than the host writer drains, or the host's MEL pipeline never realized the OTel DB exporter at all. Common causes after this fix shipped:

1. **Stale vendored DLLs.** A consumer that vendors `Roadbed.Common.dll` and `Roadbed.Logging.dll` into its `lib/` directory needs to re-vendor both after this fix lands — the pre-fix `InstallExtensionsLogging` froze a throwaway `ILoggerFactory` instance that orphaned the OTel DB exporter from the host's runtime singletons. Re-vendor from the framework solution's `bin/Release/net10.0/` directory.
2. **Re-registered `LoggingChannel`.** Search `Program.cs` and any installer in the host's solution for `AddSingleton<LoggingChannel>` calls. The framework installer is the single owner; another registration replaces the shared instance.
3. **DB user missing privileges on `log_entries`.** The `activity` write only proves grants on `activity`. Run `SHOW GRANTS FOR <user>` and confirm `INSERT` on `<schema>.log_entries`. A silent grant failure manifests as the exporter enqueuing, the writer flushing, the DB rejecting the INSERT, and the `Console.Error` fallback firing — check the host's stderr first.

### Startup crash: `No service for type 'Roadbed.Logging.LoggingChannel' has been registered.`

This was the pre-fix symptom of an installer realizing the OTel logger provider before `InstallLogging` had registered the channel descriptor. With the fix applied:

- `InstallExtensionsLogging` no longer eagerly resolves `ILoggerFactory`.
- `RoadbedDbLogRecordExporter` resolves the channel lazily on first `Export(...)` call.

If this crash persists, you are running pre-fix vendored DLLs. Re-vendor both `Roadbed.Common.dll` and `Roadbed.Logging.dll`.

### Log lines from `ServiceLocator`-resolved components are missing while host-resolved ones land fine

The cause-2 pre-fix symptom. `LoggingChannel` was `TryAddSingleton<LoggingChannel>()` (typed-factory registration), so every `IServiceProvider` built from the underlying collection — host container plus each `ServiceLocator` snapshot — created its **own** `LoggingChannel`. The fix promotes it to a concrete-instance singleton. Confirm `Roadbed.Logging.dll` is at or after this fix; the channel-sharing test in `Roadbed.Test.Unit.Logging.InstallLoggingTests` covers exactly this scenario.

---

## Quick Reference

| Need                                                            | Use                                                                                                |
| --------------------------------------------------------------- | -------------------------------------------------------------------------------------------------- |
| Insert a run record                                             | `await service.BeginAsync(request, ct)`                                                            |
| Stamp `last_heartbeat_on`                                       | `await service.HeartbeatAsync(activityId, ct)`                                                     |
| Patch current-state columns (target, metrics, Quartz, …)        | `await service.UpdateAsync(updateRequest, ct)`                                                     |
| Finish successfully                                             | `await service.CompleteAsync(activityId, LoggingActivityStatus.Succeeded, recordsImpacted: n, ct)` |
| Finish on exception                                             | `await service.FailAsync(activityId, exception, ct)`                                               |
| Mark canceled / skipped                                         | `await service.CompleteAsync(activityId, LoggingActivityStatus.Canceled, ct)`                      |
| Record a lineage edge                                           | `await service.AddInputAsync(consumerId, inputId, inputRole, ct)`                                  |
| Wire MEL → OTel → DB                                            | `builder.Logging.AddRoadbedDbLogging()`                                                            |
| Switch to back-pressure on overflow                             | `new LoggingOptions { ChannelFullPolicy = LoggingChannelFullPolicy.BlockBriefly }`                  |
| Quiet a noisy third-party category                              | `options.RecursionGuardCategories.Add("ThirdParty.Noisy")`                                          |
| Read the schema scripts                                         | [src/Roadbed.Logging/Assets/Tables/](../../src/Roadbed.Logging/Assets/Tables/)                     |
