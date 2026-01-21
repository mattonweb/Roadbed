/*
 * The namespace Roadbed.Crud.Repositories was removed on purpose and replaced with Roadbed.Crud so that no additional using statements are required.
 */
namespace Roadbed.Crud;

using Roadbed.Crud.Entities.Operations;

/// <summary>
/// Entity contract for the Create, Retrieve, Update, Delete, and List operations.
/// </summary>
/// <typeparam name="TDtoType">Type of Data Transfer Object (DTO) object.</typeparam>
/// <typeparam name="TIdType">Data type for the ID.</typeparam>
public interface IBaseRepositoryWithCrudl<TDtoType, TIdType>
        : IRepositoryOperationCreate<TDtoType, TIdType>,
            IRepositoryOperationRead<TDtoType, TIdType>,
            IRepositoryOperationUpdate<TDtoType, TIdType>,
            IEntityOperatioDelete<TDtoType, TIdType>,
            IRepositoryOperationList<TDtoType, TIdType>
        where TDtoType : IDataTransferObject<TIdType>
{
}