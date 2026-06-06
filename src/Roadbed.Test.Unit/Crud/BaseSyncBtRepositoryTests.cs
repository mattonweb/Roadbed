namespace Roadbed.Test.Unit.Crud;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Crud;
using Roadbed.Crud.Operations.Sync;
using Roadbed.Crud.Repositories.Sync;

/// <summary>
/// Contains unit tests for verifying the behavior of the
/// <see cref="BaseSyncBtRepository{TEntity, TId}"/> abstract class.
/// </summary>
[TestClass]
public class BaseSyncBtRepositoryTests
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
        var instance = new StubSyncBtRepository();

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
        var instance = new StubSyncBtRepository(logger);

        // Assert (Then)
        Assert.IsNotNull(
            instance,
            "Instance should be created successfully with the logger constructor.");
    }

    /// <summary>
    /// Unit test to verify that instance implements ISyncBtRepository interface.
    /// </summary>
    [TestMethod]
    public void Instance_TypeCheck_ImplementsISyncBtRepository()
    {
        // Arrange (Given)

        // Act (When)
        var instance = new StubSyncBtRepository();

        // Assert (Then)
        Assert.IsInstanceOfType<ISyncBtRepository<TestEntityRecord, long>>(
            instance,
            "Instance should implement ISyncBtRepository<TestEntityRecord, long>.");
    }

    /// <summary>
    /// Unit test to verify that instance also implements ISyncBulkOnlyRepository
    /// through inheritance.
    /// </summary>
    [TestMethod]
    public void Instance_TypeCheck_ImplementsISyncBulkOnlyRepository()
    {
        // Arrange (Given)

        // Act (When)
        var instance = new StubSyncBtRepository();

        // Assert (Then)
        Assert.IsInstanceOfType<ISyncBulkOnlyRepository<TestEntityRecord, long>>(
            instance,
            "Bt repository should also implement ISyncBulkOnlyRepository via inheritance.");
    }

    /// <summary>
    /// Unit test to verify that instance implements ISyncTruncateOperation.
    /// </summary>
    [TestMethod]
    public void Instance_TypeCheck_ImplementsISyncTruncateOperation()
    {
        // Arrange (Given)

        // Act (When)
        var instance = new StubSyncBtRepository();

        // Assert (Then)
        Assert.IsInstanceOfType<ISyncTruncateOperation<TestEntityRecord, long>>(
            instance,
            "Instance should implement ISyncTruncateOperation<TestEntityRecord, long>.");
    }

    /// <summary>
    /// Unit test to verify that instance implements IRepository marker interface.
    /// </summary>
    [TestMethod]
    public void Instance_TypeCheck_ImplementsIRepository()
    {
        // Arrange (Given)

        // Act (When)
        var instance = new StubSyncBtRepository();

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
        var instance = new StubSyncBtRepository();

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

        var instance = new StubSyncBtRepository
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
        var instance = new StubSyncBtRepository();

        // Act (When)
        instance.BulkInsert(expectedActivityId, expectedRows);

        // Assert (Then)
        Assert.AreEqual(
            expectedActivityId,
            instance.LastBulkInsertActivityId,
            "BulkInsert should pass the activity id to the concrete implementation.");
        Assert.AreSame(
            expectedRows,
            instance.LastBulkInsertRows,
            "BulkInsert should pass the rows collection to the concrete implementation.");
    }

    /// <summary>
    /// Unit test to verify that Truncate invokes the implementation.
    /// </summary>
    [TestMethod]
    public void Truncate_Called_InvokesImplementation()
    {
        // Arrange (Given)
        var instance = new StubSyncBtRepository();

        // Act (When)
        instance.Truncate();

        // Assert (Then)
        Assert.AreEqual(
            1,
            instance.TruncateInvocationCount,
            "Truncate should call the concrete implementation exactly once.");
    }

    #endregion Public Methods

    #region Private Classes

    /// <summary>
    /// Concrete stub for testing the abstract BaseSyncBtRepository.
    /// </summary>
    private sealed class StubSyncBtRepository
        : BaseSyncBtRepository<TestEntityRecord, long>
    {
        public long RowsToReturn { get; set; }

        public string? LastBulkInsertActivityId { get; private set; }

        public IList<TestEntityRecord>? LastBulkInsertRows { get; private set; }

        public int TruncateInvocationCount { get; private set; }

        public StubSyncBtRepository()
            : base()
        {
        }

        public StubSyncBtRepository(ILogger logger)
            : base(logger)
        {
        }

        public override long BulkInsert(string activityId, IList<TestEntityRecord> rows)
        {
            this.LastBulkInsertActivityId = activityId;
            this.LastBulkInsertRows = rows;
            return this.RowsToReturn;
        }

        public override void Truncate()
        {
            this.TruncateInvocationCount++;
        }
    }

    #endregion Private Classes
}
