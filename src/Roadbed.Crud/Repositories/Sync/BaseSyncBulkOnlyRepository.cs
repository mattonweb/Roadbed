namespace Roadbed.Crud.Repositories.Sync;

using Microsoft.Extensions.Logging;

/// <summary>
/// Abstract base for sync bulk-only repositories.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <remarks>
/// One abstract method: <see cref="BulkInsert"/>. The consuming class must
/// implement the data access logic. Use Visual Studio's "Implement abstract
/// class" to generate the stub. Concrete implementations are responsible for
/// validating <c>activityId</c> and <c>rows</c> arguments; the surrounding
/// service layer also validates when a request flows through
/// <see cref="Roadbed.Crud.Services.Sync.BaseSyncBulkOnlyService{TEntity, TId}"/>.
/// </remarks>
public abstract class BaseSyncBulkOnlyRepository<TEntity, TId>
    : BaseClassWithLogging,
      ISyncBulkOnlyRepository<TEntity, TId>
    where TEntity : IEntity<TId>
{
    #region Protected Constructors

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="BaseSyncBulkOnlyRepository{TEntity, TId}"/> class.
    /// </summary>
    protected BaseSyncBulkOnlyRepository()
        : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="BaseSyncBulkOnlyRepository{TEntity, TId}"/> class.
    /// </summary>
    /// <param name="logger">Represents a type used to perform logging.</param>
    protected BaseSyncBulkOnlyRepository(ILogger logger)
        : base(logger)
    {
    }

    #endregion Protected Constructors

    #region Public Methods

    /// <inheritdoc/>
    public abstract long BulkInsert(string activityId, IList<TEntity> rows);

    #endregion Public Methods
}
