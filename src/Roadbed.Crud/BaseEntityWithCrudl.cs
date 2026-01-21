/*
 * The namespace Roadbed.Crud.Entities was removed on purpose and replaced with Roadbed.Crud so that no additional using statements are required.
 */

namespace Roadbed.Crud;

using System.Text;
using Microsoft.Extensions.Logging;
using Roadbed;

/// <summary>
/// Base Entity for the Create, Retrieve, Update, Delete, and List operations.
/// </summary>
/// <typeparam name="TEntityType">Type inheriting from the Base.</typeparam>
/// <typeparam name="TDtoType">Type of Data Transfer Object (DTO) object.</typeparam>
/// <typeparam name="TIdType">Data type for the ID.</typeparam>
public abstract class BaseEntityWithCrudl<TEntityType, TDtoType, TIdType>
        : BaseClassWithLogging<TEntityType>
        where TDtoType : IDataTransferObject<TIdType>
{
    #region Private Fields

    /// <summary>
    /// Container for the Repository property.
    /// </summary>
    private readonly IBaseRepositoryWithCrudl<TDtoType, TIdType> repository;

    #endregion Private Fields

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseEntityWithCrudl{TEntityType, TDtoType, TIdType}"/> class.
    /// </summary>
    /// <param name="repository">Instance of the <see cref="IBaseRepositoryWithCrudl{TDtoType, TIdType}"/> repository.</param>
    /// <param name="factory">Represents a type used to configure the logging system.</param>
    protected BaseEntityWithCrudl(
        IBaseRepositoryWithCrudl<TDtoType, TIdType> repository,
        ILoggerFactory factory)
        : base(factory)
    {
        ArgumentNullException.ThrowIfNull(repository);

        this.repository = repository;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseEntityWithCrudl{TEntityType, TDtoType, TIdType}"/> class.
    /// </summary>
    /// <param name="repository">Instance of the <see cref="IBaseRepositoryWithCrudl{TDtoType, TIdType}"/> repository.</param>
    /// <param name="logger">Represents a type used to configure the logging system.</param>
    protected BaseEntityWithCrudl(
        IBaseRepositoryWithCrudl<TDtoType, TIdType> repository,
        ILogger logger)
        : base(logger)
    {
        ArgumentNullException.ThrowIfNull(repository);

        this.repository = repository;
    }

    #endregion Public Constructors

    #region Public Properties

    /// <summary>
    /// Gets the <see cref="IBaseRepositoryWithCrudl{TDtoType, TIdType}"/> Repository.
    /// </summary>
    public IBaseRepositoryWithCrudl<TDtoType, TIdType> Repository
    {
        get
        {
            return this.repository;
        }
    }

    #endregion Public Properties

    #region Public Methods

    /// <summary>
    /// Create Operation for the Data Transfer Object (DTO) entity.
    /// </summary>
    /// <param name="dto">Data Transfer Object (DTO) entity.</param>
    /// <param name="cancellationToken">Token to notify when an operation should be canceled.</param>
    /// <returns>ID of the Data Transfer Object (DTO) entity.</returns>
    public async Task<TIdType?> CreateAsync(TDtoType dto, CancellationToken cancellationToken = default)
    {
        // Create Log Entry
        StringBuilder builder = new StringBuilder();

        builder.Append($"Create operation called in {typeof(TEntityType).ToString()}. ");

        // Log message
        this.LogTrace(builder.ToString());

        return await this.Repository.CreateAsync(dto, cancellationToken);
    }

    /// <summary>
    /// Delete Operation for the Data Transfer Object (DTO) entity.
    /// </summary>
    /// <param name="id">ID of the Data Transfer Object (DTO) entity.</param>
    /// <param name="cancellationToken">Token to notify when an operation should be canceled.</param>
    /// <returns>A unit of work representing when operation has been completed.</returns>
    public async Task DeleteAsync(TIdType id, CancellationToken cancellationToken = default)
    {
        // Create Log Entry
        StringBuilder builder = new StringBuilder();

        builder.Append($"Delete operation called in {typeof(TEntityType).ToString()}.");

        if (object.Equals(id, default(TIdType)))
        {
            builder.Append($"DTO ID is null.");
        }
        else
        {
            builder.Append($"DTO ID is {id!.ToString()}.");
        }

        // Log message
        this.LogTrace(builder.ToString());

        // Null Check
        if (object.Equals(id, default(TIdType)))
        {
            return;
        }

        await this.Repository.DeleteAsync(id, cancellationToken);
    }

    /// <summary>
    /// List Operation for the Data Transfer Object (DTO) entity.
    /// </summary>
    /// <param name="cancellationToken">Token to notify when an operation should be canceled.</param>
    /// <returns>List of the Data Transfer Object (DTO) entites.</returns>
    public async Task<IList<TDtoType>> ListAsync(CancellationToken cancellationToken = default)
    {
        // Log message
        this.LogTrace($"List operation called in {typeof(TEntityType).ToString()}.");

        return await this.repository.ListAsync(cancellationToken);
    }

    /// <summary>
    /// Read Operation for the Data Transfer Object (DTO) entity.
    /// </summary>
    /// <param name="id">ID of the Data Transfer Object (DTO) entity.</param>
    /// <param name="cancellationToken">Token to notify when an operation should be canceled.</param>
    /// <returns>Data Transfer Object (DTO) entity.</returns>
    public async Task<TDtoType?> ReadAsync(TIdType id, CancellationToken cancellationToken = default)
    {
        // Create Log Entry
        StringBuilder builder = new StringBuilder();

        builder.Append($"Retrieve operation called in {typeof(TEntityType).ToString()}. ");

        if (!object.Equals(id, default(TIdType)))
        {
            builder.Append($"DTO ID is {id!.ToString()}.");
        }

        // Log message
        this.LogTrace(builder.ToString());

        // Null Check
        if (object.Equals(id, default(TIdType)))
        {
            return default(TDtoType);
        }

        return await this.Repository.ReadAsync(id, cancellationToken);
    }

    /// <summary>
    /// Update Operation for the Data Transfer Object (DTO) entity.
    /// </summary>
    /// <param name="dto">Data Transfer Object (DTO) entity.</param>
    /// <param name="cancellationToken">Token to notify when an operation should be canceled.</param>
    /// <returns>A unit of work representing when operation has been completed.</returns>
    public async Task UpdateAsync(TDtoType dto, CancellationToken cancellationToken = default)
    {
        // Create Log Entry
        StringBuilder builder = new StringBuilder();

        builder.Append($"Update operation called in {typeof(TEntityType).ToString()}.");

        if (object.Equals(dto, default(TDtoType)))
        {
            builder.Append($"DTO is null.");
        }
        else
        {
            if (!object.Equals(dto.Id, default(TIdType)))
            {
                builder.Append($"DTO ID is {dto.Id!.ToString()}.");
            }
        }

        // Log message
        this.LogTrace(builder.ToString());

        // Null Check
        if (object.Equals(dto, default(TDtoType)))
        {
            return;
        }

        // Call repository
        await this.Repository.UpdateAsync(dto, cancellationToken);
    }

    #endregion Public Methods
}