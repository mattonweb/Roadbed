# Plan: Roadbed.IO.Xlsx (+ Roadbed.Net streaming download)

Status: proposed — ready for implementation.
Audience: the Roadbed coding agent.

A new `Roadbed.IO.Xlsx` library for ingesting **large** Excel workbooks
(hundreds of MB, hundreds of thousands to millions of rows) sourced from
government sites, without loading the whole file into memory. It pairs with a
small new **streaming download** capability on `Roadbed.Net` so the end-to-end
flow is: download the binary to a local file → stream-read rows forward-only →
feed them into a sink (typically the bronze bulk insert) in batches.

The driving constraint is **bounded memory**. The existing `Roadbed.IO.Csv`
library buffers every row into an in-memory `IList<T> DataRows`; this library
deliberately does **not** — it is streaming-first.

---

## 1. Why this exists (context)

These were settled in design discussion and must be honored, not re-litigated:

1. **Download to disk, then stream.** `.xlsx` is a ZIP archive of XML parts.
   Reading a ZIP requires a **seekable** stream (the central directory is at the
   end of the file). You cannot parse an `.xlsx` off a forward-only HTTP
   response stream. The two ways to get a seekable source are a `MemoryStream`
   (holds the whole compressed file in RAM — defeats the purpose) or a temp
   **file on disk** (`FileStream` — RAM stays bounded). Disk wins.
2. **Streaming read library = Sylvan.Data.Excel** (MIT). It exposes a
   forward-only `DbDataReader`, is the fastest/lowest-allocation .NET Excel
   reader, supports `.xlsx`, `.xlsb`, and `.xls`, and its reader-driven shape
   mirrors how `Roadbed.IO.Csv` uses CsvHelper's `CsvReader`. Explicitly
   rejected: **EPPlus** (Polyform Noncommercial license — paid for commercial
   use), **ClosedXML** / Open XML SDK DOM mode / NPOI `XSSFWorkbook` (all load
   the whole workbook into memory). ExcelDataReader is an acceptable fallback
   but is slower/higher-allocation and more dated.
3. **Read/stream only for v1.** No write/export surface (unlike CSV). Add later
   only if a real need appears.
4. **Sheet selection and header/banner handling are required.** Government
   files routinely have a chosen sheet by name, preamble/banner rows above the
   header, and sometimes multi-row headers.

### Known memory floor

Like every `.xlsx` reader, Sylvan reads the workbook's **shared-strings table**
into memory. For typical large gov files this is far smaller than the row data
and is acceptable. Only a pathological shared-strings table would justify the
Open XML SDK SAX route (`OpenXmlReader`) — out of scope here; note it as the
escape hatch.

---

## 2. Architecture overview

Two independently-shippable pieces:

```
Roadbed.Net      : DownloadFileAsync(NetHttpDownloadRequest)  → streams body to a local file
Roadbed.IO.Xlsx  : IoXlsxFile<T> + IXlsxEntityMapper<T>       → forward-only IAsyncEnumerable<T>
```

End-to-end consumer flow (bounded memory throughout):

```csharp
// 1. Download to a local file (streaming, never buffers the whole body in RAM).
var dl = await httpClient.DownloadFileAsync(new NetHttpDownloadRequest
{
    HttpEndPoint = new Uri(sourceUrl),
    DestinationPath = localPath,
    TimeoutInSecondsPerAttempt = 300,
}, ct);

// 2. Stream rows forward-only and feed the bronze bulk insert in batches.
var xlsx = IoXlsxFile<PlaceRow>.FromFile(localPath, new PlaceRowMapper(), new IoXlsxReadOptions
{
    SheetName = "Places",
    SkipLeadingRows = 3,   // banner rows above the header
    HasHeaders = true,
});

await foreach (PlaceRow row in xlsx.ReadRowsAsync(ct))
{
    buffer.Add(row);
    if (buffer.Count >= batchSize) { await bronze.BulkInsertAsync(activityId, buffer, ct); buffer.Clear(); }
}
if (buffer.Count > 0) await bronze.BulkInsertAsync(activityId, buffer, ct);
```

---

## 3. Deliverables

### 3.1 Roadbed.Net — streaming download

**New request type — subclass, not a parallel type.** A download needs
everything `NetHttpRequest` already carries (endpoint, headers, auth, retry
pattern, compression, timeout) plus a few download-only fields. Subclassing
means the existing `CreateHttpRequestMessage(NetHttpRequest)` works unchanged.

```csharp
public class NetHttpDownloadRequest : NetHttpRequest
{
    public string DestinationPath { get; set; } = string.Empty;
    public bool Overwrite { get; set; } = true;
    public int BufferSizeBytes { get; set; } = 81920;   // Stream.CopyToAsync default
    public bool ComputeContentHash { get; set; } = true; // SHA-256, computed while copying
    // Constructor sets a larger default TimeoutInSecondsPerAttempt (e.g. 300) —
    // the base 15s aborts a healthy large download.
}

public sealed class NetHttpDownloadResult
{
    public string FilePath { get; init; } = string.Empty;
    public long BytesWritten { get; init; }
    public string? ContentType { get; init; }
    public string? ContentSha256 { get; init; }          // null when ComputeContentHash = false
}
```

**New method** on `INetHttpClient` / `NetHttpClient`, returning the same
envelope as the rest of the library for uniform success/failure handling:

```csharp
Task<NetHttpResponse<NetHttpDownloadResult>> DownloadFileAsync(
    NetHttpDownloadRequest request,
    CancellationToken cancellationToken = default);
```

**Internal refactor (required) — the retried unit is the *whole* attempt,
headers + body copy.** `MakeRequestWithBackoffRetryAsync` hardcodes
`HttpCompletionOption.ResponseContentRead` (buffers the entire body) and
disposes the `HttpClient` on return. A streaming download needs
`ResponseHeadersRead`, must copy the content to disk *while the response and
client are still alive*, and — critically — must **retry a mid-body drop**, the
single most common failure on a hundreds-of-MB transfer. So the copy cannot
happen after the retry loop returns; it must run *inside* the retried region.

Extract the retry/backoff/timeout/socket skeleton to take a per-attempt
"consume the response" delegate that runs **inside** the try, so both paths
retry the same way:

```csharp
private async Task<T> ExecuteWithBackoffRetryAsync<T>(
    NetHttpRequest request,
    HttpCompletionOption completion,
    Func<HttpResponseMessage, CancellationToken, Task<T>> consumeAsync,
    CancellationToken ct);
```

- `MakeHttpRequestAsync` → `completion = ResponseContentRead`; `consumeAsync`
  reads the string / deserializes (unchanged behavior).
- `DownloadFileAsync` → `completion = ResponseHeadersRead`; `consumeAsync`
  (re)creates the `.part` file and copies the body to it. An `IOException` /
  `HttpRequestException` thrown **mid-copy** propagates into the loop's existing
  catch and triggers backoff + retry; on each new attempt the delegate
  truncates/recreates `.part` and restarts from byte 0 (HTTP Range/resume is out
  of scope — a clean restart is sufficient).
- The **per-attempt timeout must enclose the copy**, not just the header read —
  i.e. the `.WaitAsync(timeout)` / linked-CTS wraps send **and** `consumeAsync`,
  so a stalled body copy cancels and retries rather than hanging or hard-failing.

**Content hash computed in the single copy pass.** When `ComputeContentHash` is
true, tee the bytes through SHA-256 *as they are written to `.part`* — e.g. wrap
the destination `FileStream` in a `CryptoStream(fileStream, sha256,
CryptoStreamMode.Write)` and `CopyToAsync` into that, then `FlushFinalBlock()`
and read `sha256.Hash`. Never re-read the finished file to hash it — a second
full read of a 500 MB file defeats the streaming design. The hash is computed
per attempt; only the successful attempt's hash is returned. This value is the
reproducibility/provenance anchor for the live-fetch model (the consumer records
`ContentSha256` on the activity row before deleting the temp file).

**Atomic write.** Copy to `DestinationPath + ".part"`, then move into place on
success, so a failed/retried download never leaves a truncated file that looks
complete. Honor `Overwrite`; ensure the destination directory exists; delete the
`.part` file on failure.

Tests (Roadbed.Test.Unit, mocked `IHttpClientFactory` / handler):
- Successful download writes the expected bytes to `DestinationPath`, returns
  `Success` with `BytesWritten`/`ContentType`.
- Streaming: the handler returns a stream; assert the body is not read into a
  single in-memory buffer before writing (write occurs via `CopyToAsync`).
- `ContentSha256` matches a known SHA-256 of the payload and is computed in one
  pass (no second read of the file); `null` when `ComputeContentHash = false`.
- **Mid-body drop retries**: a handler that throws partway through the body on
  attempt 1 and succeeds on attempt 2 produces a complete, correct file (and the
  `.part` from attempt 1 did not survive).
- Retriable status (503/504/408) retries per `RetryPattern`, then succeeds.
- Failure leaves no partial file at `DestinationPath` (the `.part` is cleaned up).
- `Overwrite = false` against an existing file fails cleanly.

### 3.2 Roadbed.IO.Xlsx — project + API

New project `src/Roadbed.IO.Xlsx`, mirroring `Roadbed.IO.Csv` conventions:
`net10.0`, `ImplicitUsings`, `Nullable`, `GenerateDocumentationFile`, the four
analyzers, `InternalsVisibleTo` for `Roadbed.Test.Unit` /
`Roadbed.Test.Integration` / `DynamicProxyGenAssembly2`, and
**`<RootNamespace>Roadbed.IO</RootNamespace>`** (same quirk as IO.Csv — public
types live in `Roadbed.IO`). References `Roadbed.IO` + `Sylvan.Data.Excel`.

**Public surface** (all in namespace `Roadbed.IO`):

```csharp
public interface IXlsxEntityMapper<out T>
{
    // Reader is positioned on a data row; mapper reads by ordinal or by
    // GetOrdinal("ColumnName") when HasHeaders is true. Mirrors ICsvEntityMapper<T>.
    T? MapEntity(System.Data.Common.DbDataReader reader);
}

public sealed class IoXlsxReadOptions
{
    public string? SheetName { get; set; }       // null => use SheetIndex
    public int SheetIndex { get; set; }          // default 0 (first sheet)
    public int SkipLeadingRows { get; set; }     // banner/preamble rows above the header
    public bool HasHeaders { get; set; } = true;
}

public sealed class IoXlsxFile<T> : IoFile
{
    public static IoXlsxFile<T> FromFile(string path, IXlsxEntityMapper<T> mapper, IoXlsxReadOptions? options = null);

    // Streaming-first: forward-only, bounded memory. NO DataRows buffer.
    public IAsyncEnumerable<T> ReadRowsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default);

    // Convenience: same stream, pre-batched, so consumers skip the manual
    // buffer + trailing-flush boilerplate (the most-forgotten line in the
    // ingest loop). Thin wrapper over ReadRowsAsync.
    public IAsyncEnumerable<IReadOnlyList<T>> ReadBatchesAsync(
        int batchSize, [EnumeratorCancellation] CancellationToken cancellationToken = default);
}
```

Design notes:
- `MapEntity` takes Sylvan's `DbDataReader` (the base type), keeping the mapper
  decoupled from the concrete Sylvan reader and matching the CSV mapper idiom.
- **`[EnumeratorCancellation]`** on the `CancellationToken` of both async
  iterators — without it the token does not flow into `await foreach` and
  cancellation silently no-ops.
- `ReadRowsAsync` is an `async` iterator that yields per row and skips `null`
  mapper results (a mapper may filter). It opens the `FileStream` and the Sylvan
  reader, disposes both on enumeration completion/cancellation.
- `ReadBatchesAsync` accumulates into a `List<T>` of `batchSize`, yields the
  list, then starts a fresh one; it flushes a final partial batch on completion.
- Keep `IoXlsxFile<T> : IoFile` for convention parity even though v1 doesn't use
  the inherited `Save` surface.

**Package collision guard.** Sylvan's reader type is literally
`Sylvan.Data.Excel.ExcelDataReader` — the *same type name* as the rejected
`ExcelDataReader` NuGet package. Pin the `Sylvan.Data.Excel` PackageReference
explicitly (exact version) and **fully-qualify the namespace**
(`Sylvan.Data.Excel.ExcelDataReader`) at every use site so the wrong package
can't be pulled in. Sylvan stays confined to `Roadbed.IO.Xlsx`; core
`Roadbed.IO` gains no Excel dependency.

**Sheet + header handling against Sylvan** (confirm exact option/property names
against the pinned Sylvan version at implementation time):
- Open via `Sylvan.Data.Excel.ExcelDataReader.Create(path, options)`.
- Header row → `ExcelDataReaderOptions.Schema` with `hasHeaders` from
  `IoXlsxReadOptions.HasHeaders` (drives `GetOrdinal("ColumnName")`).
- Sheet selection → worksheets are result-sets: `WorksheetCount`,
  `WorksheetName`, `NextResult()`. By name → advance until `WorksheetName`
  matches (throw a clear exception if not found); by index → advance N times.
- `SkipLeadingRows` → Sylvan has no native "start at row N"; implement as
  read-and-discard (`reader.Read()`).

> **MUST-VERIFY before shipping the headered+banner combination.** Sylvan binds
> the header that powers `GetOrdinal("ColumnName")` from the sheet's **first
> physical row at reader creation**. If banner/preamble rows precede the real
> header, discarding rows *after* creation is too late — ordinals were already
> bound from a banner row and name-based mapping is wrong. Confirm whether Sylvan
> can bind the header *after* skipping N rows.
> - If **yes** → `SkipLeadingRows` + `HasHeaders = true` works as written.
> - If **no** → change the contract: headered reads require the header on the
>   sheet's first row (validate `SkipLeadingRows == 0` when `HasHeaders == true`
>   and throw otherwise); files with banner rows use `HasHeaders = false` +
>   ordinal mapping. Do **not** ship headered+banner as "just works" unverified.

Tests (Roadbed.Test.Unit; commit a few small `.xlsx` fixtures — see
`.gitattributes` deliverable in §3.3 — or generate them in a fixture helper):
- Maps a simple single-sheet workbook with headers to typed rows.
- `SheetName` selects the right sheet; unknown sheet name throws a clear error.
- `SheetIndex` selects by position.
- `HasHeaders = false` maps by ordinal.
- Header + `SkipLeadingRows` behaves per the verified contract above (either the
  combination maps correctly, or it throws the documented validation error).
- A mapper returning `null` filters that row out of the stream.
- `ReadBatchesAsync` yields full batches plus a final partial batch; total row
  count matches `ReadRowsAsync`.
- Large-ish fixture asserts streaming semantics (rows are yielded; the whole
  sheet is not materialized) — at minimum, that enumeration is lazy.

### 3.3 Solution, repo, + docs wiring

- Add the new project to `src/Roadbed .NET Solution.slnx` (`Roadbed.IO.Xlsx`; the
  `Roadbed.Net` change is in-place).
- **`.gitattributes` — mark Office formats binary.** The committed `.xlsx` test
  fixtures are not covered today. Add `*.xlsx`, `*.xlsb`, `*.xls`, `*.docx`,
  `*.pptx` to the binary list (alongside the existing `*.zip` etc.) in the repo's
  `.gitattributes` and the shared template. `text=auto` usually auto-detects
  them, but explicit is safer and prevents EOL normalization from corrupting a
  fixture. Do this **before** committing any fixture.
- New reference doc `skills/code-roadbed-csharp/references/reference-roadbed-io-xlsx.md`
  mirroring the `reference-roadbed-io-csv.md` structure (type catalog, MUST,
  MUST NOT, patterns, pitfalls, quick reference), with the end-to-end
  download→stream→bulk-insert recipe and an explicit callout that this library
  is streaming-first (no `DataRows` buffer, unlike IO.Csv). The pitfalls section
  **must** document:
  - **Numeric-cell leading zeros.** Excel stores FIPS/ZIP codes as numbers, so
    `01001` round-trips as `1001` or `1`. Mappers for such columns must read the
    cell **as a string and re-pad** (e.g. `PadLeft(5, '0')`), never as an int.
    This hits the very first consumer file.
  - **Mapper owns coercion** (read as string + `TryParse`), not Sylvan's typed
    getters — see §4.
- Add the pointer-table row in `skills/code-roadbed-csharp/SKILL.md`.
- Update `reference-roadbed-net.md` (if present) with `DownloadFileAsync` /
  `NetHttpDownloadRequest` / `NetHttpDownloadResult` (incl. `ContentSha256`).

---

## 4. Key design decisions (locked)

- **Download to disk, not memory** (ZIP needs seek; disk bounds RAM). §1.
- **Sylvan.Data.Excel** as the read dependency (MIT, `DbDataReader`, low-alloc);
  pinned version + fully-qualified type name to avoid the `ExcelDataReader`
  name collision. §1, §3.2.
- **Streaming-first read model** — `IAsyncEnumerable<T>`, no `IList<T> DataRows`.
  Deliberate divergence from `IoCsvFile<T>`.
- **`ReadBatchesAsync(batchSize)` ships in v1** (confirmed) alongside
  `ReadRowsAsync` — removes the manual buffer + trailing-flush from every consumer.
- **Mapper owns all type coercion** (confirmed) — mappers read cells **as strings
  and `TryParse`**, never Sylvan's typed getters (which throw on dirty gov data;
  we've already hit `"1"` vs `"1.0"` and empty-string-as-null).
- **Read-only v1** — no write/export.
- **The retried unit is the whole download attempt** — headers + body copy run
  inside the retry loop; a mid-body drop restarts the attempt (clean `.part`
  recreate, no Range/resume). §3.1.
- **SHA-256 content hash computed in the single copy pass** (confirmed) — the
  provenance anchor for the live-fetch model; the consumer records
  `ContentSha256` on the activity row before deleting the temp file. §3.1.
- **Same envelope for downloads** — `NetHttpResponse<NetHttpDownloadResult>`.
- **Consumer owns the temp-file lifecycle** (confirmed) — passes
  `DestinationPath`, reads `ContentSha256`, then deletes.
- **`RootNamespace = Roadbed.IO`** for the Xlsx project (convention parity).
- **Atomic `.part` download**; default per-attempt timeout **300s** (confirmed
  for v1), and that timeout must cancel the **body copy** and trigger a retry
  rather than a hard fail.

---

## 5. Open / deferred decisions

All four prior open decisions were resolved in review (batched overload: yes;
mapper-owned coercion: yes; consumer owns temp-file lifecycle: yes; 300s
per-attempt timeout: accepted) — see §4. What remains:

- **Inactivity-based download timeout** — more correct than a fixed 300s
  per-attempt wall clock for very large files, but more work. Deferred; revisit
  if 300s proves too blunt in practice.
- **Header-after-skip support** — resolve the §3.2 MUST-VERIFY against the pinned
  Sylvan version and pick the "works" or the "validate-and-throw" contract. This
  is verification, not an open design choice.

---

## 6. Out of scope

- Writing/exporting `.xlsx`.
- The Open XML SDK SAX path for streaming the shared-strings table (escape hatch
  only; revisit if a real file blows the memory floor).
- Auto-detecting file format / repairing mislabeled files (e.g. HTML-as-`.xls`).
- Any scheduling/orchestration — consumers decide when to download and ingest.

---

## 7. Acceptance criteria

- A consumer can download a large `.xlsx` to disk and stream-map it to typed
  rows with memory bounded to roughly the shared-strings table + current
  batch — no full-file or full-sheet materialization.
- `Roadbed.Net` exposes `DownloadFileAsync` with retry/backoff parity to
  `MakeHttpRequestAsync`, streaming to disk via `ResponseHeadersRead`, with
  atomic `.part` write and no partial file on failure.
- **A mid-body connection drop is retried** (not just header-stage failures),
  and the per-attempt timeout cancels the body copy.
- **`ContentSha256` is returned, matches the payload's SHA-256, and is computed
  in the single copy pass** (no second read of the file); `null` when disabled.
- `Roadbed.IO.Xlsx` supports sheet selection by name and index, headered/ordinal
  mapping, banner-row skipping per the verified header-after-skip contract, and
  `ReadBatchesAsync`.
- `[EnumeratorCancellation]` is present so cancellation flows into `await foreach`.
- Core `Roadbed.IO` gains no Excel-client dependency (Sylvan lives only in
  `Roadbed.IO.Xlsx`); the `Sylvan.Data.Excel` reference is version-pinned and the
  type is fully-qualified to avoid the `ExcelDataReader` collision.
- `.gitattributes` marks Office formats binary before any fixture is committed.
- Full solution builds clean (0 warnings, analyzers as errors); unit tests green.
- Reference doc (incl. leading-zero + mapper-coercion pitfalls) + SKILL pointer added.

---

## 8. Skill / docs update

- New `reference-roadbed-io-xlsx.md` (structure mirrors `reference-roadbed-io-csv.md`).
- SKILL.md pointer-table row for Xlsx ingest.
- `reference-roadbed-net.md` updated for the download API.
```
