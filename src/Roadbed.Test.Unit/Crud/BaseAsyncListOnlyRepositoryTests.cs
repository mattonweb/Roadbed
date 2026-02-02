namespace Roadbed.Test.Unit.Crud;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Crud;
using Roadbed.Crud.Repositories.Async;

/// <summary>
/// Contains unit tests for verifying the behavior of the
/// <see cref="BaseAsyncListOnlyRepository{TEntity, TId}"/> abstract class.
/// </summary>
[TestClass]
public class BaseAsyncListOnlyRepositoryTests
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
        var instance = new StubAsyncListOnlyRepository();

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
        var instance = new StubAsyncListOnlyRepository(logger);

        // Assert (Then)
        Assert.IsNotNull(
            instance,
            "Instance should be created successfully with the logger constructor.");
    }

    /// <summary>
    /// Unit test to verify that instance implements IAsyncListOnlyRepository interface.
    /// </summary>
    [TestMethod]
    public void Instance_TypeCheck_ImplementsIAsyncListOnlyRepository()
    {
        // Arrange (Given)

        // Act (When)
        var instance = new StubAsyncListOnlyRepository();

        // Assert (Then)
        Assert.IsInstanceOfType<IAsyncListOnlyRepository<TestEntityRecord, long>>(
            instance,
            "Instance should implement IAsyncListOnlyRepository<TestEntityRecord, long>.");
    }

    /// <summary>
    /// Unit test to verify that instance implements IRepository marker interface.
    /// </summary>
    [TestMethod]
    public void Instance_TypeCheck_ImplementsIRepository()
    {
        // Arrange (Given)

        // Act (When)
        var instance = new StubAsyncListOnlyRepository();

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
        var instance = new StubAsyncListOnlyRepository();

        // Assert (Then)
        Assert.IsInstanceOfType<BaseClassWithLogging>(
            instance,
            "Instance should inherit from BaseClassWithLogging.");
    }

    /// <summary>
    /// Unit test to verify that ListAsync can be called and returns expected results.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task ListAsync_Called_ReturnsExpectedResults()
    {
        // Arrange (Given)
        var expectedEntities = new List<TestEntityRecord>
        {
            new TestEntityRecord { Id = 1, Name = "First" },
            new TestEntityRecord { Id = 2, Name = "Second" },
        };

        var instance = new StubAsyncListOnlyRepository
        {
            EntitiesToReturn = expectedEntities,
        };

        // Act (When)
        IList<TestEntityRecord> result = await instance.ListAsync();

        // Assert (Then)
        Assert.AreSame(
            expectedEntities,
            result,
            "ListAsync should return the collection provided by the concrete implementation.");
    }

    /// <summary>
    /// Unit test to verify that ListAsync passes CancellationToken to the implementation.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task ListAsync_WithCancellationToken_PassesTokenToImplementation()
    {
        // Arrange (Given)
        using var cts = new CancellationTokenSource();
        CancellationToken expectedToken = cts.Token;

        var instance = new StubAsyncListOnlyRepository();

        // Act (When)
        await instance.ListAsync(expectedToken);

        // Assert (Then)
        Assert.AreEqual(
            expectedToken,
            instance.LastCancellationToken,
            "ListAsync should pass the CancellationToken to the concrete implementation.");
    }

    /// <summary>
    /// Unit test to verify that ListAsync uses default CancellationToken when none is provided.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task ListAsync_NoCancellationToken_UsesDefaultToken()
    {
        // Arrange (Given)
        var instance = new StubAsyncListOnlyRepository();

        // Act (When)
        await instance.ListAsync();

        // Assert (Then)
        Assert.AreEqual(
            default(CancellationToken),
            instance.LastCancellationToken,
            "ListAsync should use default CancellationToken when none is provided.");
    }

    /// <summary>
    /// Unit test to verify that ListAsync returns empty collection when no entities exist.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task ListAsync_NoEntities_ReturnsEmptyCollection()
    {
        // Arrange (Given)
        var instance = new StubAsyncListOnlyRepository
        {
            EntitiesToReturn = new List<TestEntityRecord>(),
        };

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
    /// Concrete stub for testing the abstract BaseAsyncListOnlyRepository.
    /// </summary>
    private sealed class StubAsyncListOnlyRepository
        : BaseAsyncListOnlyRepository<TestEntityRecord, long>
    {
        public IList<TestEntityRecord> EntitiesToReturn { get; set; }
            = new List<TestEntityRecord>();

        public CancellationToken LastCancellationToken { get; private set; }

        public StubAsyncListOnlyRepository()
            : base()
        {
        }

        public StubAsyncListOnlyRepository(ILogger logger)
            : base(logger)
        {
        }

        public override Task<IList<TestEntityRecord>> ListAsync(
            CancellationToken cancellationToken = default)
        {
            this.LastCancellationToken = cancellationToken;
            return Task.FromResult(this.EntitiesToReturn);
        }
    }

    #endregion Private Classes
}