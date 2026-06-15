namespace Roadbed.IO;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Streams rows of a single worksheet from a large <c>.xlsx</c>/<c>.xlsb</c>/<c>.xls</c>
/// workbook and maps them to typed entities, with memory bounded to roughly the
/// shared-strings table plus the current row/batch.
/// </summary>
/// <typeparam name="T">The entity type produced per row.</typeparam>
/// <remarks>
/// Unlike <c>IoCsvFile&lt;T&gt;</c>, this type is <strong>streaming-first</strong>:
/// it never materializes the whole sheet into an in-memory list. Enumerate
/// <see cref="ReadRowsAsync"/> (or <see cref="ReadBatchesAsync"/>) and feed a sink
/// — typically a bulk insert — as rows arrive.
/// </remarks>
public sealed class IoXlsxFile<T> : IoFile
{
    #region Private Fields

    private readonly string _path;
    private readonly IXlsxEntityMapper<T> _mapper;
    private readonly IoXlsxReadOptions _options;

    #endregion Private Fields

    #region Private Constructors

    private IoXlsxFile(string path, IXlsxEntityMapper<T> mapper, IoXlsxReadOptions options)
        : base(new IoFileInfo(path))
    {
        this._path = path;
        this._mapper = mapper;
        this._options = options;
    }

    #endregion Private Constructors

    #region Public Methods

    /// <summary>
    /// Creates a reader over the workbook at <paramref name="path"/>.
    /// </summary>
    /// <param name="path">Local path to the workbook. The file is opened (seekably) only when enumeration begins.</param>
    /// <param name="mapper">Per-row mapper.</param>
    /// <param name="options">Sheet/header options; defaults to the first sheet with a header row.</param>
    /// <returns>A configured <see cref="IoXlsxFile{T}"/>.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="path"/> is blank, or <see cref="IoXlsxReadOptions.HasHeaders"/>
    /// is <c>true</c> while <see cref="IoXlsxReadOptions.SkipLeadingRows"/> is greater
    /// than 0 (a header must be on the sheet's first row — see
    /// <see cref="IoXlsxReadOptions.HasHeaders"/>).
    /// </exception>
    public static IoXlsxFile<T> FromFile(string path, IXlsxEntityMapper<T> mapper, IoXlsxReadOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(mapper);

        options ??= new IoXlsxReadOptions();

        ArgumentOutOfRangeException.ThrowIfNegative(options.SkipLeadingRows);
        ArgumentOutOfRangeException.ThrowIfNegative(options.SheetIndex);

        if (options.HasHeaders && options.SkipLeadingRows > 0)
        {
            throw new ArgumentException(
                "A headered read requires the header on the sheet's first row, so SkipLeadingRows must be 0 when HasHeaders is true. " +
                "For a sheet whose header is preceded by banner rows, set HasHeaders = false, set SkipLeadingRows, and map by ordinal.",
                nameof(options));
        }

        return new IoXlsxFile<T>(path, mapper, options);
    }

    /// <summary>
    /// Streams the worksheet's rows, forward-only, mapping each to a
    /// <typeparamref name="T"/>. Rows the mapper maps to <c>null</c> are skipped.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel enumeration.</param>
    /// <returns>An async sequence of mapped entities.</returns>
    public async IAsyncEnumerable<T> ReadRowsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var schema = this._options.HasHeaders
            ? Sylvan.Data.Excel.ExcelSchema.Default
            : Sylvan.Data.Excel.ExcelSchema.NoHeaders;

        var readerOptions = new Sylvan.Data.Excel.ExcelDataReaderOptions { Schema = schema };

        await using Sylvan.Data.Excel.ExcelDataReader reader =
            await Sylvan.Data.Excel.ExcelDataReader.CreateAsync(this._path, readerOptions, cancellationToken)
                .ConfigureAwait(false);

        OpenWorksheet(reader, this._options);

        // Discard the configured number of leading banner rows. This loop only
        // runs for headerless reads because FromFile rejects skipping leading
        // rows when a header is expected.
        for (int skipped = 0; skipped < this._options.SkipLeadingRows; skipped++)
        {
            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                yield break;
            }
        }

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            T? entity = this._mapper.MapEntity(reader);

            if (entity is not null)
            {
                yield return entity;
            }
        }
    }

    /// <summary>
    /// Streams the worksheet's rows pre-grouped into batches of
    /// <paramref name="batchSize"/> — a thin wrapper over <see cref="ReadRowsAsync"/>
    /// that removes the manual buffer + trailing-flush from the consumer's loop.
    /// </summary>
    /// <param name="batchSize">Maximum rows per yielded batch.</param>
    /// <param name="cancellationToken">Token to cancel enumeration.</param>
    /// <returns>An async sequence of row batches; the final batch may be partial.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="batchSize"/> is not positive.</exception>
    public async IAsyncEnumerable<IReadOnlyList<T>> ReadBatchesAsync(
        int batchSize,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);

        var batch = new List<T>(batchSize);

        await foreach (T row in this.ReadRowsAsync(cancellationToken).ConfigureAwait(false))
        {
            batch.Add(row);

            if (batch.Count >= batchSize)
            {
                yield return batch;
                batch = new List<T>(batchSize);
            }
        }

        if (batch.Count > 0)
        {
            yield return batch;
        }
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Positions the reader on the requested worksheet (by name when supplied,
    /// otherwise by index). The first worksheet is already open after creation.
    /// </summary>
    /// <param name="reader">The open reader.</param>
    /// <param name="options">The sheet selection options.</param>
    /// <exception cref="ArgumentException">A named worksheet was not found.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The sheet index is outside the workbook.</exception>
    private static void OpenWorksheet(Sylvan.Data.Excel.ExcelDataReader reader, IoXlsxReadOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.SheetName))
        {
            if (!reader.TryOpenWorksheet(options.SheetName))
            {
                throw new ArgumentException(
                    $"Worksheet '{options.SheetName}' was not found. Available worksheets: {string.Join(", ", reader.WorksheetNames)}.",
                    nameof(options));
            }

            return;
        }

        if (options.SheetIndex >= reader.WorksheetCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                $"SheetIndex {options.SheetIndex} is outside the workbook, which has {reader.WorksheetCount} worksheet(s).");
        }

        // The first worksheet is current immediately after creation.
        if (options.SheetIndex == 0)
        {
            return;
        }

        string name = reader.WorksheetNames.ElementAt(options.SheetIndex);

        if (!reader.TryOpenWorksheet(name))
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                $"Worksheet at index {options.SheetIndex} ('{name}') could not be opened.");
        }
    }

    #endregion Private Methods
}
