namespace Roadbed.Crud.Operations.Sync;

/// <summary>
/// Defines the synchronous Bulk Insert operation for an entity.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <remarks>
/// Designed for set-based loads (ETL / medallion architecture) where many rows
/// are written in a single call. Each row is tagged with an opaque,
/// caller-supplied <c>activityId</c> that threads the rows back to the
/// activity / load that produced them. The consuming application owns the
/// activity record and its lifecycle; Roadbed never parses, validates the
/// shape of, or generates the identifier.
/// </remarks>
public interface ISyncBulkInsertOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
    #region Public Methods

    /// <summary>
    /// Inserts every entity in <paramref name="rows"/> in a single set-based load,
    /// tagging each row with <paramref name="activityId"/>.
    /// </summary>
    /// <param name="activityId">
    /// An opaque, caller-supplied identifier of the activity / load this insert
    /// belongs to. Must not be null, empty, or whitespace. Roadbed does not
    /// parse or interpret the value; the caller chooses the scheme (ULID,
    /// GUID, run number, etc.).
    /// </param>
    /// <param name="rows">Rows to insert. Must not be null.</param>
    /// <returns>The number of rows inserted.</returns>
    long BulkInsert(string activityId, IList<TEntity> rows);

    #endregion Public Methods
}
