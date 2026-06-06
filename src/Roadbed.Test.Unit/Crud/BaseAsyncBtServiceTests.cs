namespace Roadbed.Test.Unit.Crud;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Crud;
using Roadbed.Crud.Repositories.Async;
using Roadbed.Crud.Services.Async;

/// <summary>
/// Contains unit tests for verifying the behavior of the
/// <see cref="BaseAsyncBtService{TEntity, TId}"/> class.
/// </summary>
[TestClass]
public class BaseAsyncBtServiceTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that constructor creates an instance with valid parameters.
    /// </summary>
    [TestMethod]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        // Arrange (Given)
        var repository = new MockAsyncBtRepository();
        ILogger logger = NullLogger.Instance;

        // Act (When)
        var instance = new StubAsyncBtService(repository, logger);

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
        IAsyncBtRepository<TestEntityRecord, long>? nullRepository = null;
        ILogger logger = NullLogger.Instance;
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            new StubAsyncBtService(nullRepository!, logger);
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
    /// Unit test to verify that instance implements IAsyncBtService interface.
    /// </summary>
    [TestMethod]
    public void Instance_TypeCheck_ImplementsIAsyncBtService()
    {
        // Arrange (Given)
        var instance = new StubAsyncBtService(
            new MockAsyncBtRepository(),
            NullLogger.Instance);

        // Act (When)

        // Assert (Then)
        Assert.IsInstanceOfType<IAsyncBtService<TestEntityRecord, long>>(
            instance,
            "Instance should implement IAsyncBtService<TestEntityRecord, long>.");
    }

    /// <summary>
    /// Unit test to verify that instance also implements IAsyncBulkOnlyService via inheritance.
    /// </summary>
    [TestMethod]
    public void Instance_TypeCheck_ImplementsIAsyncBulkOnlyService()
    {
        // Arrange (Given)
        var instance = new StubAsyncBtService(
            new MockAsyncBtRepository(),
            NullLogger.Instance);

        // Act (When)

        // Assert (Then)
        Assert.IsInstanceOfType<IAsyncBulkOnlyService<TestEntityRecord, long>>(
            instance,
            "Bt service should also implement IAsyncBulkOnlyService via inheritance.");
    }

    /// <summary>
    /// Unit test to verify that instance inherits from BaseClassWithLogging.
    /// </summary>
    [TestMethod]
    public void Instance_TypeCheck_InheritsBaseClassWithLogging()
    {
        // Arrange (Given)
        var instance = new StubAsyncBtService(
            new MockAsyncBtRepository(),
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
        var repository = new MockAsyncBtRepository();
        var instance = new StubAsyncBtService(repository, NullLogger.Instance);

        // Act (When)
        IAsyncBtRepository<TestEntityRecord, long> result = instance.ExposedRepository;

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
        };

        var repository = new MockAsyncBtRepository
        {
            BulkInsertResult = rows.Count,
        };

        var instance = new StubAsyncBtService(repository, NullLogger.Instance);

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
        Assert.AreSame(
            rows,
            repository.LastBulkInsertRows,
            "BulkInsertAsync should pass the rows collection to the repository.");
        Assert.AreEqual(
            "activity-1",
            repository.LastBulkInsertActivityId,
            "BulkInsertAsync should pass the activity id to the repository.");
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
        var repository = new MockAsyncBtRepository();
        var instance = new StubAsyncBtService(repository, NullLogger.Instance);
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
        var repository = new MockAsyncBtRepository();
        var instance = new StubAsyncBtService(repository, NullLogger.Instance);
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

    /// <summary>
    /// Unit test to verify that TruncateAsync delegates to the repository.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task TruncateAsync_Called_DelegatesToRepository()
    {
        // Arrange (Given)
        var repository = new MockAsyncBtRepository();
        var instance = new StubAsyncBtService(repository, NullLogger.Instance);

        // Act (When)
        await instance.TruncateAsync();

        // Assert (Then)
        Assert.AreEqual(
            1,
            repository.TruncateCallCount,
            "TruncateAsync should delegate to the repository exactly once.");
    }

    /// <summary>
    /// Unit test to verify that TruncateAsync passes CancellationToken to the repository.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task TruncateAsync_WithCancellationToken_PassesTokenToRepository()
    {
        // Arrange (Given)
        using var cts = new CancellationTokenSource();
        CancellationToken expectedToken = cts.Token;

        var repository = new MockAsyncBtRepository();
        var instance = new StubAsyncBtService(repository, NullLogger.Instance);

        // Act (When)
        await instance.TruncateAsync(expectedToken);

        // Assert (Then)
        Assert.AreEqual(
            expectedToken,
            repository.LastTruncateCancellationToken,
            "TruncateAsync should pass the CancellationToken to the repository.");
    }

    #endregion Public Methods

    #region Private Classes

    /// <summary>
    /// Concrete subclass exposing the protected ctor and Repository property of
    /// <see cref="BaseAsyncBtService{TEntity, TId}"/>.
    /// </summary>
    private sealed class StubAsyncBtService
        : BaseAsyncBtService<TestEntityRecord, long>
    {
        public StubAsyncBtService(
            IAsyncBtRepository<TestEntityRecord, long> repository,
            ILogger logger)
            : base(repository, logger)
        {
        }

        public IAsyncBtRepository<TestEntityRecord, long> ExposedRepository
            => this.Repository;
    }

    /// <summary>
    /// In-line mock implementing the async Bt repository for service tests.
    /// </summary>
    private sealed class MockAsyncBtRepository
        : IAsyncBtRepository<TestEntityRecord, long>
    {
        public long BulkInsertResult { get; set; }

        public int BulkInsertCallCount { get; private set; }

        public int TruncateCallCount { get; private set; }

        public string? LastBulkInsertActivityId { get; private set; }

        public IList<TestEntityRecord>? LastBulkInsertRows { get; private set; }

        public CancellationToken LastTruncateCancellationToken { get; private set; }

        public Task<long> BulkInsertAsync(
            string activityId,
            IList<TestEntityRecord> rows,
            CancellationToken cancellationToken = default)
        {
            this.BulkInsertCallCount++;
            this.LastBulkInsertActivityId = activityId;
            this.LastBulkInsertRows = rows;
            return Task.FromResult(this.BulkInsertResult);
        }

        public Task TruncateAsync(CancellationToken cancellationToken = default)
        {
            this.TruncateCallCount++;
            this.LastTruncateCancellationToken = cancellationToken;
            return Task.CompletedTask;
        }
    }

    #endregion Private Classes
}
