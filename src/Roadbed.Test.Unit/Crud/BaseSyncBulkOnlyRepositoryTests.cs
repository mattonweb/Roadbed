namespace Roadbed.Test.Unit.Crud;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Crud;
using Roadbed.Crud.Operations.Sync;
using Roadbed.Crud.Repositories.Sync;

/// <summary>
/// Contains unit tests for verifying the behavior of the
/// <see cref="BaseSyncBulkOnlyRepository{TEntity, TId}"/> abstract class.
/// </summary>
[TestClass]
public class BaseSyncBulkOnlyRepositoryTests
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
        var instance = new StubSyncBulkOnlyRepository();

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
        var instance = new StubSyncBulkOnlyRepository(logger);

        // Assert (Then)
        Assert.IsNotNull(
            instance,
            "Instance should be created successfully with the logger constructor.");
    }

    /// <summary>
    /// Unit test to verify that instance implements ISyncBulkOnlyRepository interface.
    /// </summary>
    [TestMethod]
    public void Instance_TypeCheck_ImplementsISyncBulkOnlyRepository()
    {
        // Arrange (Given)

        // Act (When)
        var instance = new StubSyncBulkOnlyRepository();

        // Assert (Then)
        Assert.IsInstanceOfType<ISyncBulkOnlyRepository<TestEntityRecord, long>>(
            instance,
            "Instance should implement ISyncBulkOnlyRepository<TestEntityRecord, long>.");
    }

    /// <summary>
    /// Unit test to verify that instance implements ISyncBulkInsertOperation.
    /// </summary>
    [TestMethod]
    public void Instance_TypeCheck_ImplementsISyncBulkInsertOperation()
    {
        // Arrange (Given)

        // Act (When)
        var instance = new StubSyncBulkOnlyRepository();

        // Assert (Then)
        Assert.IsInstanceOfType<ISyncBulkInsertOperation<TestEntityRecord, long>>(
            instance,
            "Instance should implement ISyncBulkInsertOperation<TestEntityRecord, long>.");
    }

    /// <summary>
    /// Unit test to verify that instance implements IRepository marker interface.
    /// </summary>
    [TestMethod]
    public void Instance_TypeCheck_ImplementsIRepository()
    {
        // Arrange (Given)

        // Act (When)
        var instance = new StubSyncBulkOnlyRepository();

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
        var instance = new StubSyncBulkOnlyRepository();

        // Assert (Then)
        Assert.IsInstanceOfType<BaseClassWithLogging>(
            instance,
            "Instance should inherit from BaseClassWithLogging.");
    }

    /// <summary>
    /// Unit test to verify that BulkInsert returns expected row count.
    /// </summary>
    [TestMethod]
    public void BulkInsert_Called_ReturnsExpectedRowCount()
    {
        // Arrange (Given)
        var rows = new List<TestEntityRecord>
        {
            new TestEntityRecord { Id = 1, Name = "First" },
            new TestEntityRecord { Id = 2, Name = "Second" },
        };

        var instance = new StubSyncBulkOnlyRepository
        {
            RowsToReturn = rows.Count,
        };

        // Act (When)
        long result = instance.BulkInsert("activity-1", rows);

        // Assert (Then)
        Assert.AreEqual(
            (long)rows.Count,
            result,
            "BulkInsert should return the row count supplied by the concrete implementation.");
    }

    /// <summary>
    /// Unit test to verify that BulkInsert passes its arguments to the implementation.
    /// </summary>
    [TestMethod]
    public void BulkInsert_Called_PassesArgumentsToImplementation()
    {
        // Arrange (Given)
        const string expectedActivityId = "activity-42";
        var expectedRows = new List<TestEntityRecord>
        {
            new TestEntityRecord { Id = 1, Name = "Row 1" },
        };
        var instance = new StubSyncBulkOnlyRepository();

        // Act (When)
        instance.BulkInsert(expectedActivityId, expectedRows);

        // Assert (Then)
        Assert.AreEqual(
            expectedActivityId,
            instance.LastActivityId,
            "BulkInsert should pass the activity id to the concrete implementation.");
        Assert.AreSame(
            expectedRows,
            instance.LastRows,
            "BulkInsert should pass the rows collection to the concrete implementation.");
    }

    #endregion Public Methods

    #region Private Classes

    /// <summary>
    /// Concrete stub for testing the abstract BaseSyncBulkOnlyRepository.
    /// </summary>
    private sealed class StubSyncBulkOnlyRepository
        : BaseSyncBulkOnlyRepository<TestEntityRecord, long>
    {
        public long RowsToReturn { get; set; }

        public string? LastActivityId { get; private set; }

        public IList<TestEntityRecord>? LastRows { get; private set; }

        public StubSyncBulkOnlyRepository()
            : base()
        {
        }

        public StubSyncBulkOnlyRepository(ILogger logger)
            : base(logger)
        {
        }

        public override long BulkInsert(string activityId, IList<TestEntityRecord> rows)
        {
            this.LastActivityId = activityId;
            this.LastRows = rows;
            return this.RowsToReturn;
        }
    }

    #endregion Private Classes
}
