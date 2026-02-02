namespace Roadbed.Test.Unit.Crud;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Crud;
using Roadbed.Crud.Repositories.Sync;
using Roadbed.Crud.Services.Sync;

/// <summary>
/// Contains unit tests for verifying the behavior of the
/// <see cref="BaseSyncCrudlService{TEntity, TId}"/> class.
/// </summary>
[TestClass]
public class BaseSyncCrudlServiceTests
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
        var instance = new StubSyncCrudlService(repository, logger);

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
        ISyncCrudlRepository<TestEntityRecord, long>? nullRepository = null;
        ILogger logger = NullLogger.Instance;
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            new StubSyncCrudlService(nullRepository!, logger);
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
    /// Unit test to verify that instance implements ISyncCrudlService interface.
    /// </summary>
    [TestMethod]
    public void Instance_TypeCheck_ImplementsISyncCrudlService()
    {
        // Arrange (Given)
        var repository = new MockSyncRepository();
        ILogger logger = NullLogger.Instance;

        // Act (When)
        var instance = new StubSyncCrudlService(repository, logger);

        // Assert (Then)
        Assert.IsInstanceOfType<ISyncCrudlService<TestEntityRecord, long>>(
            instance,
            "Instance should implement ISyncCrudlService<TestEntityRecord, long>.");
    }

    /// <summary>
    /// Unit test to verify that instance implements ISyncCrudService interface.
    /// </summary>
    [TestMethod]
    public void Instance_TypeCheck_ImplementsISyncCrudService()
    {
        // Arrange (Given)
        var repository = new MockSyncRepository();
        ILogger logger = NullLogger.Instance;

        // Act (When)
        var instance = new StubSyncCrudlService(repository, logger);

        // Assert (Then)
        Assert.IsInstanceOfType<ISyncCrudService<TestEntityRecord, long>>(
            instance,
            "Instance should implement ISyncCrudService<TestEntityRecord, long>.");
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
        var instance = new StubSyncCrudlService(repository, logger);

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
        var instance = new StubSyncCrudlService(repository, logger);

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
        var instance = new StubSyncCrudlService(repository, logger);

        // Act (When)
        ISyncCrudlRepository<TestEntityRecord, long> result =
            instance.ExposedRepository;

        // Assert (Then)
        Assert.AreSame(
            repository,
            result,
            "Repository property should return the repository passed to the constructor.");
    }

    /// <summary>
    /// Unit test to verify that Create delegates to the repository.
    /// </summary>
    [TestMethod]
    public void Create_ValidEntity_DelegatesToRepository()
    {
        // Arrange (Given)
        var entity = new TestEntityRecord { Id = 1, Name = "Test" };
        var repository = new MockSyncRepository();
        repository.OnCreate = (e) => e;

        ILogger logger = NullLogger.Instance;
        var instance = new StubSyncCrudlService(repository, logger);

        // Act (When)
        TestEntityRecord result = instance.Create(entity);

        // Assert (Then)
        Assert.AreSame(
            entity,
            result,
            "Create should return the entity from the repository.");
        Assert.AreEqual(
            1,
            repository.CreateCallCount,
            "Create should delegate to the repository exactly once.");
    }

    /// <summary>
    /// Unit test to verify that Read delegates to the repository.
    /// </summary>
    [TestMethod]
    public void Read_ExistingId_DelegatesToRepository()
    {
        // Arrange (Given)
        var expectedEntity = new TestEntityRecord { Id = 1, Name = "Test" };
        var repository = new MockSyncRepository();
        repository.OnRead = (id) => expectedEntity;

        ILogger logger = NullLogger.Instance;
        var instance = new StubSyncCrudlService(repository, logger);

        // Act (When)
        TestEntityRecord? result = instance.Read(1);

        // Assert (Then)
        Assert.AreSame(
            expectedEntity,
            result,
            "Read should return the entity from the repository.");
        Assert.AreEqual(
            1,
            repository.ReadCallCount,
            "Read should delegate to the repository exactly once.");
    }

    /// <summary>
    /// Unit test to verify that Read returns null when entity is not found.
    /// </summary>
    [TestMethod]
    public void Read_NonExistingId_ReturnsNull()
    {
        // Arrange (Given)
        var repository = new MockSyncRepository();
        ILogger logger = NullLogger.Instance;
        var instance = new StubSyncCrudlService(repository, logger);

        // Act (When)
        TestEntityRecord? result = instance.Read(999);

        // Assert (Then)
        Assert.IsNull(
            result,
            "Read should return null when the entity is not found.");
    }

    /// <summary>
    /// Unit test to verify that Update delegates to the repository.
    /// </summary>
    [TestMethod]
    public void Update_ValidEntity_DelegatesToRepository()
    {
        // Arrange (Given)
        var entity = new TestEntityRecord { Id = 1, Name = "Updated" };
        var repository = new MockSyncRepository();
        repository.OnUpdate = (e) => e;

        ILogger logger = NullLogger.Instance;
        var instance = new StubSyncCrudlService(repository, logger);

        // Act (When)
        TestEntityRecord result = instance.Update(entity);

        // Assert (Then)
        Assert.AreSame(
            entity,
            result,
            "Update should return the entity from the repository.");
        Assert.AreEqual(
            1,
            repository.UpdateCallCount,
            "Update should delegate to the repository exactly once.");
    }

    /// <summary>
    /// Unit test to verify that Delete delegates to the repository.
    /// </summary>
    [TestMethod]
    public void Delete_ValidId_DelegatesToRepository()
    {
        // Arrange (Given)
        long idToDelete = 1;
        var repository = new MockSyncRepository();
        ILogger logger = NullLogger.Instance;
        var instance = new StubSyncCrudlService(repository, logger);

        // Act (When)
        instance.Delete(idToDelete);

        // Assert (Then)
        Assert.AreEqual(
            1,
            repository.DeleteCallCount,
            "Delete should delegate to the repository exactly once.");
        Assert.AreEqual(
            idToDelete,
            repository.LastDeleteId,
            "Delete should pass the correct identifier to the repository.");
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
        var instance = new StubSyncCrudlService(repository, logger);

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
        var instance = new StubSyncCrudlService(repository, logger);

        // Act (When)
        IList<TestEntityRecord> result = instance.List();

        // Assert (Then)
        Assert.HasCount(
            0,
            result,
            "List should return an empty collection when no entities exist.");
    }

    /// <summary>
    /// Unit test to verify that Exists returns true when entity is found.
    /// </summary>
    [TestMethod]
    public void Exists_EntityFound_ReturnsTrue()
    {
        // Arrange (Given)
        var existingEntity = new TestEntityRecord { Id = 1, Name = "Exists" };
        var repository = new MockSyncRepository();
        repository.OnRead = (id) => existingEntity;

        ILogger logger = NullLogger.Instance;
        var instance = new StubSyncCrudlService(repository, logger);

        // Act (When)
        bool result = instance.Exists(1);

        // Assert (Then)
        Assert.IsTrue(
            result,
            "Exists should return true when the repository returns an entity.");
    }

    /// <summary>
    /// Unit test to verify that Exists returns false when entity is not found.
    /// </summary>
    [TestMethod]
    public void Exists_EntityNotFound_ReturnsFalse()
    {
        // Arrange (Given)
        var repository = new MockSyncRepository();
        ILogger logger = NullLogger.Instance;
        var instance = new StubSyncCrudlService(repository, logger);

        // Act (When)
        bool result = instance.Exists(999);

        // Assert (Then)
        Assert.IsFalse(
            result,
            "Exists should return false when the repository returns null.");
    }

    /// <summary>
    /// Unit test to verify that Exists delegates to Read on the repository.
    /// </summary>
    [TestMethod]
    public void Exists_Called_DelegatesToReadOnRepository()
    {
        // Arrange (Given)
        var repository = new MockSyncRepository();
        ILogger logger = NullLogger.Instance;
        var instance = new StubSyncCrudlService(repository, logger);

        // Act (When)
        instance.Exists(42);

        // Assert (Then)
        Assert.AreEqual(
            1,
            repository.ReadCallCount,
            "Exists should delegate to Read on the repository exactly once.");
        Assert.AreEqual(
            42L,
            repository.LastReadId,
            "Exists should pass the correct identifier to Read on the repository.");
    }

    /// <summary>
    /// Unit test to verify that Upsert throws when entity is null.
    /// </summary>
    [TestMethod]
    public void Upsert_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange (Given)
        TestEntityRecord? nullEntity = null;
        var repository = new MockSyncRepository();
        ILogger logger = NullLogger.Instance;
        var instance = new StubSyncCrudlService(repository, logger);
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            instance.Upsert(nullEntity!);
        }
        catch (ArgumentNullException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "Upsert should throw ArgumentNullException when entity is null.");
    }

    /// <summary>
    /// Unit test to verify that Upsert calls Update when entity exists.
    /// </summary>
    [TestMethod]
    public void Upsert_ExistingEntity_CallsUpdate()
    {
        // Arrange (Given)
        var entity = new TestEntityRecord { Id = 1, Name = "Existing" };
        var repository = new MockSyncRepository();
        repository.OnRead = (id) => entity;
        repository.OnUpdate = (e) => e;

        ILogger logger = NullLogger.Instance;
        var instance = new StubSyncCrudlService(repository, logger);

        // Act (When)
        TestEntityRecord result = instance.Upsert(entity);

        // Assert (Then)
        Assert.AreEqual(
            1,
            repository.UpdateCallCount,
            "Upsert should call Update when the entity exists.");
        Assert.AreEqual(
            0,
            repository.CreateCallCount,
            "Upsert should not call Create when the entity exists.");
        Assert.AreSame(
            entity,
            result,
            "Upsert should return the entity from Update.");
    }

    /// <summary>
    /// Unit test to verify that Upsert calls Create when entity does not exist.
    /// </summary>
    [TestMethod]
    public void Upsert_NewEntity_CallsCreate()
    {
        // Arrange (Given)
        var entity = new TestEntityRecord { Id = 99, Name = "New" };
        var repository = new MockSyncRepository();
        repository.OnCreate = (e) => e;

        ILogger logger = NullLogger.Instance;
        var instance = new StubSyncCrudlService(repository, logger);

        // Act (When)
        TestEntityRecord result = instance.Upsert(entity);

        // Assert (Then)
        Assert.AreEqual(
            1,
            repository.CreateCallCount,
            "Upsert should call Create when the entity does not exist.");
        Assert.AreEqual(
            0,
            repository.UpdateCallCount,
            "Upsert should not call Update when the entity does not exist.");
        Assert.AreSame(
            entity,
            result,
            "Upsert should return the entity from Create.");
    }

    #endregion Public Methods

    #region Private Classes

    /// <summary>
    /// Concrete subclass for testing the BaseSyncCrudlService base class.
    /// Exposes the protected constructor and Repository property.
    /// </summary>
    private sealed class StubSyncCrudlService
        : BaseSyncCrudlService<TestEntityRecord, long>
    {
        public StubSyncCrudlService(
            ISyncCrudlRepository<TestEntityRecord, long> repository,
            ILogger logger)
            : base(repository, logger)
        {
        }

        public ISyncCrudlRepository<TestEntityRecord, long> ExposedRepository
            => this.Repository;
    }

    #endregion Private Classes
}