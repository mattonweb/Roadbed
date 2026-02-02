namespace Roadbed.Test.Unit.Crud;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Crud;
using Roadbed.Crud.Repositories.Async;
using Roadbed.Crud.Services.Async;

/// <summary>
/// Contains unit tests for verifying the behavior of the
/// <see cref="BaseAsyncListOnlyService{TEntity, TId}"/> class.
/// </summary>
[TestClass]
public class BaseAsyncListOnlyServiceTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that constructor creates an instance with valid parameters.
    /// </summary>
    [TestMethod]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        // Arrange (Given)
        var repository = new MockAsyncRepository();
        ILogger logger = NullLogger.Instance;

        // Act (When)
        var instance = new StubAsyncListOnlyService(repository, logger);

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
        IAsyncListOnlyRepository<TestEntityRecord, long>? nullRepository = null;
        ILogger logger = NullLogger.Instance;
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            new StubAsyncListOnlyService(nullRepository!, logger);
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
    /// Unit test to verify that instance implements IAsyncListOnlyService interface.
    /// </summary>
    [TestMethod]
    public void Instance_TypeCheck_ImplementsIAsyncListOnlyService()
    {
        // Arrange (Given)
        var repository = new MockAsyncRepository();
        ILogger logger = NullLogger.Instance;

        // Act (When)
        var instance = new StubAsyncListOnlyService(repository, logger);

        // Assert (Then)
        Assert.IsInstanceOfType<IAsyncListOnlyService<TestEntityRecord, long>>(
            instance,
            "Instance should implement IAsyncListOnlyService<TestEntityRecord, long>.");
    }

    /// <summary>
    /// Unit test to verify that instance inherits from BaseClassWithLogging.
    /// </summary>
    [TestMethod]
    public void Instance_TypeCheck_InheritsBaseClassWithLogging()
    {
        // Arrange (Given)
        var repository = new MockAsyncRepository();
        ILogger logger = NullLogger.Instance;

        // Act (When)
        var instance = new StubAsyncListOnlyService(repository, logger);

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
        var repository = new MockAsyncRepository();
        ILogger logger = NullLogger.Instance;
        var instance = new StubAsyncListOnlyService(repository, logger);

        // Act (When)
        IAsyncListOnlyRepository<TestEntityRecord, long> result =
            instance.ExposedRepository;

        // Assert (Then)
        Assert.AreSame(
            repository,
            result,
            "Repository property should return the repository passed to the constructor.");
    }

    /// <summary>
    /// Unit test to verify that ListAsync delegates to the repository.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task ListAsync_Called_DelegatesToRepository()
    {
        // Arrange (Given)
        var expectedEntities = new List<TestEntityRecord>
        {
            new TestEntityRecord { Id = 1, Name = "First" },
            new TestEntityRecord { Id = 2, Name = "Second" },
        };

        var repository = new MockAsyncRepository();
        repository.OnListAsync = (ct) =>
            Task.FromResult<IList<TestEntityRecord>>(expectedEntities);

        ILogger logger = NullLogger.Instance;
        var instance = new StubAsyncListOnlyService(repository, logger);

        // Act (When)
        IList<TestEntityRecord> result = await instance.ListAsync();

        // Assert (Then)
        Assert.AreSame(
            expectedEntities,
            result,
            "ListAsync should return the collection from the repository.");
        Assert.AreEqual(
            1,
            repository.ListAsyncCallCount,
            "ListAsync should delegate to the repository exactly once.");
    }

    /// <summary>
    /// Unit test to verify that ListAsync passes CancellationToken to the repository.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task ListAsync_WithCancellationToken_PassesTokenToRepository()
    {
        // Arrange (Given)
        using var cts = new CancellationTokenSource();
        CancellationToken expectedToken = cts.Token;
        CancellationToken capturedToken = default;

        var repository = new MockAsyncRepository();
        repository.OnListAsync = (ct) =>
        {
            capturedToken = ct;
            return Task.FromResult<IList<TestEntityRecord>>(
                new List<TestEntityRecord>());
        };

        ILogger logger = NullLogger.Instance;
        var instance = new StubAsyncListOnlyService(repository, logger);

        // Act (When)
        await instance.ListAsync(expectedToken);

        // Assert (Then)
        Assert.AreEqual(
            expectedToken,
            capturedToken,
            "ListAsync should pass the CancellationToken to the repository.");
    }

    /// <summary>
    /// Unit test to verify that ListAsync returns empty collection when no entities exist.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task ListAsync_NoEntities_ReturnsEmptyCollection()
    {
        // Arrange (Given)
        var repository = new MockAsyncRepository();
        ILogger logger = NullLogger.Instance;
        var instance = new StubAsyncListOnlyService(repository, logger);

        // Act (When)
        IList<TestEntityRecord> result = await instance.ListAsync();

        // Assert (Then)
        Assert.HasCount(
            0,
            result,
            "ListAsync should return an empty collection when no entities exist.");
    }

    #endregion Public Methods

    #region Private Classes

    /// <summary>
    /// Concrete subclass for testing the BaseAsyncListOnlyService base class.
    /// Exposes the protected constructor and Repository property.
    /// </summary>
    private sealed class StubAsyncListOnlyService
        : BaseAsyncListOnlyService<TestEntityRecord, long>
    {
        public StubAsyncListOnlyService(
            IAsyncListOnlyRepository<TestEntityRecord, long> repository,
            ILogger logger)
            : base(repository, logger)
        {
        }

        public IAsyncListOnlyRepository<TestEntityRecord, long> ExposedRepository
            => this.Repository;
    }

    #endregion Private Classes
}