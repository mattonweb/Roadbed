namespace Roadbed.Test.Unit.Crud;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Crud;
using Roadbed.Crud.Repositories.Async;
using Roadbed.Crud.Services.Async;

/// <summary>
/// Contains unit tests for verifying the behavior of the
/// <see cref="BaseAsyncBulkOnlyService{TEntity, TId}"/> class.
/// </summary>
[TestClass]
public class BaseAsyncBulkOnlyServiceTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that constructor creates an instance with valid parameters.
    /// </summary>
    [TestMethod]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        // Arrange (Given)
        var repository = new MockAsyncBulkOnlyRepository();
        ILogger logger = NullLogger.Instance;

        // Act (When)
        var instance = new StubAsyncBulkOnlyService(repository, logger);

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
        IAsyncBulkOnlyRepository<TestEntityRecord, long>? nullRepository = null;
        ILogger logger = NullLogger.Instance;
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            new StubAsyncBulkOnlyService(nullRepository!, logger);
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
    /// Unit test to verify that instance implements IAsyncBulkOnlyService interface.
    /// </summary>
    [TestMethod]
    public void Instance_TypeCheck_ImplementsIAsyncBulkOnlyService()
    {
        // Arrange (Given)
        var instance = new StubAsyncBulkOnlyService(
            new MockAsyncBulkOnlyRepository(),
            NullLogger.Instance);

        // Act (When)

        // Assert (Then)
        Assert.IsInstanceOfType<IAsyncBulkOnlyService<TestEntityRecord, long>>(
            instance,
            "Instance should implement IAsyncBulkOnlyService<TestEntityRecord, long>.");
    }

    /// <summary>
    /// Unit test to verify that instance inherits from BaseClassWithLogging.
    /// </summary>
    [TestMethod]
    public void Instance_TypeCheck_InheritsBaseClassWithLogging()
    {
        // Arrange (Given)
        var instance = new StubAsyncBulkOnlyService(
            new MockAsyncBulkOnlyRepository(),
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
        var repository = new MockAsyncBulkOnlyRepository();
        var instance = new StubAsyncBulkOnlyService(repository, NullLogger.Instance);

        // Act (When)
        IAsyncBulkOnlyRepository<TestEntityRecord, long> result =
            instance.ExposedRepository;

        // Assert (Then)
        Assert.AreSame(
            repository,
            result,
            "Repository property should return the repository passed to the constructor.");
    }

    /// <summary>
    /// Unit test to verify that BulkInsertAsync delegates to the repository.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task BulkInsertAsync_Called_DelegatesToRepository()
    {
        // Arrange (Given)
        var rows = new List<TestEntityRecord>
        {
            new TestEntityRecord { Id = 1, Name = "First" },
            new TestEntityRecord { Id = 2, Name = "Second" },
        };

        var repository = new MockAsyncBulkOnlyRepository
        {
            BulkInsertResult = rows.Count,
        };

        var instance = new StubAsyncBulkOnlyService(repository, NullLogger.Instance);

        // Act (When)
        long result = await instance.BulkInsertAsync("activity-1", rows);

        // Assert (Then)
        Assert.AreEqual(
            (long)rows.Count,
            result,
            "BulkInsertAsync should return the value from the repository.");
        Assert.AreEqual(
            1,
            repository.BulkInsertCallCount,
            "BulkInsertAsync should delegate to the repository exactly once.");
        Assert.AreEqual(
            "activity-1",
            repository.LastActivityId,
            "BulkInsertAsync should pass the activity id to the repository.");
        Assert.AreSame(
            rows,
            repository.LastRows,
            "BulkInsertAsync should pass the rows collection to the repository.");
    }

    /// <summary>
    /// Unit test to verify that BulkInsertAsync passes CancellationToken to the repository.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task BulkInsertAsync_WithCancellationToken_PassesTokenToRepository()
    {
        // Arrange (Given)
        using var cts = new CancellationTokenSource();
        CancellationToken expectedToken = cts.Token;

        var repository = new MockAsyncBulkOnlyRepository();
        var instance = new StubAsyncBulkOnlyService(repository, NullLogger.Instance);

        // Act (When)
        await instance.BulkInsertAsync("activity-1", new List<TestEntityRecord>(), expectedToken);

        // Assert (Then)
        Assert.AreEqual(
            expectedToken,
            repository.LastCancellationToken,
            "BulkInsertAsync should pass the CancellationToken to the repository.");
    }

    /// <summary>
    /// Unit test to verify that BulkInsertAsync throws ArgumentException when activityId is blank.
    /// </summary>
    /// <param name="activityId">Blank activity id supplied by DataRow.</param>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public async Task BulkInsertAsync_BlankActivityId_ThrowsArgumentException(string? activityId)
    {
        // Arrange (Given)
        var repository = new MockAsyncBulkOnlyRepository();
        var instance = new StubAsyncBulkOnlyService(repository, NullLogger.Instance);
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            await instance.BulkInsertAsync(activityId!, new List<TestEntityRecord>());
        }
        catch (ArgumentException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "BulkInsertAsync should throw ArgumentException when activityId is blank.");
        Assert.AreEqual(
            0,
            repository.BulkInsertCallCount,
            "Repository should not be called when activityId validation fails.");
    }

    /// <summary>
    /// Unit test to verify that BulkInsertAsync throws ArgumentNullException when rows is null.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task BulkInsertAsync_NullRows_ThrowsArgumentNullException()
    {
        // Arrange (Given)
        var repository = new MockAsyncBulkOnlyRepository();
        var instance = new StubAsyncBulkOnlyService(repository, NullLogger.Instance);
        IList<TestEntityRecord>? nullRows = null;
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            await instance.BulkInsertAsync("activity-1", nullRows!);
        }
        catch (ArgumentNullException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "BulkInsertAsync should throw ArgumentNullException when rows is null.");
        Assert.AreEqual(
            0,
            repository.BulkInsertCallCount,
            "Repository should not be called when rows validation fails.");
    }

    #endregion Public Methods

    #region Private Classes

    /// <summary>
    /// Concrete subclass exposing the protected ctor and Repository property of
    /// <see cref="BaseAsyncBulkOnlyService{TEntity, TId}"/>.
    /// </summary>
    private sealed class StubAsyncBulkOnlyService
        : BaseAsyncBulkOnlyService<TestEntityRecord, long>
    {
        public StubAsyncBulkOnlyService(
            IAsyncBulkOnlyRepository<TestEntityRecord, long> repository,
            ILogger logger)
            : base(repository, logger)
        {
        }

        public IAsyncBulkOnlyRepository<TestEntityRecord, long> ExposedRepository
            => this.Repository;
    }

    /// <summary>
    /// In-line mock implementing the async bulk-only repository for service tests.
    /// </summary>
    private sealed class MockAsyncBulkOnlyRepository
        : IAsyncBulkOnlyRepository<TestEntityRecord, long>
    {
        public long BulkInsertResult { get; set; }

        public int BulkInsertCallCount { get; private set; }

        public string? LastActivityId { get; private set; }

        public IList<TestEntityRecord>? LastRows { get; private set; }

        public CancellationToken LastCancellationToken { get; private set; }

        public Task<long> BulkInsertAsync(
            string activityId,
            IList<TestEntityRecord> rows,
            CancellationToken cancellationToken = default)
        {
            this.BulkInsertCallCount++;
            this.LastActivityId = activityId;
            this.LastRows = rows;
            this.LastCancellationToken = cancellationToken;
            return Task.FromResult(this.BulkInsertResult);
        }
    }

    #endregion Private Classes
}
