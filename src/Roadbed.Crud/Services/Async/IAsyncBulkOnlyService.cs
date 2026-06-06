namespace Roadbed.Crud.Services.Async;

using Roadbed.Crud.Operations.Async;

/// <summary>
/// Async bulk-only service. Provides only the Bulk Insert operation.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
public interface IAsyncBulkOnlyService<TEntity, TId>
    : IAsyncBulkInsertOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
}
