# Roadbed.DbQueue.MySql

MySQL/MariaDB provider satellite for [`Roadbed.DbQueue`](../Roadbed.DbQueue/README.md).

## What it ships

- **`MySqlDbQueueDataExecutor`** — a thin adapter over `MySqlExecutor` that fulfils the core's internal `IDbQueueDataExecutor` port. One singleton serves every queue in the host; the per-queue connection factory arrives via each method call from `QueueDefinition<T>.ConnectionFactory`.
- **`InstallDbQueueMySql`** — auto-discovered `IServiceCollectionInstaller` that registers the executor and captures the service provider into `ServiceLocator` so the public `QueueProcessor<T>` constructor can resolve it.
- **Reference DDL templates** under `Assets/Tables/queue_message/install_mysql.txt` and `Assets/Tables/queue_processed/install_mysql.txt` — partitioned `CREATE TABLE` scripts authored for the DBA. The library itself runs no DDL; replace the literal `{q}` placeholder with the queue's logical name and the DBA runs the scripts against the business schema that hosts the queue.

Reference exactly one Roadbed.DbQueue provider package per host.

## Host wiring

1. Add a project / package reference to `Roadbed.DbQueue.MySql`.
2. Reference `Roadbed.DbQueue` for `QueueDefinition<T>` / `QueueProcessor<T>`.
3. Register the per-schema `IFooDatabaseFactory` marker (the standard Roadbed.Data marker pattern) for whichever business schema each queue lives in.
4. Have the DBA create `queue_message_{q}` + `queue_processed_{q}` from the templates against the matching schema(s).
5. In your scheduled job (`BaseSchedulingJob<T>`, marked `[DisallowConcurrentExecution]`), construct a `QueueProcessor<T>` with `new QueueDefinition<T>("{q}", fooDatabaseFactory)` and call `ProcessBatchAsync(batchSize, handler, ct)`.

The host owns the `QueueDefinition<T>` registrations because each queue carries a schema-specific connection factory.

## Retention reminder

The library performs no `DROP PARTITION`. The DBA drops month partitions older than the configured window — **message-table FIRST, processed-table SECOND**. The reverse order orphans messages back into the claim pool and re-runs the handler. Both templates print this guidance in their header.
