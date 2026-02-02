namespace Roadbed.Test.Unit.Crud;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Crud;
using Roadbed.Crud.Repositories.Async;
using Roadbed.Crud.Services.Async;

/// <summary>
/// Contains unit tests for verifying the behavior of the
/// <see cref="BaseAsyncCrudService{TEntity, TId}"/> class.
/// </summary>
[TestClass]
public class BaseAsyncCrudServiceTests
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
        var instance = new StubAsyncCrudService(repository, logger);

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
        IAsyncCrudRepository<TestEntityRecord, long>? nullRepository = null;
        ILogger logger = NullLogger.Instance;
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            new StubAsyncCrudService(nullRepository!, logger);
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
    /// Unit test to verify that instance implements IAsyncCrudService interface.
    /// </summary>
    [TestMethod]
    public void Instance_TypeCheck_ImplementsIAsyncCrudService()
    {
        // Arrange (Given)
        var repository = new MockAsyncRepository();
        ILogger logger = NullLogger.Instance;

        // Act (When)
        var instance = new StubAsyncCrudService(repository, logger);

        // Assert (Then)
        Assert.IsInstanceOfType<IAsyncCrudService<TestEntityRecord, long>>(
            instance,
            "Instance should implement IAsyncCrudService<TestEntityRecord, long>.");
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
        var instance = new StubAsyncCrudService(repository, logger);

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
        var instance = new StubAsyncCrudService(repository, logger);

        // Act (When)
        IAsyncCrudRepository<TestEntityRecord, long> result =
            instance.ExposedRepository;

        // Assert (Then)
        Assert.AreSame(
            repository,
            result,
            "Repository property should return the repository passed to the constructor.");
    }

    /// <summary>
    /// Unit test to verify that CreateAsync delegates to the repository.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task CreateAsync_ValidEntity_DelegatesToRepository()
    {
        // Arrange (Given)
        var entity = new TestEntityRecord { Id = 1, Name = "Test" };
        var repository = new MockAsyncRepository();
        repository.OnCreateAsync = (e, ct) => Task.FromResult(e);

        ILogger logger = NullLogger.Instance;
        var instance = new StubAsyncCrudService(repository, logger);

        // Act (When)
        TestEntityRecord result = await instance.CreateAsync(entity);

        // Assert (Then)
        Assert.AreSame(
            entity,
            result,
            "CreateAsync should return the entity from the repository.");
        Assert.AreEqual(
            1,
            repository.CreateAsyncCallCount,
            "CreateAsync should delegate to the repository exactly once.");
    }

    /// <summary>
    /// Unit test to verify that CreateAsync passes CancellationToken to the repository.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task CreateAsync_WithCancellationToken_PassesTokenToRepository()
    {
        // Arrange (Given)
        using var cts = new CancellationTokenSource();
        CancellationToken expectedToken = cts.Token;
        CancellationToken capturedToken = default;

        var entity = new TestEntityRecord { Id = 1, Name = "Test" };
        var repository = new MockAsyncRepository();
        repository.OnCreateAsync = (e, ct) =>
        {
            capturedToken = ct;
            return Task.FromResult(e);
        };

        ILogger logger = NullLogger.Instance;
        var instance = new StubAsyncCrudService(repository, logger);

        // Act (When)
        await instance.CreateAsync(entity, expectedToken);

        // Assert (Then)
        Assert.AreEqual(
            expectedToken,
            capturedToken,
            "CreateAsync should pass the CancellationToken to the repository.");
    }

    /// <summary>
    /// Unit test to verify that ReadAsync delegates to the repository.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task ReadAsync_ExistingId_DelegatesToRepository()
    {
        // Arrange (Given)
        var expectedEntity = new TestEntityRecord { Id = 1, Name = "Test" };
        var repository = new MockAsyncRepository();
        repository.OnReadAsync = (id, ct) => Task.FromResult<TestEntityRecord?>(expectedEntity);

        ILogger logger = NullLogger.Instance;
        var instance = new StubAsyncCrudService(repository, logger);

        // Act (When)
        TestEntityRecord? result = await instance.ReadAsync(1);

        // Assert (Then)
        Assert.AreSame(
            expectedEntity,
            result,
            "ReadAsync should return the entity from the repository.");
        Assert.AreEqual(
            1,
            repository.ReadAsyncCallCount,
            "ReadAsync should delegate to the repository exactly once.");
    }

    /// <summary>
    /// Unit test to verify that ReadAsync returns null when entity is not found.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task ReadAsync_NonExistingId_ReturnsNull()
    {
        // Arrange (Given)
        var repository = new MockAsyncRepository();
        ILogger logger = NullLogger.Instance;
        var instance = new StubAsyncCrudService(repository, logger);

        // Act (When)
        TestEntityRecord? result = await instance.ReadAsync(999);

        // Assert (Then)
        Assert.IsNull(
            result,
            "ReadAsync should return null when the entity is not found.");
    }

    /// <summary>
    /// Unit test to verify that ReadAsync passes CancellationToken to the repository.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task ReadAsync_WithCancellationToken_PassesTokenToRepository()
    {
        // Arrange (Given)
        using var cts = new CancellationTokenSource();
        CancellationToken expectedToken = cts.Token;
        CancellationToken capturedToken = default;

        var repository = new MockAsyncRepository();
        repository.OnReadAsync = (id, ct) =>
        {
            capturedToken = ct;
            return Task.FromResult<TestEntityRecord?>(null);
        };

        ILogger logger = NullLogger.Instance;
        var instance = new StubAsyncCrudService(repository, logger);

        // Act (When)
        await instance.ReadAsync(1, expectedToken);

        // Assert (Then)
        Assert.AreEqual(
            expectedToken,
            capturedToken,
            "ReadAsync should pass the CancellationToken to the repository.");
    }

    /// <summary>
    /// Unit test to verify that UpdateAsync delegates to the repository.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task UpdateAsync_ValidEntity_DelegatesToRepository()
    {
        // Arrange (Given)
        var entity = new TestEntityRecord { Id = 1, Name = "Updated" };
        var repository = new MockAsyncRepository();
        repository.OnUpdateAsync = (e, ct) => Task.FromResult(e);

        ILogger logger = NullLogger.Instance;
        var instance = new StubAsyncCrudService(repository, logger);

        // Act (When)
        TestEntityRecord result = await instance.UpdateAsync(entity);

        // Assert (Then)
        Assert.AreSame(
            entity,
            result,
            "UpdateAsync should return the entity from the repository.");
        Assert.AreEqual(
            1,
            repository.UpdateAsyncCallCount,
            "UpdateAsync should delegate to the repository exactly once.");
    }

    /// <summary>
    /// Unit test to verify that UpdateAsync passes CancellationToken to the repository.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task UpdateAsync_WithCancellationToken_PassesTokenToRepository()
    {
        // Arrange (Given)
        using var cts = new CancellationTokenSource();
        CancellationToken expectedToken = cts.Token;
        CancellationToken capturedToken = default;

        var entity = new TestEntityRecord { Id = 1, Name = "Test" };
        var repository = new MockAsyncRepository();
        repository.OnUpdateAsync = (e, ct) =>
        {
            capturedToken = ct;
            return Task.FromResult(e);
        };

        ILogger logger = NullLogger.Instance;
        var instance = new StubAsyncCrudService(repository, logger);

        // Act (When)
        await instance.UpdateAsync(entity, expectedToken);

        // Assert (Then)
        Assert.AreEqual(
            expectedToken,
            capturedToken,
            "UpdateAsync should pass the CancellationToken to the repository.");
    }

    /// <summary>
    /// Unit test to verify that DeleteAsync delegates to the repository.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task DeleteAsync_ValidId_DelegatesToRepository()
    {
        // Arrange (Given)
        long idToDelete = 1;
        var repository = new MockAsyncRepository();
        ILogger logger = NullLogger.Instance;
        var instance = new StubAsyncCrudService(repository, logger);

        // Act (When)
        await instance.DeleteAsync(idToDelete);

        // Assert (Then)
        Assert.AreEqual(
            1,
            repository.DeleteAsyncCallCount,
            "DeleteAsync should delegate to the repository exactly once.");
        Assert.AreEqual(
            idToDelete,
            repository.LastDeleteId,
            "DeleteAsync should pass the correct identifier to the repository.");
    }

    /// <summary>
    /// Unit test to verify that DeleteAsync passes CancellationToken to the repository.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task DeleteAsync_WithCancellationToken_PassesTokenToRepository()
    {
        // Arrange (Given)
        using var cts = new CancellationTokenSource();
        CancellationToken expectedToken = cts.Token;
        CancellationToken capturedToken = default;

        var repository = new MockAsyncRepository();
        repository.OnDeleteAsync = (id, ct) =>
        {
            capturedToken = ct;
            return Task.CompletedTask;
        };

        ILogger logger = NullLogger.Instance;
        var instance = new StubAsyncCrudService(repository, logger);

        // Act (When)
        await instance.DeleteAsync(1, expectedToken);

        // Assert (Then)
        Assert.AreEqual(
            expectedToken,
            capturedToken,
            "DeleteAsync should pass the CancellationToken to the repository.");
    }

    /// <summary>
    /// Unit test to verify that ExistsAsync returns true when entity is found.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task ExistsAsync_EntityFound_ReturnsTrue()
    {
        // Arrange (Given)
        var existingEntity = new TestEntityRecord { Id = 1, Name = "Exists" };
        var repository = new MockAsyncRepository();
        repository.OnReadAsync = (id, ct) =>
            Task.FromResult<TestEntityRecord?>(existingEntity);

        ILogger logger = NullLogger.Instance;
        var instance = new StubAsyncCrudService(repository, logger);

        // Act (When)
        bool result = await instance.ExistsAsync(1);

        // Assert (Then)
        Assert.IsTrue(
            result,
            "ExistsAsync should return true when the repository returns an entity.");
    }

    /// <summary>
    /// Unit test to verify that ExistsAsync returns false when entity is not found.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task ExistsAsync_EntityNotFound_ReturnsFalse()
    {
        // Arrange (Given)
        var repository = new MockAsyncRepository();
        ILogger logger = NullLogger.Instance;
        var instance = new StubAsyncCrudService(repository, logger);

        // Act (When)
        bool result = await instance.ExistsAsync(999);

        // Assert (Then)
        Assert.IsFalse(
            result,
            "ExistsAsync should return false when the repository returns null.");
    }

    /// <summary>
    /// Unit test to verify that ExistsAsync passes CancellationToken to the repository.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task ExistsAsync_WithCancellationToken_PassesTokenToRepository()
    {
        // Arrange (Given)
        using var cts = new CancellationTokenSource();
        CancellationToken expectedToken = cts.Token;
        CancellationToken capturedToken = default;

        var repository = new MockAsyncRepository();
        repository.OnReadAsync = (id, ct) =>
        {
            capturedToken = ct;
            return Task.FromResult<TestEntityRecord?>(null);
        };

        ILogger logger = NullLogger.Instance;
        var instance = new StubAsyncCrudService(repository, logger);

        // Act (When)
        await instance.ExistsAsync(1, expectedToken);

        // Assert (Then)
        Assert.AreEqual(
            expectedToken,
            capturedToken,
            "ExistsAsync should pass the CancellationToken to the repository.");
    }

    /// <summary>
    /// Unit test to verify that UpsertAsync throws when entity is null.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task UpsertAsync_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange (Given)
        TestEntityRecord? nullEntity = null;
        var repository = new MockAsyncRepository();
        ILogger logger = NullLogger.Instance;
        var instance = new StubAsyncCrudService(repository, logger);
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            await instance.UpsertAsync(nullEntity!);
        }
        catch (ArgumentNullException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "UpsertAsync should throw ArgumentNullException when entity is null.");
    }

    /// <summary>
    /// Unit test to verify that UpsertAsync calls UpdateAsync when entity exists.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task UpsertAsync_ExistingEntity_CallsUpdateAsync()
    {
        // Arrange (Given)
        var entity = new TestEntityRecord { Id = 1, Name = "Existing" };
        var repository = new MockAsyncRepository();
        repository.OnReadAsync = (id, ct) =>
            Task.FromResult<TestEntityRecord?>(entity);
        repository.OnUpdateAsync = (e, ct) => Task.FromResult(e);

        ILogger logger = NullLogger.Instance;
        var instance = new StubAsyncCrudService(repository, logger);

        // Act (When)
        TestEntityRecord result = await instance.UpsertAsync(entity);

        // Assert (Then)
        Assert.AreEqual(
            1,
            repository.UpdateAsyncCallCount,
            "UpsertAsync should call UpdateAsync when the entity exists.");
        Assert.AreEqual(
            0,
            repository.CreateAsyncCallCount,
            "UpsertAsync should not call CreateAsync when the entity exists.");
        Assert.AreSame(
            entity,
            result,
            "UpsertAsync should return the entity from UpdateAsync.");
    }

    /// <summary>
    /// Unit test to verify that UpsertAsync calls CreateAsync when entity does not exist.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task UpsertAsync_NewEntity_CallsCreateAsync()
    {
        // Arrange (Given)
        var entity = new TestEntityRecord { Id = 99, Name = "New" };
        var repository = new MockAsyncRepository();
        repository.OnCreateAsync = (e, ct) => Task.FromResult(e);

        ILogger logger = NullLogger.Instance;
        var instance = new StubAsyncCrudService(repository, logger);

        // Act (When)
        TestEntityRecord result = await instance.UpsertAsync(entity);

        // Assert (Then)
        Assert.AreEqual(
            1,
            repository.CreateAsyncCallCount,
            "UpsertAsync should call CreateAsync when the entity does not exist.");
        Assert.AreEqual(
            0,
            repository.UpdateAsyncCallCount,
            "UpsertAsync should not call UpdateAsync when the entity does not exist.");
        Assert.AreSame(
            entity,
            result,
            "UpsertAsync should return the entity from CreateAsync.");
    }

    /// <summary>
    /// Unit test to verify that UpsertAsync passes CancellationToken through all operations.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task UpsertAsync_WithCancellationToken_PassesTokenToRepository()
    {
        // Arrange (Given)
        using var cts = new CancellationTokenSource();
        CancellationToken expectedToken = cts.Token;
        CancellationToken capturedReadToken = default;
        CancellationToken capturedCreateToken = default;

        var entity = new TestEntityRecord { Id = 99, Name = "New" };
        var repository = new MockAsyncRepository();
        repository.OnReadAsync = (id, ct) =>
        {
            capturedReadToken = ct;
            return Task.FromResult<TestEntityRecord?>(null);
        };
        repository.OnCreateAsync = (e, ct) =>
        {
            capturedCreateToken = ct;
            return Task.FromResult(e);
        };

        ILogger logger = NullLogger.Instance;
        var instance = new StubAsyncCrudService(repository, logger);

        // Act (When)
        await instance.UpsertAsync(entity, expectedToken);

        // Assert (Then)
        Assert.AreEqual(
            expectedToken,
            capturedReadToken,
            "UpsertAsync should pass the CancellationToken to ReadAsync via ExistsAsync.");
        Assert.AreEqual(
            expectedToken,
            capturedCreateToken,
            "UpsertAsync should pass the CancellationToken to CreateAsync.");
    }

    #endregion Public Methods

    #region Private Classes

    /// <summary>
    /// Concrete subclass for testing the BaseAsyncCrudService base class.
    /// Exposes the protected constructor and Repository property.
    /// </summary>
    private sealed class StubAsyncCrudService
        : BaseAsyncCrudService<TestEntityRecord, long>
    {
        public StubAsyncCrudService(
            IAsyncCrudRepository<TestEntityRecord, long> repository,
            ILogger logger)
            : base(repository, logger)
        {
        }

        public IAsyncCrudRepository<TestEntityRecord, long> ExposedRepository
            => this.Repository;
    }

    #endregion Private Classes
}