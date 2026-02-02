namespace Roadbed.Test.Unit.Crud;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Crud;
using Roadbed.Crud.Repositories.Sync;

/// <summary>
/// Contains unit tests for verifying the behavior of the
/// <see cref="BaseSyncListOnlyRepository{TEntity, TId}"/> abstract class.
/// </summary>
[TestClass]
public class BaseSyncListOnlyRepositoryTests
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
        var instance = new StubSyncListOnlyRepository();

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
        var instance = new StubSyncListOnlyRepository(logger);

        // Assert (Then)
        Assert.IsNotNull(
            instance,
            "Instance should be created successfully with the logger constructor.");
    }

    /// <summary>
    /// Unit test to verify that instance implements ISyncListOnlyRepository interface.
    /// </summary>
    [TestMethod]
    public void Instance_TypeCheck_ImplementsISyncListOnlyRepository()
    {
        // Arrange (Given)

        // Act (When)
        var instance = new StubSyncListOnlyRepository();

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
        var instance = new StubSyncListOnlyRepository();

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
        var instance = new StubSyncListOnlyRepository();

        // Assert (Then)
        Assert.IsInstanceOfType<BaseClassWithLogging>(
            instance,
            "Instance should inherit from BaseClassWithLogging.");
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

        var instance = new StubSyncListOnlyRepository
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
        var instance = new StubSyncListOnlyRepository
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
    /// Concrete stub for testing the abstract BaseSyncListOnlyRepository.
    /// </summary>
    private sealed class StubSyncListOnlyRepository
        : BaseSyncListOnlyRepository<TestEntityRecord, long>
    {
        public IList<TestEntityRecord> EntitiesToReturn { get; set; }
            = new List<TestEntityRecord>();

        public StubSyncListOnlyRepository()
            : base()
        {
        }

        public StubSyncListOnlyRepository(ILogger logger)
            : base(logger)
        {
        }

        public override IList<TestEntityRecord> List()
        {
            return this.EntitiesToReturn;
        }
    }

    #endregion Private Classes
}