namespace Roadbed.Test.Unit.Crud.Mocks;

using Microsoft.Extensions.Logging;
using Roadbed.Crud;

/// <summary>
/// Mock entity for testing BaseEntityWithCrudl.
/// </summary>
public class MockCrudEntity : BaseEntityWithCrud<MockCrudEntity, MockDto, int>
{
    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="MockCrudEntity"/> class.
    /// </summary>
    /// <param name="repository">Mock repository for data management.</param>
    /// <param name="factory">Mock factory for logging.</param>
    public MockCrudEntity(IBaseRepositoryWithCrud<MockDto, int> repository, ILoggerFactory factory)
        : base(repository, factory)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MockCrudEntity"/> class.
    /// </summary>
    /// <param name="repository">Mock repository for data management.</param>
    /// <param name="logger">Mock logger for log messages.</param>
    public MockCrudEntity(IBaseRepositoryWithCrud<MockDto, int> repository, ILogger logger)
        : base(repository, logger)
    {
    }

    #endregion Public Constructors
}