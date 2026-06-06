namespace Roadbed.Test.Unit.Crud;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Crud;
using Roadbed.Crud.Repositories.Sync;
using Roadbed.Crud.Services.Sync;

/// <summary>
/// Contains unit tests for verifying the behavior of the
/// <see cref="BaseSyncBtService{TEntity, TId}"/> class.
/// </summary>
[TestClass]
public class BaseSyncBtServiceTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that constructor creates an instance with valid parameters.
    /// </summary>
    [TestMethod]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        // Arrange (Given)
        var repository = new MockSyncBtRepository();
        ILogger logger = NullLogger.Instance;

        // Act (When)
        var instance = new StubSyncBtService(repository, logger);

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
        ISyncBtRepository<TestEntityRecord, long>? nullRepository = null;
        ILogger logger = NullLogger.Instance;
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            new StubSyncBtService(nullRepository!, logger);
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
    /// Unit test to verify that instance implements ISyncBtService interface.
    /// </summary>
    [TestMethod]
    public void Instance_TypeCheck_ImplementsISyncBtService()
    {
        // Arrange (Given)
        var instance = new StubSyncBtService(
            new MockSyncBtRepository(),
            NullLogger.Instance);

        // Act (When)

        // Assert (Then)
        Assert.IsInstanceOfType<ISyncBtService<TestEntityRecord, long>>(
            instance,
            "Instance should implement ISyncBtService<TestEntityRecord, long>.");
    }

    /// <summary>
    /// Unit test to verify that instance also implements ISyncBulkOnlyService via inheritance.
    /// </summary>
    [TestMethod]
    public void Instance_TypeCheck_ImplementsISyncBulkOnlyService()
    {
        // Arrange (Given)
        var instance = new StubSyncBtService(
            new MockSyncBtRepository(),
            NullLogger.Instance);

        // Act (When)

        // Assert (Then)
        Assert.IsInstanceOfType<ISyncBulkOnlyService<TestEntityRecord, long>>(
            instance,
            "Bt service should also implement ISyncBulkOnlyService via inheritance.");
    }

    /// <summary>
    /// Unit test to verify that instance inherits from BaseClassWithLogging.
    /// </summary>
    [TestMethod]
    public void Instance_TypeCheck_InheritsBaseClassWithLogging()
    {
        // Arrange (Given)
        var instance = new StubSyncBtService(
            new MockSyncBtRepository(),
            NullLogger.Instance);

        // Act (When)

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
        var repository = new MockSyncBtRepository();
        var instance = new StubSyncBtService(repository, NullLogger.Instance);

        // Act (When)
        ISyncBtRepository<TestEntityRecord, long> result = instance.ExposedRepository;

        // Assert (Then)
        Assert.AreSame(
            repository,
            result,
            "Repository property should return the repository passed to the constructor.");
    }

    /// <summary>
    /// Unit test to verify that BulkInsert delegates to the repository.
    /// </summary>
    [TestMethod]
    public void BulkInsert_Called_DelegatesToRepository()
    {
        // Arrange (Given)
        var rows = new List<TestEntityRecord>
        {
            new TestEntityRecord { Id = 1, Name = "First" },
        };

        var repository = new MockSyncBtRepository
        {
            BulkInsertResult = rows.Count,
        };

        var instance = new StubSyncBtService(repository, NullLogger.Instance);

        // Act (When)
        long result = instance.BulkInsert("activity-1", rows);

        // Assert (Then)
        Assert.AreEqual(
            (long)rows.Count,
            result,
            "BulkInsert should return the value from the repository.");
        Assert.AreEqual(
            1,
            repository.BulkInsertCallCount,
            "BulkInsert should delegate to the repository exactly once.");
        Assert.AreEqual(
            "activity-1",
            repository.LastBulkInsertActivityId,
            "BulkInsert should pass the activity id to the repository.");
        Assert.AreSame(
            rows,
            repository.LastBulkInsertRows,
            "BulkInsert should pass the rows collection to the repository.");
    }

    /// <summary>
    /// Unit test to verify that BulkInsert throws ArgumentException when activityId is blank.
    /// </summary>
    /// <param name="activityId">Blank activity id supplied by DataRow.</param>
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void BulkInsert_BlankActivityId_ThrowsArgumentException(string? activityId)
    {
        // Arrange (Given)
        var repository = new MockSyncBtRepository();
        var instance = new StubSyncBtService(repository, NullLogger.Instance);
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            instance.BulkInsert(activityId!, new List<TestEntityRecord>());
        }
        catch (ArgumentException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "BulkInsert should throw ArgumentException when activityId is blank.");
        Assert.AreEqual(
            0,
            repository.BulkInsertCallCount,
            "Repository should not be called when activityId validation fails.");
    }

    /// <summary>
    /// Unit test to verify that BulkInsert throws ArgumentNullException when rows is null.
    /// </summary>
    [TestMethod]
    public void BulkInsert_NullRows_ThrowsArgumentNullException()
    {
        // Arrange (Given)
        var repository = new MockSyncBtRepository();
        var instance = new StubSyncBtService(repository, NullLogger.Instance);
        IList<TestEntityRecord>? nullRows = null;
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            instance.BulkInsert("activity-1", nullRows!);
        }
        catch (ArgumentNullException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "BulkInsert should throw ArgumentNullException when rows is null.");
        Assert.AreEqual(
            0,
            repository.BulkInsertCallCount,
            "Repository should not be called when rows validation fails.");
    }

    /// <summary>
    /// Unit test to verify that Truncate delegates to the repository.
    /// </summary>
    [TestMethod]
    public void Truncate_Called_DelegatesToRepository()
    {
        // Arrange (Given)
        var repository = new MockSyncBtRepository();
        var instance = new StubSyncBtService(repository, NullLogger.Instance);

        // Act (When)
        instance.Truncate();

        // Assert (Then)
        Assert.AreEqual(
            1,
            repository.TruncateCallCount,
            "Truncate should delegate to the repository exactly once.");
    }

    #endregion Public Methods

    #region Private Classes

    /// <summary>
    /// Concrete subclass exposing the protected ctor and Repository property of
    /// <see cref="BaseSyncBtService{TEntity, TId}"/>.
    /// </summary>
    private sealed class StubSyncBtService
        : BaseSyncBtService<TestEntityRecord, long>
    {
        public StubSyncBtService(
            ISyncBtRepository<TestEntityRecord, long> repository,
            ILogger logger)
            : base(repository, logger)
        {
        }

        public ISyncBtRepository<TestEntityRecord, long> ExposedRepository
            => this.Repository;
    }

    /// <summary>
    /// In-line mock implementing the sync Bt repository for service tests.
    /// </summary>
    private sealed class MockSyncBtRepository
        : ISyncBtRepository<TestEntityRecord, long>
    {
        public long BulkInsertResult { get; set; }

        public int BulkInsertCallCount { get; private set; }

        public int TruncateCallCount { get; private set; }

        public string? LastBulkInsertActivityId { get; private set; }

        public IList<TestEntityRecord>? LastBulkInsertRows { get; private set; }

        public long BulkInsert(string activityId, IList<TestEntityRecord> rows)
        {
            this.BulkInsertCallCount++;
            this.LastBulkInsertActivityId = activityId;
            this.LastBulkInsertRows = rows;
            return this.BulkInsertResult;
        }

        public void Truncate()
        {
            this.TruncateCallCount++;
        }
    }

    #endregion Private Classes
}
