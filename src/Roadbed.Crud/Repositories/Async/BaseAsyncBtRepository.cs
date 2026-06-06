namespace Roadbed.Crud.Repositories.Async;

using Microsoft.Extensions.Logging;

/// <summary>
/// Abstract base for async BT ("bee-tee") repositories. Bulk Insert + Truncate.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <remarks>
/// Two abstract methods: <see cref="BulkInsertAsync"/> and
/// <see cref="TruncateAsync"/>. The consuming class must implement both. Use
/// Visual Studio's "Implement abstract class" to generate the stubs. Concrete
/// implementations are responsible for validating <c>activityId</c> and
/// <c>rows</c> arguments; the surrounding service layer also validates when a
/// request flows through
/// <see cref="Roadbed.Crud.Services.Async.BaseAsyncBtService{TEntity, TId}"/>.
/// </remarks>
public abstract class BaseAsyncBtRepository<TEntity, TId>
    : BaseClassWithLogging,
      IAsyncBtRepository<TEntity, TId>
    where TEntity : IEntity<TId>
{
    #region Protected Constructors

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="BaseAsyncBtRepository{TEntity, TId}"/> class.
    /// </summary>
    protected BaseAsyncBtRepository()
        : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="BaseAsyncBtRepository{TEntity, TId}"/> class.
    /// </summary>
    /// <param name="logger">Represents a type used to perform logging.</param>
    protected BaseAsyncBtRepository(ILogger logger)
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

    /// <inheritdoc/>
    public abstract Task TruncateAsync(
        CancellationToken cancellationToken = default);

    #endregion Public Methods
}
