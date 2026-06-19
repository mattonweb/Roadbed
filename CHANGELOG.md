# Changelog

## Unreleased

### Added

- Add `DapperDateOnlyHandler` and `DapperNullableDateOnlyHandler` for
  SQLite TEXT / MariaDB DATE round-trip of `DateOnly` properties.
  Consumers must register both via `SqlMapper.AddTypeHandler` (same
  pattern as the existing DateTime handlers).

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
