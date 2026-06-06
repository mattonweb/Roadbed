namespace Roadbed.Test.Unit.Crud;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Crud;
using Roadbed.Crud.Operations.Async;
using Roadbed.Crud.Repositories.Async;

/// <summary>
/// Contains unit tests for verifying the behavior of the
/// <see cref="BaseAsyncBulkOnlyRepository{TEntity, TId}"/> abstract class.
/// </summary>
[TestClass]
public class BaseAsyncBulkOnlyRepositoryTests
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
        var instance = new StubAsyncBulkOnlyRepository();

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
        var instance = new StubAsyncBulkOnlyRepository(logger);

        // Assert (Then)
        Assert.IsNotNull(
            instance,
            "Instance should be created successfully with the logger constructor.");
    }

    /// <summary>
    /// Unit test to verify that instance implements IAsyncBulkOnlyRepository interface.
    /// </summary>
    [TestMethod]
    public void Instance_TypeCheck_ImplementsIAsyncBulkOnlyRepository()
    {
        // Arrange (Given)

        // Act (When)
        var instance = new StubAsyncBulkOnlyRepository();

        // Assert (Then)
        Assert.IsInstanceOfType<IAsyncBulkOnlyRepository<TestEntityRecord, long>>(
            instance,
            "Instance should implement IAsyncBulkOnlyRepository<TestEntityRecord, long>.");
    }

    /// <summary>
    /// Unit test to verify that instance implements IAsyncBulkInsertOperation operation.
    /// </summary>
    [TestMethod]
    public void Instance_TypeCheck_ImplementsIAsyncBulkInsertOperation()
    {
        // Arrange (Given)

        // Act (When)
        var instance = new StubAsyncBulkOnlyRepository();

        // Assert (Then)
        Assert.IsInstanceOfType<IAsyncBulkInsertOperation<TestEntityRecord, long>>(
            instance,
            "Instance should implement IAsyncBulkInsertOperation<TestEntityRecord, long>.");
    }

    /// <summary>
    /// Unit test to verify that instance implements IRepository marker interface.
    /// </summary>
    [TestMethod]
    public void Instance_TypeCheck_ImplementsIRepository()
    {
        // Arrange (Given)

        // Act (When)
        var instance = new StubAsyncBulkOnlyRepository();

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
        var instance = new StubAsyncBulkOnlyRepository();

        // Assert (Then)
        Assert.IsInstanceOfType<BaseClassWithLogging>(
            instance,
            "Instance should inherit from BaseClassWithLogging.");
    }

    /// <summary>
    /// Unit test to verify that BulkInsertAsync can be called and returns expected results.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task BulkInsertAsync_Called_ReturnsExpectedRowCount()
    {
        // Arrange (Given)
        var rows = new List<TestEntityRecord>
        {
            new TestEntityRecord { Id = 1, Name = "First" },
            new TestEntityRecord { Id = 2, Name = "Second" },
        };

        var instance = new StubAsyncBulkOnlyRepository
        {
            RowsToReturn = rows.Count,
        };

        // Act (When)
        long result = await instance.BulkInsertAsync("activity-1", rows);

        // Assert (Then)
        Assert.AreEqual(
            (long)rows.Count,
            result,
            "BulkInsertAsync should return the row count supplied by the concrete implementation.");
    }

    /// <summary>
    /// Unit test to verify that BulkInsertAsync passes the activity id to the implementation.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task BulkInsertAsync_Called_PassesActivityIdToImplementation()
    {
        // Arrange (Given)
        const string expectedActivityId = "activity-42";
        var instance = new StubAsyncBulkOnlyRepository();

        // Act (When)
        await instance.BulkInsertAsync(expectedActivityId, new List<TestEntityRecord>());

        // Assert (Then)
        Assert.AreEqual(
            expectedActivityId,
            instance.LastActivityId,
            "BulkInsertAsync should pass the activity id to the concrete implementation.");
    }

    /// <summary>
    /// Unit test to verify that BulkInsertAsync passes the rows collection to the implementation.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task BulkInsertAsync_Called_PassesRowsToImplementation()
    {
        // Arrange (Given)
        var expectedRows = new List<TestEntityRecord>
        {
            new TestEntityRecord { Id = 1, Name = "Row 1" },
        };
        var instance = new StubAsyncBulkOnlyRepository();

        // Act (When)
        await instance.BulkInsertAsync("activity-1", expectedRows);

        // Assert (Then)
        Assert.AreSame(
            expectedRows,
            instance.LastRows,
            "BulkInsertAsync should pass the rows collection to the concrete implementation.");
    }

    /// <summary>
    /// Unit test to verify that BulkInsertAsync passes CancellationToken to the implementation.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task BulkInsertAsync_WithCancellationToken_PassesTokenToImplementation()
    {
        // Arrange (Given)
        using var cts = new CancellationTokenSource();
        CancellationToken expectedToken = cts.Token;

        var instance = new StubAsyncBulkOnlyRepository();

        // Act (When)
        await instance.BulkInsertAsync("activity-1", new List<TestEntityRecord>(), expectedToken);

        // Assert (Then)
        Assert.AreEqual(
            expectedToken,
            instance.LastCancellationToken,
            "BulkInsertAsync should pass the CancellationToken to the concrete implementation.");
    }

    /// <summary>
    /// Unit test to verify that BulkInsertAsync uses default CancellationToken when none is provided.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task BulkInsertAsync_NoCancellationToken_UsesDefaultToken()
    {
        // Arrange (Given)
        var instance = new StubAsyncBulkOnlyRepository();

        // Act (When)
        await instance.BulkInsertAsync("activity-1", new List<TestEntityRecord>());

        // Assert (Then)
        Assert.AreEqual(
            default(CancellationToken),
            instance.LastCancellationToken,
            "BulkInsertAsync should use default CancellationToken when none is provided.");
    }

    #endregion Public Methods

    #region Private Classes

    /// <summary>
    /// Concrete stub for testing the abstract BaseAsyncBulkOnlyRepository.
    /// </summary>
    private sealed class StubAsyncBulkOnlyRepository
        : BaseAsyncBulkOnlyRepository<TestEntityRecord, long>
    {
        public long RowsToReturn { get; set; }

        public string? LastActivityId { get; private set; }

        public IList<TestEntityRecord>? LastRows { get; private set; }

        public CancellationToken LastCancellationToken { get; private set; }

        public StubAsyncBulkOnlyRepository()
            : base()
        {
        }

        public StubAsyncBulkOnlyRepository(ILogger logger)
            : base(logger)
        {
        }

        public override Task<long> BulkInsertAsync(
            string activityId,
            IList<TestEntityRecord> rows,
            CancellationToken cancellationToken = default)
        {
            this.LastActivityId = activityId;
            this.LastRows = rows;
            this.LastCancellationToken = cancellationToken;
            return Task.FromResult(this.RowsToReturn);
        }
    }

    #endregion Private Classes
}
