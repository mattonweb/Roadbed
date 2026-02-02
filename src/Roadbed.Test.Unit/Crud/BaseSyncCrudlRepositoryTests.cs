namespace Roadbed.Test.Unit.Crud;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Crud;
using Roadbed.Crud.Repositories.Sync;

/// <summary>
/// Contains unit tests for verifying the behavior of the
/// <see cref="BaseSyncCrudlRepository{TEntity, TId}"/> abstract class.
/// </summary>
[TestClass]
public class BaseSyncCrudlRepositoryTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that parameterless constructor creates an instance successfully.
    /// </summary>
    [TestMethod]
    public void Constructor_NoParameters_CreatesInstance()
    {
        // Arrange (Given)

        // Act (When)
        var instance = new StubSyncCrudlRepository();

        // Assert (Then)
        Assert.IsNotNull(
            instance,
            "Instance should be created successfully with the parameterless constructor.");
    }

    /// <summary>
    /// Unit test to verify that logger constructor creates an instance successfully.
    /// </summary>
    [TestMethod]
    public void Constructor_WithLogger_CreatesInstance()
    {
        // Arrange (Given)
        ILogger logger = NullLogger.Instance;

        // Act (When)
        var instance = new StubSyncCrudlRepository(logger);

        // Assert (Then)
        Assert.IsNotNull(
            instance,
            "Instance should be created successfully with the logger constructor.");
    }

    /// <summary>
    /// Unit test to verify that instance implements ISyncCrudlRepository interface.
    /// </summary>
    [TestMethod]
    public void Instance_TypeCheck_ImplementsISyncCrudlRepository()
    {
        // Arrange (Given)

        // Act (When)
        var instance = new StubSyncCrudlRepository();

        // Assert (Then)
        Assert.IsInstanceOfType<ISyncCrudlRepository<TestEntityRecord, long>>(
            instance,
            "Instance should implement ISyncCrudlRepository<TestEntityRecord, long>.");
    }

    /// <summary>
    /// Unit test to verify that instance implements ISyncCrudRepository interface.
    /// </summary>
    [TestMethod]
    public void Instance_TypeCheck_ImplementsISyncCrudRepository()
    {
        // Arrange (Given)

        // Act (When)
        var instance = new StubSyncCrudlRepository();

        // Assert (Then)
        Assert.IsInstanceOfType<ISyncCrudRepository<TestEntityRecord, long>>(
            instance,
            "Instance should implement ISyncCrudRepository<TestEntityRecord, long>.");
    }

    /// <summary>
    /// Unit test to verify that instance implements ISyncListOnlyRepository interface.
    /// </summary>
    [TestMethod]
    public void Instance_TypeCheck_ImplementsISyncListOnlyRepository()
    {
        // Arrange (Given)

        // Act (When)
        var instance = new StubSyncCrudlRepository();

        // Assert (Then)
        Assert.IsInstanceOfType<ISyncListOnlyRepository<TestEntityRecord, long>>(
            instance,
            "Instance should implement ISyncListOnlyRepository<TestEntityRecord, long>.");
    }

    /// <summary>
    /// Unit test to verify that instance implements IRepository marker interface.
    /// </summary>
    [TestMethod]
    public void Instance_TypeCheck_ImplementsIRepository()
    {
        // Arrange (Given)

        // Act (When)
        var instance = new StubSyncCrudlRepository();

        // Assert (Then)
        Assert.IsInstanceOfType<IRepository<TestEntityRecord, long>>(
            instance,
            "Instance should implement IRepository<TestEntityRecord, long>.");
    }

    /// <summary>
    /// Unit test to verify that instance inherits from BaseClassWithLogging.
    /// </summary>
    [TestMethod]
    public void Instance_TypeCheck_InheritsBaseClassWithLogging()
    {
        // Arrange (Given)

        // Act (When)
        var instance = new StubSyncCrudlRepository();

        // Assert (Then)
        Assert.IsInstanceOfType<BaseClassWithLogging>(
            instance,
            "Instance should inherit from BaseClassWithLogging.");
    }

    /// <summary>
    /// Unit test to verify that Create can be called and returns expected entity.
    /// </summary>
    [TestMethod]
    public void Create_ValidEntity_ReturnsCreatedEntity()
    {
        // Arrange (Given)
        var entity = new TestEntityRecord { Id = 1, Name = "Test" };
        var instance = new StubSyncCrudlRepository
        {
            EntityToReturn = entity,
        };

        // Act (When)
        TestEntityRecord result = instance.Create(entity);

        // Assert (Then)
        Assert.AreSame(
            entity,
            result,
            "Create should return the entity provided by the concrete implementation.");
    }

    /// <summary>
    /// Unit test to verify that Read can be called and returns expected entity.
    /// </summary>
    [TestMethod]
    public void Read_ExistingId_ReturnsEntity()
    {
        // Arrange (Given)
        var expectedEntity = new TestEntityRecord { Id = 1, Name = "Test" };
        var instance = new StubSyncCrudlRepository
        {
            EntityToReturn = expectedEntity,
        };

        // Act (When)
        TestEntityRecord? result = instance.Read(1);

        // Assert (Then)
        Assert.AreSame(
            expectedEntity,
            result,
            "Read should return the entity provided by the concrete implementation.");
    }

    /// <summary>
    /// Unit test to verify that Read returns null when entity is not found.
    /// </summary>
    [TestMethod]
    public void Read_NonExistingId_ReturnsNull()
    {
        // Arrange (Given)
        var instance = new StubSyncCrudlRepository
        {
            EntityToReturn = null,
        };

        // Act (When)
        TestEntityRecord? result = instance.Read(999);

        // Assert (Then)
        Assert.IsNull(
            result,
            "Read should return null when the entity is not found.");
    }

    /// <summary>
    /// Unit test to verify that Read captures the identifier passed by the caller.
    /// </summary>
    [TestMethod]
    public void Read_ValidId_CapturesIdentifier()
    {
        // Arrange (Given)
        long expectedId = 42;
        var instance = new StubSyncCrudlRepository();

        // Act (When)
        instance.Read(expectedId);

        // Assert (Then)
        Assert.AreEqual(
            expectedId,
            instance.LastReadId,
            "Read should receive the identifier passed by the caller.");
    }

    /// <summary>
    /// Unit test to verify that Update can be called and returns expected entity.
    /// </summary>
    [TestMethod]
    public void Update_ValidEntity_ReturnsUpdatedEntity()
    {
        // Arrange (Given)
        var entity = new TestEntityRecord { Id = 1, Name = "Updated" };
        var instance = new StubSyncCrudlRepository
        {
            EntityToReturn = entity,
        };

        // Act (When)
        TestEntityRecord result = instance.Update(entity);

        // Assert (Then)
        Assert.AreSame(
            entity,
            result,
            "Update should return the entity provided by the concrete implementation.");
    }

    /// <summary>
    /// Unit test to verify that Delete can be called successfully.
    /// </summary>
    [TestMethod]
    public void Delete_ValidId_CompletesSuccessfully()
    {
        // Arrange (Given)
        long idToDelete = 1;
        var instance = new StubSyncCrudlRepository();

        // Act (When)
        instance.Delete(idToDelete);

        // Assert (Then)
        Assert.AreEqual(
            idToDelete,
            instance.LastDeleteId,
            "Delete should receive the identifier passed by the caller.");
    }

    /// <summary>
    /// Unit test to verify that List can be called and returns expected results.
    /// </summary>
    [TestMethod]
    public void List_Called_ReturnsExpectedResults()
    {
        // Arrange (Given)
        var expectedEntities = new List<TestEntityRecord>
        {
            new TestEntityRecord { Id = 1, Name = "First" },
            new TestEntityRecord { Id = 2, Name = "Second" },
        };

        var instance = new StubSyncCrudlRepository
        {
            EntitiesToReturn = expectedEntities,
        };

        // Act (When)
        IList<TestEntityRecord> result = instance.List();

        // Assert (Then)
        Assert.AreSame(
            expectedEntities,
            result,
            "List should return the collection provided by the concrete implementation.");
    }

    /// <summary>
    /// Unit test to verify that List returns empty collection when no entities exist.
    /// </summary>
    [TestMethod]
    public void List_NoEntities_ReturnsEmptyCollection()
    {
        // Arrange (Given)
        var instance = new StubSyncCrudlRepository
        {
            EntitiesToReturn = new List<TestEntityRecord>(),
        };

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
    /// Concrete stub for testing the abstract BaseSyncCrudlRepository.
    /// </summary>
    private sealed class StubSyncCrudlRepository
        : BaseSyncCrudlRepository<TestEntityRecord, long>
    {
        public TestEntityRecord? EntityToReturn { get; set; }

        public IList<TestEntityRecord> EntitiesToReturn { get; set; }
            = new List<TestEntityRecord>();

        public long? LastReadId { get; private set; }

        public long? LastDeleteId { get; private set; }

        public StubSyncCrudlRepository()
            : base()
        {
        }

        public StubSyncCrudlRepository(ILogger logger)
            : base(logger)
        {
        }

        public override TestEntityRecord Create(TestEntityRecord entity)
        {
            return this.EntityToReturn ?? entity;
        }

        public override TestEntityRecord? Read(long id)
        {
            this.LastReadId = id;
            return this.EntityToReturn;
        }

        public override TestEntityRecord Update(TestEntityRecord entity)
        {
            return this.EntityToReturn ?? entity;
        }

        public override void Delete(long id)
        {
            this.LastDeleteId = id;
        }

        public override IList<TestEntityRecord> List()
        {
            return this.EntitiesToReturn;
        }
    }

    #endregion Private Classes
}