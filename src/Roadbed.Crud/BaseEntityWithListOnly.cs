/*
 * The namespace Roadbed.Crud.Entities was removed on purpose and replaced with Roadbed.Crud so that no additional using statements are required.
 */

namespace Roadbed.Crud;

using Microsoft.Extensions.Logging;
using Roadbed;

/// <summary>
/// Base Entity for the Create, Retrieve, Update, Delete, and List operations.
/// </summary>
/// <typeparam name="TEntityType">Type inheriting from the Base.</typeparam>
/// <typeparam name="TDtoType">Type of Data Transfer Object (DTO) object.</typeparam>
/// <typeparam name="TIdType">Data type for the ID.</typeparam>
public abstract class BaseEntityWithListOnly<TEntityType, TDtoType, TIdType>
        : BaseClassWithLogging<TEntityType>
        where TDtoType : IDataTransferObject<TIdType>
{
    #region Private Fields

    /// <summary>
    /// Container for the Repository property.
    /// </summary>
    private readonly IBaseRepositoryWithListOnly<TDtoType, TIdType> repository;

    #endregion Private Fields

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseEntityWithListOnly{TEntityType, TDtoType, TIdType}"/> class.
    /// </summary>
    /// <param name="repository">Instance of the <see cref="IBaseRepositoryWithListOnly{TDtoType, TIdType}"/> repository.</param>
    /// <param name="factory">Represents a type used to configure the logging system.</param>
    protected BaseEntityWithListOnly(
        IBaseRepositoryWithListOnly<TDtoType, TIdType> repository,
        ILoggerFactory factory)
        : base(factory)
    {
        ArgumentNullException.ThrowIfNull(repository);

        this.repository = repository;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseEntityWithListOnly{TEntityType, TDtoType, TIdType}"/> class.
    /// </summary>
    /// <param name="repository">Instance of the <see cref="IBaseRepositoryWithListOnly{TDtoType, TIdType}"/> repository.</param>
    /// <param name="logger">Represents a type used to configure the logging system.</param>
    protected BaseEntityWithListOnly(
        IBaseRepositoryWithListOnly<TDtoType, TIdType> repository,
        ILogger logger)
        : base(logger)
    {
        ArgumentNullException.ThrowIfNull(repository);

        this.repository = repository;
    }

    #endregion Public Constructors

    #region Public Properties

    /// <summary>
    /// Gets the <see cref="IBaseRepositoryWithListOnly{TDtoType, TIdType}"/> Repository.
    /// </summary>
    public IBaseRepositoryWithListOnly<TDtoType, TIdType> Repository
    {
        get
        {
            return this.repository;
        }
    }

    #endregion Public Properties

    #region Public Methods

    /// <summary>
    /// List Operation for the Data Transfer Object (DTO) entity.
    /// </summary>
    /// <param name="cancellationToken">Token to notify when an operation should be canceled.</param>
    /// <returns>List of the Data Transfer Object (DTO) entites.</returns>
    public async Task<IList<TDtoType>> ListAsync(CancellationToken cancellationToken = default)
    {
        // Log message
        this.LogTrace($"List operation called in {typeof(TEntityType).ToString()}.");

        return await this.Repository.ListAsync(cancellationToken);
    }

    #endregion Public Methods
}