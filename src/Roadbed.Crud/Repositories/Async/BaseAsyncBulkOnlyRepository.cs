namespace Roadbed.Crud.Repositories.Async;

using Microsoft.Extensions.Logging;

/// <summary>
/// Abstract base for async bulk-only repositories.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <remarks>
/// One abstract method: <see cref="BulkInsertAsync"/>. The consuming class
/// must implement the data access logic. Use Visual Studio's "Implement
/// abstract class" to generate the stub. Concrete implementations are
/// responsible for validating <c>activityId</c> and <c>rows</c> arguments;
/// the surrounding service layer also validates when a request flows through
/// <see cref="Roadbed.Crud.Services.Async.BaseAsyncBulkOnlyService{TEntity, TId}"/>.
/// </remarks>
public abstract class BaseAsyncBulkOnlyRepository<TEntity, TId>
    : BaseClassWithLogging,
      IAsyncBulkOnlyRepository<TEntity, TId>
    where TEntity : IEntity<TId>
{
    #region Protected Constructors

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="BaseAsyncBulkOnlyRepository{TEntity, TId}"/> class.
    /// </summary>
    protected BaseAsyncBulkOnlyRepository()
        : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="BaseAsyncBulkOnlyRepository{TEntity, TId}"/> class.
    /// </summary>
    /// <param name="logger">Represents a type used to perform logging.</param>
    protected BaseAsyncBulkOnlyRepository(ILogger logger)
        : base(logger)
    {
    }

    #endregion Protected Constructors

    #region Public Methods

    /// <inheritdoc/>
    public abstract Task<long> BulkInsertAsync(
        string activityId,
        IList<TEntity> rows,
        CancellationToken cancellationToken = default);

    #endregion Public Methods
}
