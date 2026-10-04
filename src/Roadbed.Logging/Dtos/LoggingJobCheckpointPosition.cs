namespace Roadbed.Logging;

using System;

/// <summary>
/// The position a checkpointed job advances to: the point its next run
/// starts from.
/// </summary>
/// <remarks>
/// <para>
/// Set whichever cursor fits the source being read; at least one value is
/// required. A job may use more than one (for example an id and the timestamp
/// it was read at).
/// </para>
/// <para>
/// A high-water mark has a known hole: with concurrent writers, an
/// auto-increment id or a clock time can be committed out of order and is then
/// skipped forever. A checkpoint suits change detection and bounded scans. It
/// does not replace a queue that drains by a state flag on each row.
/// </para>
/// </remarks>
public sealed class LoggingJobCheckpointPosition
{
    #region Public Properties

    /// <summary>
    /// Gets the highest numeric id processed.
    /// </summary>
    public long? Id { get; init; }

    /// <summary>
    /// Gets the latest timestamp processed.
    /// </summary>
    /// <remarks>
    /// Stored exactly as supplied, truncated to microsecond precision
    /// (MariaDB <c>DATETIME(6)</c>). Roadbed does not convert it to UTC; keep it
    /// in the clock of the source it describes.
    /// </remarks>
    public DateTime? On { get; init; }

    /// <summary>
    /// Gets a text cursor, such as a UUIDv7, a ULID or a vendor page token.
    /// </summary>
    /// <remarks>
    /// At most 1,024 characters. Compared and stored case-sensitively.
    /// </remarks>
    public string? Text { get; init; }

    #endregion Public Properties
}
