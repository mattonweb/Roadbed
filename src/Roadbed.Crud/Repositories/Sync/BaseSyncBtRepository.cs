namespace Roadbed.Crud.Repositories.Sync;

using Microsoft.Extensions.Logging;

/// <summary>
/// Abstract base for sync BT ("bee-tee") repositories. Bulk Insert + Truncate.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <remarks>
/// Two abstract methods: <see cref="BulkInsert"/> and <see cref="Truncate"/>.
/// The consuming class must implement both. Use Visual Studio's "Implement
/// abstract class" to generate the stubs. Concrete implementations are
/// responsible for validating <c>activityId</c> and <c>rows</c> arguments;
/// the surrounding service layer also validates when a request flows through
/// <see cref="Roadbed.Crud.Services.Sync.BaseSyncBtService{TEntity, TId}"/>.
/// </remarks>
public abstract class BaseSyncBtRepository<TEntity, TId>
    : BaseClassWithLogging,
      ISyncBtRepository<TEntity, TId>
    where TEntity : IEntity<TId>
{
    #region Protected Constructors

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="BaseSyncBtRepository{TEntity, TId}"/> class.
    /// </summary>
    protected BaseSyncBtRepository()
        : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="BaseSyncBtRepository{TEntity, TId}"/> class.
    /// </summary>
    /// <param name="logger">Represents a type used to perform logging.</param>
    protected BaseSyncBtRepository(ILogger logger)
        : base(logger)
    {
    }

    #endregion Protected Constructors

    #region Public Methods

    /// <inheritdoc/>
    public abstract long BulkInsert(string activityId, IList<TEntity> rows);

    /// <inheritdoc/>
    public abstract void Truncate();

    #endregion Public Methods
}
