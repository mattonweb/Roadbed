# Roadbed.DbQueue

Provider-agnostic core for a database-backed queue: enqueue a typed payload, drain it later in a single-consumer scheduled job.

See [`Roadbed.DbQueue.MySql`](../Roadbed.DbQueue.MySql/README.md) for the MySQL/MariaDB satellite that supplies the executor and reference DDL templates.

## What it is

- **Generic over a payload type `T`** — a `QueueDefinition<T>` binds a queue name to its payload type and the connection that reaches the schema where its tables live.
- **`QueueProcessor<T>.EnqueueAsync(payload)`** mints a UUIDv7 external id, inserts the row, and returns the external id for tracking. The internal auto-increment id is never surfaced.
- **`QueueProcessor<T>.ProcessBatchAsync(batchSize, handler, ct)`** claims up to `batchSize` unprocessed messages in FIFO order via a LEFT JOIN anti-join, hands each one to the caller-supplied `QueueMessageHandler<T>` delegate, and records one processed row per attempt — success or failure — immediately.
- **No auto-retry.** A processed row excludes the message forever. A failed message is reprocessed only by an operator deleting its processed row externally; handlers must therefore be idempotent.

## Architectural model

This library uses the **immutable-message + processed-companion two-table model**. New queues should use this model; existing single-table-with-status queues in the portfolio stay as they are unless separately migrated. Do not "unify" the two patterns without a deliberate migration.

## Public surface

| Type | Kind | Purpose |
| ---- | ---- | ------- |
| `QueueDefinition<T>` | sealed class | Validated queue name + per-queue `IDataConnectionFactory`. |
| `QueueProcessor<T>` | sealed class | Enqueue + drain engine. Public + internal ctor; inherit `BaseClassWithLogging`. |
| `QueueMessage<T>` | sealed class | What the handler delegate receives: id, external id, created-on, payload. |
| `QueueMessageHandler<T>` | delegate | `Task QueueMessageHandler<T>(QueueMessage<T>, CancellationToken)`. |
| `QueueProcessResult` | sealed class | Attempted / Succeeded / Failed counts for the job's metrics + result line. |

`IDbQueueDataExecutor` is internal — a provider satellite implements it and registers it before `QueueProcessor<T>` is constructed.

## What this library does NOT do

- **No DDL.** No `CREATE`, `ALTER`, or `DROP` at any time. Tables are pre-created by the DBA from the reference templates shipped in the satellite.
- **No Quartz / scheduling dependency.** Run `ProcessBatchAsync` from a `BaseSchedulingJob<T>` in the consuming host; the library ships no job classes.
- **No `Roadbed.Messaging` dependency.** Cloud-broker envelopes belong in a different library.
- **No retention.** Month partitions are dropped externally by the DBA, message-table-first then processed-table.

## Single-consumer assumption

The processor assumes one instance per queue is running at a time. Run it from a `[DisallowConcurrentExecution]` job. There is no `SKIP LOCKED`, no visibility timeout, no claim lock. Moving to multi-consumer is a flagged design change, not a silent reconfiguration.
