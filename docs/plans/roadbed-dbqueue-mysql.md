# `Roadbed.DbQueue` / `Roadbed.DbQueue.MySql` — a reusable MySQL/MariaDB-backed queue library

> This is the implementation spec for a new Roadbed library. Place it in the Roadbed repo
> (`docs/plans/`) and follow it to build the library.

Audience: the Roadbed coding agent.

---

## 1. What this library is

A reusable, queue-agnostic library that lets multiple sites push request records into
MySQL/MariaDB tables during a web request (cheap INSERT, no inline work) and have a
background job drain them later. It is generic over a payload type `T`: a typed
*queue definition* binds a queue name to its payload type, an `EnqueueAsync(payload)`
appends a message and returns a shareable tracking id, and a `QueueProcessor<T>` claims
a FIFO batch of unprocessed messages, deserializes each payload, hands it to a
caller-supplied handler, and records the per-message outcome.

Two first-consumer **shapes** motivate the design (described generically — they are
illustration, not a coupling):

- an **email-unsubscribe queue** — a payload carrying the address + list/campaign
  context; the handler marks the address unsubscribed. A silently-dropped one is a
  legal/compliance risk, which drives the failure-visibility requirements (§9).
- a **website form-submission queue** — a payload carrying a submitted form's fields;
  the handler feeds a rule engine. Higher volume, lower per-item legal stakes.

It runs on the **existing** `Roadbed.Scheduling` Quartz wrapper (single-worker,
non-overlapping jobs). This library is **processing + enqueue only** — it ships **no**
Quartz dependency, no job classes, and no scheduling wiring. The consuming host owns the
job that calls `QueueProcessor<T>.ProcessBatchAsync(...)` on a `BaseSchedulingJob<T>`.

---

## 2. Decisions baked in

| # | Decision | Where it lands |
| - | -------- | -------------- |
| D1 | **No dependency on `Roadbed.Messaging`** (cloud-broker envelope lib, still drags Cysharp `Ulid`, which is being removed portfolio-wide). Depend on `Roadbed.Data.MySql` (connection + executor) + `Roadbed.Common` (`RoadbedJson.Options`, base classes, installer/locator). | §4, §5 |
| D2 | **Scope is ENQUEUE + PROCESS.** `EnqueueAsync(payload)` mints an `external_id`, INSERTs the message row, and **returns the `external_id`** for tracking/URLs; `ProcessBatchAsync(...)` drains the queue. | §6 |
| D3 | **`external_id = UUIDv7** via `Guid.CreateVersion7()`**, `CHAR(36)`, canonical lowercase hyphenated "D" format — **not** Cysharp `Ulid`. Stored alongside the internal auto-increment `id`. `external_id` = shareable handle; `id` = internal FIFO/PK. | §6, §7 |
| D4 | **Connection per queue.** Each queue lives **in the business schema it serves**, not a dedicated queue schema and not the logging schema. The library takes its `IDataConnectionFactory` **per queue** (a `QueueDefinition<T>` field), so each queue resolves its own schema's connection. **No connection-string management inside the library.** | §4, §5 |
| D5 | **Month RANGE partitioning on BOTH tables** (message by `created_on`, processed by `processed_on`), mirroring `logging.activity`. **No FK between the tables** (MySQL forbids FKs on partitioned tables) — `fk_queue_id` is a plain indexed **logical** reference. **Composite keys** (every unique key includes the partition column). | §7 |
| D6 | **Processed-table uniqueness weakens** to "one row per message *per month*" under partitioning. The **app-level anti-join is the real idempotency guard**, not the DB unique. | §7, §8 |
| D7 | **Retention is external/DBA** (`DROP PARTITION`), **message-table-FIRST, then processed-table.** Library does no retention. Reference DDL defaults to a **12-month** window, per-queue overridable by the DBA. | §9 |
| D8 | **No auto-retry.** A processed row (success *or* failure) excludes a message forever. Failed rows MUST surface to the portfolio observability/activity layer; stale-unprocessed-past-retention messages are silently dropped on partition removal, so monitoring must catch them before the boundary. | §8, §9 |
| D9 | **Two-table immutable-message + processed-companion model** diverges from the portfolio's existing single-table-with-status queues. New queues use THIS model; existing bespoke queues stay as-is unless separately migrated. | §10 |
| D10 | **Project structure is a core/satellite SPLIT** — a provider-agnostic `Roadbed.DbQueue` core + a `Roadbed.DbQueue.MySql` satellite — mirroring `Roadbed.Logging` + `Roadbed.Logging.MySql`. | §4 |
| D11 | Carried forward: table-name **whitelist + parameterization** safety; the **single-consumer** assumption; the **idempotency note**; the **generic** `QueueProcessor<T>` / `QueueDefinition<T>` shape; the **delegate** handler contract (not an interface). | §5, §8, §11 |
| D12 | **No runtime DDL** — the library issues no CREATE/ALTER/DROP at any time. This spec defines the **canonical table-shape contract** (§7), and the agent **also authors reference partitioned `CREATE TABLE` templates** for the DBA (the `logging` precedent — agent-authored, human-run), while the library itself runs no DDL. | §7 |

---

## 3. Grounding — the real Roadbed seams this builds on

Everything below was read in `C:\Source\Roadbed` (read-only). Roadbed has **no root
`CLAUDE.md`** — orientation is `docs/claude-project-instructions.md` +
`skills/code-roadbed-csharp/SKILL.md`. The `code-roadbed-csharp` skill's
`references/reference-roadbed-data.md`, `reference-roadbed-data-mysql.md`,
`reference-roadbed-common.md`, and `reference-roadbed-scheduling.md` are **required
reading** before writing code against those libraries.

**Connection + executor seam (the core of D4).** Roadbed's non-CRUD data pattern is:
a host registers a **marker** `IFooDatabaseFactory : IDataConnectionFactory` (empty,
so DI can distinguish databases), implemented by inheriting the per-DB factory
(`MySqlConnectionFactory`); components then call the **static** `MySqlExecutor.*Async`
passing that factory:

```
MySqlExecutor.ExecuteAsync(DataExecutorRequest request, IDataConnectionFactory factory, ILogger? logger = null, CancellationToken ct = default)  // Task<int>
MySqlExecutor.QueryAsync<T>(...)                  // Task<IEnumerable<T>>
MySqlExecutor.QuerySingleOrDefaultAsync<T>(...)   // Task<T?>
MySqlExecutor.ExecuteScalarAsync<T>(...)          // Task<T?>
```

- Marker pattern + "inject the marker, never `IDataConnectionFactory`":
  `skills/code-roadbed-csharp/references/reference-roadbed-data.md:18-23, 36-49`.
- Executor signatures + "MUST call `MySqlExecutor.*Async`; it handles connection
  lifecycle, retries on transient codes, structured logging":
  `references/reference-roadbed-data-mysql.md:18-23, 155-166`.
- **Live proof a generic non-CRUD component already takes a per-schema factory this
  exact way:** `Roadbed.Logging` keeps its core assembly DB-client-free via a
  provider-neutral execution **port** `ILoggingDataExecutor` whose methods are
  `(DataExecutorRequest, ILoggingDatabaseFactory factory, ILogger, CancellationToken)`
  — `src/Roadbed.Logging/RepositoryInterfaces/ILoggingDataExecutor.cs:29-59` — and the
  satellite implements it as a thin adapter over `MySqlExecutor`:
  `src/Roadbed.Logging.MySql/MySqlLoggingDataExecutor.cs:14-38`. The marker factory it
  takes is `ILoggingDatabaseFactory : IDataConnectionFactory`
  (`src/Roadbed.Logging/ILoggingDatabaseFactory.cs:22-24`). **This is the precedent to
  mirror** — except `Roadbed.DbQueue` takes the factory **per queue** (a
  `QueueDefinition<T>` field), not once per assembly, because queues live in different
  schemas (D4).
- The connection is returned **already open**; the caller owns disposal via `using`.
  Do **not** call `Open()` again. (`reference-roadbed-data.md:28, 246-255`.) In
  practice the static `MySqlExecutor` already owns the open/dispose, so the library
  passes the factory and never touches a raw connection.

**Serialization (D1).** Use `System.Text.Json` and pass the shared frozen options
`Roadbed.RoadbedJson.Options` to every `Serialize`/`Deserialize`. Allocating per-call
`JsonSerializerOptions` is the #1 STJ perf footgun (it keys its metadata cache by
instance). Source: `src/Roadbed.Common/Json/RoadbedJson.cs:31-66` (the property is
`RoadbedJson.Options`, line 39; namespace is `Roadbed`, line 6). Skill rule:
`SKILL.md:44`. The payload column is JSON; serialize `T` with these options on enqueue,
deserialize with the same on claim.

**Partition + composite-key DDL precedent (D5).** `logging.activity` is monthly RANGE
partitioned with the exact key rules this library needs:
- `PRIMARY KEY (id, created_on)` — every UNIQUE/PRIMARY key on a partitioned InnoDB
  table **must contain the partition column** (`install_mysql.txt:79`, rationale
  `:24-26`).
- **No FK on partitioned InnoDB tables** — lineage references are "soft on purpose"
  (`install_mysql.txt:28-29`).
- `PARTITION BY RANGE (TO_DAYS(created_on))` with a `p_min` floor, 120 monthly
  partitions (`p_202601 … p_203512`), and a `pmax` catch-all
  (`install_mysql.txt:90-213`).
- Timestamps default to `UTC_TIMESTAMP(6)` so server time zone never moves the
  partition key (`install_mysql.txt:36-46, 77`).
- UUIDv7 id stored `CHAR(36) CHARACTER SET ascii COLLATE ascii_bin` (lexical =
  chronological, since UUIDv7's first 48 bits are a big-endian ms timestamp) —
  `install_mysql.txt:48-49`, and the portfolio-wide 26→36 widen rationale is
  `upgrade_2026-06_uuidv7_widen_mysql.txt:8-21`.
- Retention is external (`DROP PARTITION` older than the window; 12 months for
  `activity`) — `install_mysql.txt:18-21`.

**Scheduling boundary (out of scope, but this is what calls the processor).** The host's
job inherits `BaseSchedulingJob<T>` and calls the processor inside `ExecuteAsync`; the
library ships no Quartz reference. Single-worker, non-overlapping, `[DisallowConcurrentExecution]`
is the consumer's contract — see `reference-roadbed-scheduling.md:21-23, 51-86`. This
library adds **no** Quartz/job code.

**Project + build facts.**
- Solution: `src/Roadbed .NET Solution.slnx` (name contains spaces — **quote it**).
- Existing libraries sit one-project-per-folder under `src/` (e.g. `Roadbed.Logging` +
  `Roadbed.Logging.MySql` are two sibling projects — the core/satellite split is real).
- Unit tests: `src/Roadbed.Test.Unit/Roadbed.Test.Unit.csproj`. MSTest, Roy-Osherove
  naming, try/catch for exceptions (no `Assert.ThrowsException`), specialized asserts
  (MSTEST0037) — `docs/claude-project-instructions.md:20-356`.
- Build with `<GenerateDocumentationFile>true</GenerateDocumentationFile>` +
  `<TreatWarningsAsErrors>True</TreatWarningsAsErrors>` — missing XML docs and any
  `TODO`/`FIXME` (Sonar S1135) **fail the build**. (`SKILL.md:47`.)

---

## 4. Project layout — core/satellite split (D10)

Mirror `Roadbed.Logging` + `Roadbed.Logging.MySql`: a **provider-agnostic core** plus a
**MySQL satellite** (the same shape `Roadbed.Logging` already ships — a core, a `.MySql`,
and a `.Sqlite`, all three sibling projects under `src/`).

- **`Roadbed.DbQueue`** (core, no DB-client dependency): the abstractions —
  `QueueDefinition<T>`, `QueueProcessor<T>`, the handler **delegate**
  `QueueMessageHandler<T>` (§5.2, D11 — a delegate, not an interface), the
  `QueueMessage<T>` / `QueueProcessResult` value types, the queue-name
  whitelist/validator, and a provider-neutral execution **port** `IDbQueueDataExecutor`
  modeled on `ILoggingDataExecutor` (`Execute`/`Query` methods taking
  `(DataExecutorRequest, IDataConnectionFactory, ILogger, CancellationToken)`).
- **`Roadbed.DbQueue.MySql`** (satellite): `MySqlDbQueueDataExecutor` — the thin adapter
  over `MySqlExecutor` (the one-to-one analogue of `MySqlLoggingDataExecutor.cs:14-38`,
  which simply forwards each call to `MySqlExecutor`) — plus the installer
  `InstallDbQueueMySql` and the reference-DDL template assets (§7, D12) as embedded
  resources under `Assets/Tables/`.

Add both projects to `src/Roadbed .NET Solution.slnx` and to `Roadbed.Test.Unit`'s
references.

> **Why split, not just `.MySql`:** the SQL composition (table-name interpolation,
> anti-join, `INSERT`) is MySQL-dialect-specific and belongs in the satellite; the
> generic `QueueProcessor<T>`/`QueueDefinition<T>` and the whitelist are
> provider-agnostic. The split keeps the door open for a future `Roadbed.DbQueue.Sqlite`
> (handy for in-memory unit tests of the processor logic) without dragging
> `MySqlConnector` into the core — exactly as `Roadbed.Logging` already does.

---

## 5. Public surface (provider-agnostic core)

> Signatures are the *intended shape*; the agent finalizes names/nullability per the
> house rules (`this.`-prefixed members, XML docs on every public/protected member,
> `ArgumentNullException.ThrowIfNull` / `ArgumentException.ThrowIfNullOrWhiteSpace`
> validation, region blocks, CancellationToken last).

### 5.1 `QueueDefinition<T>` — binds a queue name + payload type + its connection

```
public sealed class QueueDefinition<T>
{
    public string QueueName { get; }                 // validated whitelist (§11)
    public IDataConnectionFactory ConnectionFactory { get; }  // PER QUEUE (D4)
    public string MessageTableName { get; }          // "queue_message_{QueueName}"  (computed, backtick-wrapped at use)
    public string ProcessedTableName { get; }        // "queue_processed_{QueueName}" (computed)

    public QueueDefinition(string queueName, IDataConnectionFactory connectionFactory);
    // ctor VALIDATES queueName against the whitelist and THROWS before any SQL exists.
}
```

`ConnectionFactory` is the per-queue connection seam (D4). The host passes the marker
factory for the schema that queue lives in — each queue gets its own schema's factory.
The library does no connection-string management. (The host wires those marker factories
exactly as `reference-roadbed-data.md:36-93` shows, one per schema.)

### 5.2 Handler delegate (D11 — a delegate, not an interface)

```
public delegate Task QueueMessageHandler<T>(QueueMessage<T> message, CancellationToken cancellationToken);
```

The processor's `ProcessBatchAsync` takes this **delegate** (an async `Func`-style
handler the caller supplies). It is intentionally **not** an `IQueueMessageHandler<T>`
interface — the host is free to back the delegate with a DI-resolved service when it
wants, but the library's contract is the delegate.

`QueueMessage<T>` carries `long Id`, `string ExternalId`, `DateTime CreatedOn`,
`T Payload`. A handler that **returns** = success → processed row with
`is_processed_successfully = 1`. A handler that **throws** = failure → processed row
with `is_processed_successfully = 0`, the throw is caught per-message, logged, and the
batch continues (§8). Handlers are expected to be **idempotent** (§8) — documented, not
enforced.

### 5.3 `QueueProcessor<T>` — the enqueue + drain engine

```
public sealed class QueueProcessor<T> : BaseClassWithLogging   // Roadbed.Common base (level-checked logging)
{
    public QueueProcessor(QueueDefinition<T> definition, IDbQueueDataExecutor executor, ILogger<QueueProcessor<T>> logger);
    // internal ctor taking IDbQueueDataExecutor + (optionally) TimeProvider for unit tests across InternalsVisibleTo,
    // per the Roadbed dual-constructor pattern (SKILL.md:79).

    // ENQUEUE (D2): mints external_id (Guid.CreateVersion7), serializes T with RoadbedJson.Options,
    // INSERTs the message row, RETURNS the external_id.
    public Task<string> EnqueueAsync(T payload, CancellationToken cancellationToken = default);

    // PROCESS: claim up to batchSize unprocessed (anti-join, FIFO), deserialize, dispatch, record per-message.
    // Returns a summary (attempted / succeeded / failed counts).
    public Task<QueueProcessResult> ProcessBatchAsync(int batchSize, QueueMessageHandler<T> handler, CancellationToken cancellationToken = default);
}
```

`QueueProcessResult` = `{ int Attempted; int Succeeded; int Failed; }` (for the job's
`this.Context.Result` line + metrics).

### 5.4 Installer

`InstallDbQueueMySql : IServiceCollectionInstaller` (one per assembly,
`reference-roadbed-common.md:24-26, 114-134`) registers
`IDbQueueDataExecutor → MySqlDbQueueDataExecutor` and ends with
`ServiceLocator.SetLocatorProvider(services.BuildServiceProvider())`. **It does NOT
register any `QueueDefinition<T>` or marker factory** — those are host-owned, per queue,
because they carry schema-specific connections (D4). The host constructs each
`QueueProcessor<T>` (or a small typed wrapper) with its definition.

---

## 6. Enqueue path (D2 / D3)

`EnqueueAsync(payload)`:

1. `ArgumentNullException.ThrowIfNull(payload)`.
2. `string externalId = Guid.CreateVersion7().ToString("D")` — canonical lowercase
   hyphenated 36-char (D3). **Not** `Ulid`. `Guid.CreateVersion7()` is .NET 9+ BCL;
   Roadbed targets `net10.0`, so no package add (`Guid.CreateVersion7()` is BCL on this
   target framework).
3. `string json = JsonSerializer.Serialize(payload, RoadbedJson.Options)`.
4. INSERT the message row (`external_id`, `created_on` via DB default, `payload`).
   `created_on` should come from the DB default `UTC_TIMESTAMP(6)` (matches the
   partition-key UTC contract, `install_mysql.txt:36-46`) — the library supplies
   `external_id` + `payload` as **real query parameters**.
5. **Return `externalId`** to the caller — the shareable tracking handle (for a
   confirmation URL, a support lookup, a cross-system correlation id). The internal
   auto-increment `id` is never surfaced.

MySQL has no `RETURNING`; the external_id is **minted in C#** and already known, so
enqueue needs no read-back round-trip — a single parameterized `INSERT` via
`MySqlExecutor.ExecuteAsync` suffices. (Contrast the `LAST_INSERT_ID()` pattern,
`reference-roadbed-data-mysql.md:84-113` — not needed here since the caller wants
`external_id`, not `id`.)

---

## 7. Canonical table-shape contract (D5 / D12) — tables pre-created by the DBA

The library runs **no DDL** and assumes both tables exist with **exactly** this shape.
Per logical queue `{q}` (where `{q}` passed the whitelist, §11):

### 7.1 Message table `queue_message_{q}` — append-only, immutable, never UPDATEd

| Column | Type | Notes |
| ------ | ---- | ----- |
| `id` | `BIGINT UNSIGNED NOT NULL AUTO_INCREMENT` | internal PK, defines FIFO order |
| `external_id` | `CHAR(36) CHARACTER SET ascii COLLATE ascii_bin NOT NULL` | UUIDv7 (D3); shareable handle |
| `created_on` | `DATETIME(6) NOT NULL DEFAULT (UTC_TIMESTAMP(6))` | insert timestamp; **partition key** |
| `payload` | `JSON NOT NULL` | serialized `T` (`RoadbedJson.Options`) |

Keys (every unique key includes the partition column `created_on`, per
`install_mysql.txt:24-26`):
- `PRIMARY KEY (id, created_on)`
- `UNIQUE KEY uk_message_external (external_id, created_on)` — composite (D5b); the
  app guarantees `external_id` uniqueness via UUIDv7, the DB unique only enforces it
  *within a month partition*.

`ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci`,
`PARTITION BY RANGE (TO_DAYS(created_on))` with `p_min` + monthly partitions + `pmax`
(mirror `install_mysql.txt:90-213`).

### 7.2 Processed table `queue_processed_{q}` — "attempted" records (INSERT-only)

| Column | Type | Notes |
| ------ | ---- | ----- |
| `id` | `BIGINT UNSIGNED NOT NULL AUTO_INCREMENT` | PK |
| `fk_queue_id` | `BIGINT UNSIGNED NOT NULL` | **logical** reference to `queue_message_{q}.id`; **NOT** a FK (D5a) |
| `processed_on` | `DATETIME(6) NOT NULL DEFAULT (UTC_TIMESTAMP(6))` | attempt timestamp; **partition key** |
| `is_processed_successfully` | `TINYINT(1) NOT NULL` | `1` = handler succeeded; `0` = handler failed (needs investigation) |

Keys:
- `PRIMARY KEY (id, processed_on)`
- `UNIQUE KEY uk_processed_message (fk_queue_id, processed_on)` — composite (D5b).
  **Weakened to "one row per message per month"** under partitioning (D6): two attempts
  of the same `fk_queue_id` in *different* months would not collide on this key. This is
  acceptable **only because** the app-level anti-join (§8) is the real idempotency guard;
  the DB unique is a backstop within a partition. **Document this in the XML docs** on
  the claim method.
- `KEY idx_processed_fk (fk_queue_id)` — plain index for the anti-join's join column.

`ENGINE=InnoDB … PARTITION BY RANGE (TO_DAYS(processed_on))` with the same `p_min` +
monthly + `pmax` layout.

> **No FK between the tables (D5a):** MySQL/MariaDB forbid foreign keys on partitioned
> InnoDB tables (`install_mysql.txt:28-29`). `fk_queue_id` is a plain indexed integer
> that *means* "the message id" but is not an enforced constraint — it is a **logical
> reference**, not a foreign key.

### 7.3 Reference DDL templates (D12)

The library writes no DDL, but the agent **authors reference `CREATE TABLE` + partition
templates** for the DBA — parameterized on `{q}` — shipped as embedded
`Assets/Tables/queue_message/install_mysql.txt` and
`Assets/Tables/queue_processed/install_mysql.txt`, **structurally identical** to the
`logging.activity` precedent (`src/Roadbed.Logging/Assets/Tables/activity/install_mysql.txt`):
same `p_min` floor / monthly partitions / `pmax` catch-all block, same
`DEFAULT (UTC_TIMESTAMP(6))` UTC defaults, same composite-key rules. This matches the
portfolio norm (agent-authored DDL, human-run; the SQL user has no DDL grant) and
guarantees the DBA's tables match the library's assumed shape. The library itself still
issues **no** CREATE/ALTER/DROP at any time.

**Partitioning settings the templates bake in (mirroring `activity`):**

- **Granularity — MONTHLY** RANGE on `TO_DAYS(created_on)` / `TO_DAYS(processed_on)`,
  exactly as `activity` (`install_mysql.txt:90`). Uniform monthly across all queues.
- **Pre-created horizon — a 10-year run of monthly partitions** (`activity` ships
  `p_202601 … p_203512` = 120 months, `install_mysql.txt:91-211`) plus the `p_min` floor
  and the `pmax`/MAXVALUE catch-all (`install_mysql.txt:91, 212`). Print a header comment
  telling the DBA to add partitions before `pmax` starts absorbing live rows — this
  horizon is an ops-template detail the DBA can extend, not a hard library constraint.
- **Retention window — a 12-month default**, expressed as a header comment in each
  template (the same recommended window `activity` documents,
  `install_mysql.txt:18-20`). The DBA enforces it externally via `DROP PARTITION`
  (§9) and **can override the window per queue** — a longer-retention queue (e.g. a
  legal-record queue) and a shorter-retention high-volume queue are both the DBA's call;
  the templates only print the default.

---

## 8. Core behavior — claim, dispatch, record (D6 / D11)

`ProcessBatchAsync(batchSize, handler, ct)`:

1. **Claim a batch (anti-join, FIFO).** SELECT up to `batchSize` message rows with **no**
   processed row, oldest first:

   ```sql
   SELECT m.id, m.external_id, m.created_on, m.payload
   FROM `queue_message_{q}` AS m
   LEFT JOIN `queue_processed_{q}` AS p ON p.fk_queue_id = m.id
   WHERE p.fk_queue_id IS NULL
   ORDER BY m.id ASC
   LIMIT @BatchSize;
   ```

   `@BatchSize` is a **real query parameter**; `{q}`-derived table names are the **only**
   interpolated, backtick-wrapped, whitelist-validated identifiers (§11). The anti-join
   over the `idx_processed_fk` index is the **real idempotency guard** (D6) — it excludes
   any message that already has a processed row, success or failure, so nothing is
   re-claimed. (Document that this app-level anti-join, not the partition-weakened DB
   unique, is what guarantees once-only processing.)

2. **Deserialize** each `payload` into `T` with `RoadbedJson.Options`.

3. **Dispatch per message** to `handler`, each wrapped in its own try/catch:
   - returns → outcome = success.
   - throws → outcome = failure; catch it, **log at Error** (§9), continue the batch.

4. **Record per message, immediately after each attempt** — INSERT one processed row
   (`fk_queue_id = m.id`, `is_processed_successfully = 1|0`, `processed_on` via DB
   default). **Do NOT** batch all processed-row INSERTs into one end-of-batch commit: a
   mid-batch crash must leave already-attempted messages marked and not-yet-attempted
   ones still eligible next run. One failing message must not stop the rest.

5. **No auto-retry (D8).** A processed row (either flag) excludes the message from all
   future batches. A failed message is reprocessed **only** by external/DBA replay
   (deleting its processed row), which is why handlers must be idempotent.

**Single-consumer assumption (D11).** The processor assumes **one instance per queue at a
time** — the consuming host runs it from a single-worker, non-overlapping scheduled job
(`[DisallowConcurrentExecution]`). There is **no** `SKIP LOCKED`, row-claim lock,
visibility timeout, or in-flight state. **State this assumption in the XML docs on
`QueueProcessor<T>`** so a future multi-consumer move is a flagged change, not a silent
race. (If two processors ran concurrently, both anti-joins could claim the same message;
the per-partition DB unique would let only one processed row land per month, but the
handler would have already run twice.)

**Idempotency note (D11).** Document on `EnqueueAsync`/the handler delegate that handlers
must be idempotent: external replay (processed-row deletion) re-runs a message; the
library does not enforce idempotency.

---

## 9. Failure visibility + retention model (D7 / D8)

**Failed rows must surface.** `is_processed_successfully = 0` means "attempted, handler
failed, needs manual investigation" — **never** auto-retried. A silently-failed
compliance message (e.g. an unsubscribe that never took) is a **legal risk**. Therefore:
- the library **logs every handler failure at Error** via `this.LogError(ex, ...)` with
  the queue name + `external_id` (level-checked base-class method,
  `reference-roadbed-common.md:24, 200-206`), so it reaches the host's
  `Roadbed.Logging` sink and dashboards;
- the `QueueProcessResult.Failed` count is returned so the host's job can set
  `this.Context.Result` / feed metrics;
- **document** (XML + this plan) that the *host* is responsible for alerting on failed
  rows and on **stale-unprocessed** messages: a message still unprocessed when its
  monthly partition reaches the retention boundary is **silently dropped** on
  `DROP PARTITION`. Monitoring must catch a backlog before the boundary. The library
  cannot see this (it does no retention) — it is called out as a host monitoring
  requirement.

**Retention is external/DBA, message-table-FIRST (D7).** Retention = the DBA dropping
month partitions older than the window (the `logging.activity` model,
`install_mysql.txt:18-21`). The reference DDL prints a **12-month default** window
(§7.3); the DBA may set a longer or shorter window per queue. **Order matters:** drop the
**message** partition first,
then the **processed** partition. Rationale to document: if you drop a month's
**processed** rows while the corresponding **message** rows survive, those messages lose
their processed row and the anti-join makes them **re-claimable → reprocessed**. Dropping
the **message** partition first removes them from the claim pool, so the orphaned
processed rows that remain are harmless until their own partition is dropped. The library
performs **no** retention — this is documented guidance for the DBA scripts only.

---

## 10. Model divergence note (D9)

This **immutable-message + processed-companion** two-table model is deliberately
different from the portfolio's existing **single-table-with-status** queues (one row per
item, mutated through a status column). New queues should use **this** model; existing
bespoke status-column queues stay as they are unless separately migrated. Note this in
the library README so a future maintainer understands the two coexisting patterns and
does not "unify" them by accident.

---

## 11. Safety — table-name whitelist + parameterization (D11)

MySQL cannot parameterize **table identifiers**, so table names are composed into SQL
text. To make that safe:

- The queue name is **library-/host-controlled, never user-supplied**, but still
  **validated against a strict whitelist** at `QueueDefinition<T>` construction:
  lowercase ASCII letters, digits, underscore only (`^[a-z0-9_]+$`), bounded length
  (recommend ≤ 48 so `queue_processed_{q}` stays well under MySQL's 64-char identifier
  limit). **Reject (throw `ArgumentException`) before any SQL is built.**
- The derived table names `queue_message_{q}` / `queue_processed_{q}` are the **only**
  values interpolated into SQL, and they are **backtick-wrapped** as defense-in-depth.
- **Every** non-identifier value — `payload`, `external_id`, `batchSize`, `fk_queue_id`,
  the success flag — uses a **real query parameter** (`@Param`), never interpolation.
- Unit-test the validator: valid names accepted; uppercase / hyphen / space / `;` /
  backtick / over-length / empty all rejected at construction.

---

## 12. Acceptance criteria

A new queue requires **only**: (1) the DBA creates the two convention-named, partitioned
tables (§7) and (2) the host constructs a `QueueDefinition<T>` + a `QueueProcessor<T>` and
registers a handler — **no change to the library core**. Plus:

1. `EnqueueAsync(payload)` mints a UUIDv7 `external_id`, INSERTs the message row, and
   **returns the `external_id`**.
2. Exactly **one** processed row per attempted message, with the correct
   `is_processed_successfully` flag.
3. A throwing handler → message marked attempted with `is_processed_successfully = 0`,
   logged at Error, **does not stop the batch**, **not** auto-retried.
4. Already-processed (success or failure) messages are **never** re-selected
   (anti-join).
5. FIFO order — ascending `id`.
6. The library does **no** DDL and **no** `UPDATE`/`DELETE` on either table — only
   `INSERT` into message (enqueue) + `INSERT` into processed (record) + `SELECT` (claim).
7. Invalid queue names are **rejected at `QueueDefinition<T>` construction** (throw),
   before any SQL is built.
8. Tables are partitioned with composite keys per §7; the library composes SQL that
   prunes on the partition column where it filters.

---

## 13. Verification (build + unit tests only; no live checks)

- `dotnet build "src/Roadbed .NET Solution.slnx"` → **0 warnings / 0 errors** (warnings
  are errors; XML docs mandatory).
- `dotnet test "src/Roadbed.Test.Unit/Roadbed.Test.Unit.csproj"` → **green.**
- Unit tests (MSTest, Roy-Osherove, try/catch for throws, specialized asserts —
  `claude-project-instructions.md:20-356`) cover at least: queue-name validator
  (accept/reject matrix); `external_id` is a 36-char UUIDv7 "D" string; enqueue
  serializes with `RoadbedJson.Options`; the anti-join SQL excludes
  already-processed; per-message recording inserts one row with the right flag; a
  throwing handler doesn't stop the batch and records `0`; FIFO ordering. Provider-
  agnostic processor logic should be testable without a live MySQL (e.g. against the
  `IDbQueueDataExecutor` port with a fake, or a future `.Sqlite` adapter; **do not**
  introduce a live-DB requirement into `Roadbed.Test.Unit`).
- **No** live host boot, **no** `C:\Secrets` read, **no** real-DB check is run by the
  agent as proof — those are maintainer-gated and handed back.

## 14. House rules + agent guardrails

- `Roadbed.*` libraries are consumed elsewhere as **vendored DLLs** — but **this work IS
  inside the Roadbed repo**, so these are normal `ProjectReference`s within the solution;
  the "no `<PackageReference>` for `Roadbed.*`" rule applies to *consumer* repos, not to
  Roadbed itself.
- **Commit policy (G):** leave the work in `main`, **uncommitted** — no commit, branch,
  or push. The maintainer reviews the diff in VS. This is the standing portfolio rule;
  Roadbed documents no commit policy of its own, so this default applies.
- **No `TODO`/`FIXME`** (Sonar S1135 + `TreatWarningsAsErrors`).
- `System.Text.Json` with `Roadbed.RoadbedJson.Options` on all JSON — never Newtonsoft,
  never per-call options (`SKILL.md:44, 55-56`).
- `this.`-prefixed instance members; `ArgumentNullException.ThrowIfNull` /
  `ArgumentException.ThrowIfNullOrWhiteSpace` validation; `CancellationToken` last;
  `#region` blocks; XML docs on every public/protected member; level-checked
  `this.LogX` methods (inherit `BaseClassWithLogging`); `IServiceCollectionInstaller` +
  `ServiceLocator.SetLocatorProvider` for registration; dual-constructor pattern on the
  public service (`SKILL.md:38-83`).
- Required reading named in the prompt: the `code-roadbed-csharp` skill, especially
  `references/reference-roadbed-data.md` + `reference-roadbed-data-mysql.md` +
  `reference-roadbed-common.md` (+ `reference-roadbed-scheduling.md` for context on the
  out-of-scope job boundary).
- Quote build paths containing spaces (`"src/Roadbed .NET Solution.slnx"`).
