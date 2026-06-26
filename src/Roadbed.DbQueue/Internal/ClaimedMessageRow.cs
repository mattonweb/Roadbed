namespace Roadbed.DbQueue.Internal;

using System;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Internal projection of one row returned from the claim SELECT.
/// </summary>
/// <remarks>
/// <para>
/// Materialized by the satellite executor's Dapper-backed
/// <c>QueryAsync&lt;T&gt;</c>. The claim SQL aliases each column to the
/// property name on this type (e.g. <c>m.external_id AS ExternalId</c>) so
/// the core assembly never has to depend on Dapper's snake-case mapping
/// extension.
/// </para>
/// </remarks>
[SuppressMessage(
    "Major Code Smell",
    "S3459:Unassigned members should be removed",
    Justification = "Properties are assigned by the satellite executor's Dapper materializer via property setters; static analysis cannot see those writes.")]
[SuppressMessage(
    "Major Code Smell",
    "S1144:Unused private types or members should be removed",
    Justification = "Properties are read by the Dapper-backed materializer and by QueueProcessor; analysis cannot see reflection access.")]
internal sealed class ClaimedMessageRow
{
    #region Public Properties

    /// <summary>
    /// Gets or sets the UTC enqueue timestamp from <c>queue_message_{name}.created_on</c>.
    /// </summary>
    public DateTime CreatedOn { get; set; }

    /// <summary>
    /// Gets or sets the shareable UUIDv7 external identifier.
    /// </summary>
    public string ExternalId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the internal FIFO row identifier.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the serialized payload, exactly as it was stored.
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    #endregion Public Properties
}
