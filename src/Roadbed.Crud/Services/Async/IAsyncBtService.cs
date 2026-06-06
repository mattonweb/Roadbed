namespace Roadbed.Crud.Services.Async;

using Roadbed.Crud.Operations.Async;

/// <summary>
/// Async BT ("bee-tee") service. Bulk Insert + Truncate.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
public interface IAsyncBtService<TEntity, TId>
    : IAsyncBulkOnlyService<TEntity, TId>,
      IAsyncTruncateOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
}
