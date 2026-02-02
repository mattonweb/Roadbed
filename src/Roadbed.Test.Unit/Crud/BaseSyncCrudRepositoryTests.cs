namespace Roadbed.Test.Unit.Crud;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Crud;
using Roadbed.Crud.Repositories.Sync;

/// <summary>
/// Contains unit tests for verifying the behavior of the
/// <see cref="BaseSyncCrudRepository{TEntity, TId}"/> abstract class.
/// </summary>
[TestClass]
public class BaseSyncCrudRepositoryTests
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
        var instance = new StubSyncCrudRepository();

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
        var instance = new StubSyncCrudRepository(logger);

        // Assert (Then)
        Assert.IsNotNull(
            instance,
            "Instance should be created successfully with the logger constructor.");
    }

    /// <summary>
    /// Unit test to verify that instance implements ISyncCrudRepository interface.
    /// </summary>
    [TestMethod]
    public void Instance_TypeCheck_ImplementsISyncCrudRepository()
    {
        // Arrange (Given)

        // Act (When)
        var instance = new StubSyncCrudRepository();

        // Assert (Then)
        Assert.IsInstanceOfType<ISyncCrudRepository<TestEntityRecord, long>>(
            instance,
            "Instance should implement ISyncCrudRepository<TestEntityRecord, long>.");
    }

    /// <summary>
    /// Unit test to verify that instance implements IRepository marker interface.
    /// </summary>
    [TestMethod]
    public void Instance_TypeCheck_ImplementsIRepository()
    {
        // Arrange (Given)

        // Act (When)
        var instance = new StubSyncCrudRepository();

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
        var instance = new StubSyncCrudRepository();

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
        var instance = new StubSyncCrudRepository
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
        var instance = new StubSyncCrudRepository
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
        var instance = new StubSyncCrudRepository
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
        var instance = new StubSyncCrudRepository();

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
        var instance = new StubSyncCrudRepository
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
        var instance = new StubSyncCrudRepository();

        // Act (When)
        instance.Delete(idToDelete);

        // Assert (Then)
        Assert.AreEqual(
            idToDelete,
            instance.LastDeleteId,
            "Delete should receive the identifier passed by the caller.");
    }

    #endregion Public Methods

    #region Private Classes

    /// <summary>
    /// Concrete stub for testing the abstract BaseSyncCrudRepository.
    /// </summary>
    private sealed class StubSyncCrudRepository
        : BaseSyncCrudRepository<TestEntityRecord, long>
    {
        public TestEntityRecord? EntityToReturn { get; set; }

        public long? LastReadId { get; private set; }

        public long? LastDeleteId { get; private set; }

        public StubSyncCrudRepository()
            : base()
        {
        }

        public StubSyncCrudRepository(ILogger logger)
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
    }

    #endregion Private Classes
}