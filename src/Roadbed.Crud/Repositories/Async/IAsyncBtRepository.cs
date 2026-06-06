namespace Roadbed.Crud.Repositories.Async;

using Roadbed.Crud.Operations.Async;

/// <summary>
/// Async BT ("bee-tee") repository. Bulk Insert + Truncate.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <remarks>
/// Use for snapshot-reload staging / derived tables ("Silver") that are wiped
/// and rebuilt in full on every load. The intended call pattern is
/// <see cref="IAsyncTruncateOperation{TEntity, TId}.TruncateAsync"/> followed by
/// <see cref="IAsyncBulkInsertOperation{TEntity, TId}.BulkInsertAsync"/>.
/// Inherits from <see cref="IAsyncBulkOnlyRepository{TEntity, TId}"/> so the
/// Bulk Insert operation is declared once.
/// </remarks>
public interface IAsyncBtRepository<TEntity, TId>
    : IAsyncBulkOnlyRepository<TEntity, TId>,
      IAsyncTruncateOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
}
