namespace Roadbed.Crud.Repositories.Async;

using Roadbed.Crud.Operations.Async;

/// <summary>
/// Async bulk-only repository. Provides only the Bulk Insert operation.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <remarks>
/// Use for append-only landing tables ("Bronze") that accumulate rows across
/// loads. Every row is tagged with a caller-supplied <c>activityId</c> that
/// threads it back to the load that produced it.
/// </remarks>
public interface IAsyncBulkOnlyRepository<TEntity, TId>
    : IRepository<TEntity, TId>,
      IAsyncBulkInsertOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
}
