# Roadbed.DbQueue.MySql

MySQL/MariaDB provider satellite for [`Roadbed.DbQueue`](../Roadbed.DbQueue/README.md).

## What it ships

- **`MySqlDbQueueDataExecutor`** — a thin adapter over `MySqlExecutor` that fulfils the core's internal `IDbQueueDataExecutor` port. One singleton serves every queue in the host; the per-queue connection factory arrives via each method call from `QueueDefinition<T>.ConnectionFactory`.
- **`InstallDbQueueMySql`** — auto-discovered `IServiceCollectionInstaller` that registers the executor and captures the service provider into `ServiceLocator` so the public `QueueProcessor<T>` constructor can resolve it.
- **Reference DDL templates** under `Assets/Tables/queue_message/install_mysql.txt` and `Assets/Tables/queue_processed/install_mysql.txt` — partitioned `CREATE TABLE` scripts authored for the DBA. The library itself runs no DDL; replace the literal `{q}` placeholder with the queue's logical name and the DBA runs the scripts against the business schema that hosts the queue.
- **One-time upgrade script** at `Assets/Tables/upgrade_2026-06_external_id_varchar_mysql.txt` — `ALTER` the `external_id` column on already-deployed queues from `CHAR(36)` to `VARCHAR(36)`. Required only for hosts that deployed from the original template (which shipped `CHAR(36)`); new deployments don't need it.

Reference exactly one Roadbed.DbQueue provider package per host.

## Host wiring

1. Add a project / package reference to `Roadbed.DbQueue.MySql`.
2. Reference `Roadbed.DbQueue` for `QueueDefinition<T>` / `QueueProcessor<T>`.
3. Register the per-schema `IFooDatabaseFactory` marker (the standard Roadbed.Data marker pattern) for whichever business schema each queue lives in.
4. Have the DBA create `queue_message_{q}` + `queue_processed_{q}` from the templates against the matching schema(s).
5. In your scheduled job (`BaseSchedulingJob<T>`, marked `[DisallowConcurrentExecution]`), construct a `QueueProcessor<T>` with `new QueueDefinition<T>("{q}", fooDatabaseFactory)` and call `ProcessBatchAsync(batchSize, handler, ct)`.

The host owns the `QueueDefinition<T>` registrations because each queue carries a schema-specific connection factory.

## Storage contract for `external_id`

`queue_message_{q}.external_id` is **`VARCHAR(36) CHARACTER SET ascii COLLATE ascii_bin NOT NULL`** — not `CHAR(36)`. The library surfaces `ExternalId` as `string` end-to-end (`EnqueueAsync` return value, `QueueMessage<T>.ExternalId`), so the column must always materialize as a string regardless of the consumer's `MySqlConnector` `GuidFormat` connection-string option.

`GuidFormat=Char36` is MySqlConnector's **default**. Under that setting the driver auto-coerces `CHAR` columns declaring exactly 36 characters to `System.Guid` — which would crash the library's claim path with a Dapper `Convert.ChangeType(Guid → string)` failure. `VARCHAR` uses a different MySQL protocol type code (`MYSQL_TYPE_VAR_STRING` vs `MYSQL_TYPE_STRING`) and falls into a different `TypeMapper` branch, so it is never coerced.

UUIDv7 lexical-equals-chronological ordering and the composite `UNIQUE (external_id, created_on)` work identically under `VARCHAR(36) ascii ascii_bin` and `CHAR(36) ascii ascii_bin`. **Do not "fix" the column back to `CHAR(36)`** under the assumption that it's more idiomatic — the choice is deliberate.

## Retention reminder

The library performs no `DROP PARTITION`. The DBA drops month partitions older than the configured window — **message-table FIRST, processed-table SECOND**. The reverse order orphans messages back into the claim pool and re-runs the handler. Both templates print this guidance in their header.
