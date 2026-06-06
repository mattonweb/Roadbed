namespace Roadbed.Crud.Services.Sync;

using Roadbed.Crud.Operations.Sync;

/// <summary>
/// Sync bulk-only service. Provides only the Bulk Insert operation.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
public interface ISyncBulkOnlyService<TEntity, TId>
    : ISyncBulkInsertOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
}
