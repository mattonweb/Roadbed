namespace Roadbed.Test.Unit.Crud;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Crud;
using Roadbed.Crud.Repositories.Sync;
using Roadbed.Crud.Services.Sync;

/// <summary>
/// Contains unit tests for verifying the behavior of the
/// <see cref="BaseSyncListOnlyService{TEntity, TId}"/> class.
/// </summary>
[TestClass]
public class BaseSyncListOnlyServiceTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that constructor creates an instance with valid parameters.
    /// </summary>
    [TestMethod]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        // Arrange (Given)
        var repository = new MockSyncRepository();
        ILogger logger = NullLogger.Instance;

        // Act (When)
        var instance = new StubSyncListOnlyService(repository, logger);

        // Assert (Then)
        Assert.IsNotNull(
            instance,
            "Instance should be created successfully with valid parameters.");
    }

    /// <summary>
    /// Unit test to verify that constructor throws when repository is null.
    /// </summary>
    [TestMethod]
    public void Constructor_NullRepository_ThrowsArgumentNullException()
    {
        // Arrange (Given)
        ISyncListOnlyRepository<TestEntityRecord, long>? nullRepository = null;
        ILogger logger = NullLogger.Instance;
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            new StubSyncListOnlyService(nullRepository!, logger);
        }
        catch (ArgumentNullException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "Constructor should throw ArgumentNullException when repository is null.");
    }

    /// <summary>
    /// Unit test to verify that instance implements ISyncListOnlyService interface.
    /// </summary>
    [TestMethod]
    public void Instance_TypeCheck_ImplementsISyncListOnlyService()
    {
        // Arrange (Given)
        var repository = new MockSyncRepository();
        ILogger logger = NullLogger.Instance;

        // Act (When)
        var instance = new StubSyncListOnlyService(repository, logger);

        // Assert (Then)
        Assert.IsInstanceOfType<ISyncListOnlyService<TestEntityRecord, long>>(
            instance,
            "Instance should implement ISyncListOnlyService<TestEntityRecord, long>.");
    }

    /// <summary>
    /// Unit test to verify that instance inherits from BaseClassWithLogging.
    /// </summary>
    [TestMethod]
    public void Instance_TypeCheck_InheritsBaseClassWithLogging()
    {
        // Arrange (Given)
        var repository = new MockSyncRepository();
        ILogger logger = NullLogger.Instance;

        // Act (When)
        var instance = new StubSyncListOnlyService(repository, logger);

        // Assert (Then)
        Assert.IsInstanceOfType<BaseClassWithLogging>(
            instance,
            "Instance should inherit from BaseClassWithLogging.");
    }

    /// <summary>
    /// Unit test to verify that Repository property returns the injected repository.
    /// </summary>
    [TestMethod]
    public void Repository_Getter_ReturnsInjectedRepository()
    {
        // Arrange (Given)
        var repository = new MockSyncRepository();
        ILogger logger = NullLogger.Instance;
        var instance = new StubSyncListOnlyService(repository, logger);

        // Act (When)
        ISyncListOnlyRepository<TestEntityRecord, long> result =
            instance.ExposedRepository;

        // Assert (Then)
        Assert.AreSame(
            repository,
            result,
            "Repository property should return the repository passed to the constructor.");
    }

    /// <summary>
    /// Unit test to verify that List delegates to the repository.
    /// </summary>
    [TestMethod]
    public void List_Called_DelegatesToRepository()
    {
        // Arrange (Given)
        var expectedEntities = new List<TestEntityRecord>
        {
            new TestEntityRecord { Id = 1, Name = "First" },
            new TestEntityRecord { Id = 2, Name = "Second" },
        };

        var repository = new MockSyncRepository();
        repository.OnList = () => expectedEntities;

        ILogger logger = NullLogger.Instance;
        var instance = new StubSyncListOnlyService(repository, logger);

        // Act (When)
        IList<TestEntityRecord> result = instance.List();

        // Assert (Then)
        Assert.AreSame(
            expectedEntities,
            result,
            "List should return the collection from the repository.");
        Assert.AreEqual(
            1,
            repository.ListCallCount,
            "List should delegate to the repository exactly once.");
    }

    /// <summary>
    /// Unit test to verify that List returns empty collection when no entities exist.
    /// </summary>
    [TestMethod]
    public void List_NoEntities_ReturnsEmptyCollection()
    {
        // Arrange (Given)
        var repository = new MockSyncRepository();
        ILogger logger = NullLogger.Instance;
        var instance = new StubSyncListOnlyService(repository, logger);

        // Act (When)
        IList<TestEntityRecord> result = instance.List();

        // Assert (Then)
        Assert.HasCount(
            0,
            result,
            "List should return an empty collection when no entities exist.");
    }

    #endregion Public Methods

    #region Private Classes

    /// <summary>
    /// Concrete subclass for testing the BaseSyncListOnlyService base class.
    /// Exposes the protected constructor and Repository property.
    /// </summary>
    private sealed class StubSyncListOnlyService
        : BaseSyncListOnlyService<TestEntityRecord, long>
    {
        public StubSyncListOnlyService(
            ISyncListOnlyRepository<TestEntityRecord, long> repository,
            ILogger logger)
            : base(repository, logger)
        {
        }

        public ISyncListOnlyRepository<TestEntityRecord, long> ExposedRepository
            => this.Repository;
    }

    #endregion Private Classes
}