namespace Roadbed.IO;

/// <summary>
/// Options controlling which worksheet is read and how its leading rows and
/// header are interpreted.
/// </summary>
public sealed class IoXlsxReadOptions
{
    #region Public Properties

    /// <summary>
    /// Gets or sets the worksheet to read by name. When <c>null</c> or blank,
    /// <see cref="SheetIndex"/> is used instead.
    /// </summary>
    public string? SheetName { get; set; }

    /// <summary>
    /// Gets or sets the zero-based worksheet index to read when
    /// <see cref="SheetName"/> is not supplied. Defaults to the first sheet.
    /// </summary>
    public int SheetIndex { get; set; }

    /// <summary>
    /// Gets or sets the number of leading rows to discard before mapping begins
    /// (e.g. banner/preamble rows). Only valid when <see cref="HasHeaders"/> is
    /// <c>false</c> — see the remarks on <see cref="HasHeaders"/>.
    /// </summary>
    public int SkipLeadingRows { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the worksheet's first row is a
    /// header, enabling name-based column lookup
    /// (<c>reader.GetOrdinal("ColumnName")</c>).
    /// </summary>
    /// <remarks>
    /// The header is bound from the sheet's <strong>first row</strong> when the
    /// worksheet is opened, so a header cannot follow skipped banner rows. When
    /// <c>true</c>, <see cref="SkipLeadingRows"/> must be 0. For files whose real
    /// header is preceded by banner rows, set this to <c>false</c>, set
    /// <see cref="SkipLeadingRows"/> to the number of rows to discard, and map by
    /// ordinal.
    /// </remarks>
    public bool HasHeaders { get; set; } = true;

    #endregion Public Properties
}
