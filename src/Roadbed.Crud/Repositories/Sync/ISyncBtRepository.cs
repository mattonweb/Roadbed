namespace Roadbed.Crud.Repositories.Sync;

using Roadbed.Crud.Operations.Sync;

/// <summary>
/// Sync BT ("bee-tee") repository. Bulk Insert + Truncate.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <remarks>
/// Use for snapshot-reload staging / derived tables ("Silver") that are wiped
/// and rebuilt in full on every load. The intended call pattern is
/// <see cref="ISyncTruncateOperation{TEntity, TId}.Truncate"/> followed by
/// <see cref="ISyncBulkInsertOperation{TEntity, TId}.BulkInsert"/>.
/// Inherits from <see cref="ISyncBulkOnlyRepository{TEntity, TId}"/> so the
/// Bulk Insert operation is declared once.
/// </remarks>
public interface ISyncBtRepository<TEntity, TId>
    : ISyncBulkOnlyRepository<TEntity, TId>,
      ISyncTruncateOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
}
