namespace Roadbed.Crud.Services.Sync;

using Roadbed.Crud.Operations.Sync;

/// <summary>
/// Sync BT ("bee-tee") service. Bulk Insert + Truncate.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
public interface ISyncBtService<TEntity, TId>
    : ISyncBulkOnlyService<TEntity, TId>,
      ISyncTruncateOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
}
