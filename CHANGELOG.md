# Changelog

## Unreleased

### Fixed

- `LoggingActivityRepository.FindStaleAsync` no longer throws
  `System.Data.DataException: ... Object must implement IConvertible`
  when a MySQL-hosted Roadbed.Logging sweep runs the startup stale-reap.
  The bug surfaced after the UUIDv7 column widen: with
  `GuidFormat=Char36` on the connection string MySqlConnector
  materializes the CHAR(36) `id` column as `System.Guid`, and Dapper
  could not downcast a Guid back into the `QueryAsync<string>` the
  read declared. The read now uses a private typed POCO with an
  `object?` `Id` property and projects each row to a canonical
  lowercase 8-4-4-4-12 string via a runtime-type switch, so it works
  uniformly against MySQL (Guid materialization) and SQLite (string
  materialization). The public `IReadOnlyList<string>` contract is
  preserved and the format `FindStale -> Reap` round-trips through
  `id IN @ActivityIds` byte-for-byte.

### Added

- Refactor Roadbed.Messaging off the Cysharp `Ulid` package onto the
  BCL-native `Guid.CreateVersion7()` (.NET 9+). The five id-generation
  sites in `BaseMessagingMessage` and `MessagingPublisher` now mint
  UUIDv7s via `Guid.CreateVersion7().ToString()` (canonical lowercase
  hyphenated 8-4-4-4-12 form); the constructors that accept an
  explicit identifier parameter are unchanged. The `Ulid 1.4.1`
  PackageReference is removed from `Roadbed.Messaging.csproj`.
  Identifier wire format widens from 26 to 36 characters; chronological
  sortability is preserved because UUIDv7's first 48 bits are a
  big-endian millisecond timestamp. XML doc remarks, the README's
  benefits section + JSON samples + property tables + Requirements
  line, and the `code-roadbed-csharp` skill's messaging reference now
  name UUIDv7. Tests in `src/Roadbed.Test.Unit/Messaging/` converted
  to `Guid.CreateVersion7()` / `Guid.TryParse(...)`, including the
  `MessagingPublisher` Identifier length assertion (26 → 36). Last
  `Ulid` usage anywhere in Roadbed.
- Add `DapperDateOnlyHandler` and `DapperNullableDateOnlyHandler` for
  SQLite TEXT / MariaDB DATE round-trip of `DateOnly` properties.
  Consumers must register both via `SqlMapper.AddTypeHandler` (same
  pattern as the existing DateTime handlers).
- Widen Roadbed.Logging activity-id columns from `CHAR(26)` to
  `CHAR(36)` so the schema accepts a 36-character UUIDv7
  (`Guid.CreateVersion7()`). Six columns updated across the three
  install MySQL DDL assets — `activity.id` / `parent_activity_id` /
  `root_activity_id`, `activity_input.activity_id` /
  `input_activity_id`, `log_entries.activity_id`. `ascii_bin` collation
  is preserved: UUIDv7's first 48 bits are a big-endian millisecond
  timestamp, so the hex string still sorts chronologically. SQLite
  assets already type these as `TEXT` (no width cap) — only the
  inline collation comment was updated. A consolidated MySQL upgrade
  script (`Assets/Tables/upgrade_2026-06_uuidv7_widen_mysql.txt`)
  widens the six columns in place via `ALTER ... MODIFY`. Public C#
  surface is unchanged — activity ids are already `string` end-to-end
  and Roadbed never generates them — but the XML doc comments and
  README example now name UUIDv7 instead of ULID.
- Inject `System.TimeProvider` into the in-process clock paths that
  stamp framework timestamps and time retry / backoff waits.
  `LoggingActivityService`, `LoggingActivityRepository`, and
  `LogWriterHostedService` now stamp `created_on` /
  `last_modified_on` / flush-interval timing via the injected
  `TimeProvider`; `NetHttpClient` virtualizes its retry backoff
  through `Task.Delay(TimeSpan, TimeProvider, CancellationToken)`.
  Every Roadbed installer registers `TimeProvider.System` as a
  `TryAddSingleton`, so production behavior is identical and consumer
  tests can supply a `FakeTimeProvider` to drive every framework
  timestamp and every backoff wait from one virtual clock. Non-breaking;
  the parameterless `NetHttpClient` constructor and the
  `LoggingActivityService` `ServiceLocator` fallback both default to
  `TimeProvider.System`.

### Breaking

- **Replaced Newtonsoft.Json with System.Text.Json across every Roadbed
  project.** The framework now reads and writes JSON through the shared
  `Roadbed.RoadbedJson.Options` (`System.Text.Json.JsonSerializerOptions`).
  The serialization contract changes:
  - **Attribute swap.** DTOs that bind to Roadbed-deserialized JSON
    (e.g. response types passed to
    `NetHttpClient.MakeHttpRequestAsync<T>`, messaging envelopes) must
    declare names with
    **`[System.Text.Json.Serialization.JsonPropertyName]`**.
    Newtonsoft's `[JsonProperty]` is silently ignored — DTOs that still
    use it will produce default/null values instead of a compile error.
  - **Case-insensitive reads.** `PropertyNameCaseInsensitive = true` is
    set globally to match Newtonsoft's prior behavior. Without it, STJ's
    case-sensitive default is the most common silent break.
  - **Lenient number reads.** `NumberHandling.AllowReadingFromString` is
    set globally so upstream APIs that emit numbers as JSON strings
    continue to bind. Trailing commas and JSON comments are tolerated on
    reads.
  - **Null-output shape.** `DefaultIgnoreCondition.WhenWritingNull` is
    set globally to preserve the null-omission shape Roadbed DTOs
    previously declared per-property with `NullValueHandling.Ignore`.
    `null` properties are not emitted in the framework's serialized
    output.
  - **`Newtonsoft.Json` PackageReference removed** from every Roadbed
    project. Consumers who pulled it transitively must add their own
    reference if they still need it for their own code.

### Migration

- DTOs you pass to `NetHttpClient.MakeHttpRequestAsync<T>` or to the
  messaging entities must annotate their JSON-bound properties with
  `[System.Text.Json.Serialization.JsonPropertyName("…")]`.
- If you previously deserialized Roadbed JSON with Newtonsoft on the
  consumer side, switch to `System.Text.Json` and pass
  `Roadbed.RoadbedJson.Options` to ensure the same lenient-read +
  null-omission behavior.
