namespace Roadbed.Test.Unit.Crud;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Crud;
using Roadbed.Crud.Repositories.Async;

/// <summary>
/// Contains unit tests for verifying the behavior of the
/// <see cref="BaseAsyncCrudaRepository{TEntity, TId}"/> abstract class.
/// </summary>
[TestClass]
public class BaseAsyncCrudaRepositoryTests
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
        var instance = new StubAsyncCrudaRepository();

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
        var instance = new StubAsyncCrudaRepository(logger);

        // Assert (Then)
        Assert.IsNotNull(
            instance,
            "Instance should be created successfully with the logger constructor.");
    }

    /// <summary>
    /// Unit test to verify that instance implements IAsyncCrudaRepository interface.
    /// </summary>
    [TestMethod]
    public void Instance_TypeCheck_ImplementsIAsyncCrudaRepository()
    {
        // Arrange (Given)

        // Act (When)
        var instance = new StubAsyncCrudaRepository();

        // Assert (Then)
        Assert.IsInstanceOfType<IAsyncCrudaRepository<TestEntityRecord, long>>(
            instance,
            "Instance should implement IAsyncCrudaRepository<TestEntityRecord, long>.");
    }

    /// <summary>
    /// Unit test to verify that instance implements IAsyncCrudRepository interface.
    /// </summary>
    [TestMethod]
    public void Instance_TypeCheck_ImplementsIAsyncCrudRepository()
    {
        // Arrange (Given)

        // Act (When)
        var instance = new StubAsyncCrudaRepository();

        // Assert (Then)
        Assert.IsInstanceOfType<IAsyncCrudRepository<TestEntityRecord, long>>(
            instance,
            "Instance should implement IAsyncCrudRepository<TestEntityRecord, long>.");
    }

    /// <summary>
    /// Unit test to verify that instance implements IRepository marker interface.
    /// </summary>
    [TestMethod]
    public void Instance_TypeCheck_ImplementsIRepository()
    {
        // Arrange (Given)

        // Act (When)
        var instance = new StubAsyncCrudaRepository();

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
        var instance = new StubAsyncCrudaRepository();

        // Assert (Then)
        Assert.IsInstanceOfType<BaseClassWithLogging>(
            instance,
            "Instance should inherit from BaseClassWithLogging.");
    }

    /// <summary>
    /// Unit test to verify that CreateAsync can be called and returns expected entity.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task CreateAsync_ValidEntity_ReturnsCreatedEntity()
    {
        // Arrange (Given)
        var entity = new TestEntityRecord { Id = 1, Name = "Test" };
        var instance = new StubAsyncCrudaRepository
        {
            EntityToReturn = entity,
        };

        // Act (When)
        TestEntityRecord result = await instance.CreateAsync(entity);

        // Assert (Then)
        Assert.AreSame(
            entity,
            result,
            "CreateAsync should return the entity provided by the concrete implementation.");
    }

    /// <summary>
    /// Unit test to verify that CreateAsync passes CancellationToken to the implementation.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task CreateAsync_WithCancellationToken_PassesTokenToImplementation()
    {
        // Arrange (Given)
        using var cts = new CancellationTokenSource();
        CancellationToken expectedToken = cts.Token;
        var entity = new TestEntityRecord { Id = 1, Name = "Test" };
        var instance = new StubAsyncCrudaRepository();

        // Act (When)
        await instance.CreateAsync(entity, expectedToken);

        // Assert (Then)
        Assert.AreEqual(
            expectedToken,
            instance.LastCancellationToken,
            "CreateAsync should pass the CancellationToken to the concrete implementation.");
    }

    /// <summary>
    /// Unit test to verify that ReadAsync can be called and returns expected entity.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task ReadAsync_ExistingId_ReturnsEntity()
    {
        // Arrange (Given)
        var expectedEntity = new TestEntityRecord { Id = 1, Name = "Test" };
        var instance = new StubAsyncCrudaRepository
        {
            EntityToReturn = expectedEntity,
        };

        // Act (When)
        TestEntityRecord? result = await instance.ReadAsync(1);

        // Assert (Then)
        Assert.AreSame(
            expectedEntity,
            result,
            "ReadAsync should return the entity provided by the concrete implementation.");
    }

    /// <summary>
    /// Unit test to verify that ReadAsync returns null when entity is not found.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task ReadAsync_NonExistingId_ReturnsNull()
    {
        // Arrange (Given)
        var instance = new StubAsyncCrudaRepository
        {
            EntityToReturn = null,
        };

        // Act (When)
        TestEntityRecord? result = await instance.ReadAsync(999);

        // Assert (Then)
        Assert.IsNull(
            result,
            "ReadAsync should return null when the entity is not found.");
    }

    /// <summary>
    /// Unit test to verify that ReadAsync passes CancellationToken to the implementation.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task ReadAsync_WithCancellationToken_PassesTokenToImplementation()
    {
        // Arrange (Given)
        using var cts = new CancellationTokenSource();
        CancellationToken expectedToken = cts.Token;
        var instance = new StubAsyncCrudaRepository();

        // Act (When)
        await instance.ReadAsync(1, expectedToken);

        // Assert (Then)
        Assert.AreEqual(
            expectedToken,
            instance.LastCancellationToken,
            "ReadAsync should pass the CancellationToken to the concrete implementation.");
    }

    /// <summary>
    /// Unit test to verify that ReadAsync captures the identifier passed by the caller.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task ReadAsync_ValidId_CapturesIdentifier()
    {
        // Arrange (Given)
        long expectedId = 42;
        var instance = new StubAsyncCrudaRepository();

        // Act (When)
        await instance.ReadAsync(expectedId);

        // Assert (Then)
        Assert.AreEqual(
            expectedId,
            instance.LastReadId,
            "ReadAsync should receive the identifier passed by the caller.");
    }

    /// <summary>
    /// Unit test to verify that UpdateAsync can be called and returns expected entity.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task UpdateAsync_ValidEntity_ReturnsUpdatedEntity()
    {
        // Arrange (Given)
        var entity = new TestEntityRecord { Id = 1, Name = "Updated" };
        var instance = new StubAsyncCrudaRepository
        {
            EntityToReturn = entity,
        };

        // Act (When)
        TestEntityRecord result = await instance.UpdateAsync(entity);

        // Assert (Then)
        Assert.AreSame(
            entity,
            result,
            "UpdateAsync should return the entity provided by the concrete implementation.");
    }

    /// <summary>
    /// Unit test to verify that UpdateAsync passes CancellationToken to the implementation.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task UpdateAsync_WithCancellationToken_PassesTokenToImplementation()
    {
        // Arrange (Given)
        using var cts = new CancellationTokenSource();
        CancellationToken expectedToken = cts.Token;
        var entity = new TestEntityRecord { Id = 1, Name = "Test" };
        var instance = new StubAsyncCrudaRepository();

        // Act (When)
        await instance.UpdateAsync(entity, expectedToken);

        // Assert (Then)
        Assert.AreEqual(
            expectedToken,
            instance.LastCancellationToken,
            "UpdateAsync should pass the CancellationToken to the concrete implementation.");
    }

    /// <summary>
    /// Unit test to verify that DeleteAsync can be called successfully.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task DeleteAsync_ValidId_CompletesSuccessfully()
    {
        // Arrange (Given)
        long idToDelete = 1;
        var instance = new StubAsyncCrudaRepository();

        // Act (When)
        await instance.DeleteAsync(idToDelete);

        // Assert (Then)
        Assert.AreEqual(
            idToDelete,
            instance.LastDeleteId,
            "DeleteAsync should receive the identifier passed by the caller.");
    }

    /// <summary>
    /// Unit test to verify that DeleteAsync passes CancellationToken to the implementation.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task DeleteAsync_WithCancellationToken_PassesTokenToImplementation()
    {
        // Arrange (Given)
        using var cts = new CancellationTokenSource();
        CancellationToken expectedToken = cts.Token;
        var instance = new StubAsyncCrudaRepository();

        // Act (When)
        await instance.DeleteAsync(1, expectedToken);

        // Assert (Then)
        Assert.AreEqual(
            expectedToken,
            instance.LastCancellationToken,
            "DeleteAsync should pass the CancellationToken to the concrete implementation.");
    }

    /// <summary>
    /// Unit test to verify that ArchiveAsync can be called and returns expected entity.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task ArchiveAsync_ValidId_ReturnsArchivedEntity()
    {
        // Arrange (Given)
        var expectedEntity = new TestEntityRecord { Id = 1, Name = "Archived" };
        var instance = new StubAsyncCrudaRepository
        {
            EntityToReturn = expectedEntity,
        };

        // Act (When)
        TestEntityRecord result = await instance.ArchiveAsync(1);

        // Assert (Then)
        Assert.AreSame(
            expectedEntity,
            result,
            "ArchiveAsync should return the entity provided by the concrete implementation.");
    }

    /// <summary>
    /// Unit test to verify that ArchiveAsync passes CancellationToken to the implementation.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task ArchiveAsync_WithCancellationToken_PassesTokenToImplementation()
    {
        // Arrange (Given)
        using var cts = new CancellationTokenSource();
        CancellationToken expectedToken = cts.Token;
        var instance = new StubAsyncCrudaRepository();

        // Act (When)
        await instance.ArchiveAsync(1, expectedToken);

        // Assert (Then)
        Assert.AreEqual(
            expectedToken,
            instance.LastCancellationToken,
            "ArchiveAsync should pass the CancellationToken to the concrete implementation.");
    }

    /// <summary>
    /// Unit test to verify that ArchiveAsync captures the identifier passed by the caller.
    /// </summary>
    /// <returns>Task representing the completed operation.</returns>
    [TestMethod]
    public async Task ArchiveAsync_ValidId_CapturesIdentifier()
    {
        // Arrange (Given)
        long expectedId = 42;
        var instance = new StubAsyncCrudaRepository();

        // Act (When)
        await instance.ArchiveAsync(expectedId);

        // Assert (Then)
        Assert.AreEqual(
            expectedId,
            instance.LastArchiveId,
            "ArchiveAsync should receive the identifier passed by the caller.");
    }

    #endregion Public Methods

    #region Private Classes

    /// <summary>
    /// Concrete stub for testing the abstract BaseAsyncCrudaRepository.
    /// </summary>
    private sealed class StubAsyncCrudaRepository
        : BaseAsyncCrudaRepository<TestEntityRecord, long>
    {
        public TestEntityRecord? EntityToReturn { get; set; }

        public CancellationToken LastCancellationToken { get; private set; }

        public long? LastReadId { get; private set; }

        public long? LastDeleteId { get; private set; }

        public long? LastArchiveId { get; private set; }

        public StubAsyncCrudaRepository()
            : base()
        {
        }

        public StubAsyncCrudaRepository(ILogger logger)
            : base(logger)
        {
        }

        public override Task<TestEntityRecord> CreateAsync(
            TestEntityRecord entity,
            CancellationToken cancellationToken = default)
        {
            this.LastCancellationToken = cancellationToken;
            return Task.FromResult(this.EntityToReturn ?? entity);
        }

        public override Task<TestEntityRecord?> ReadAsync(
            long id,
            CancellationToken cancellationToken = default)
        {
            this.LastReadId = id;
            this.LastCancellationToken = cancellationToken;
            return Task.FromResult(this.EntityToReturn);
        }

        public override Task<TestEntityRecord> UpdateAsync(
            TestEntityRecord entity,
            CancellationToken cancellationToken = default)
        {
            this.LastCancellationToken = cancellationToken;
            return Task.FromResult(this.EntityToReturn ?? entity);
        }

        public override Task DeleteAsync(
            long id,
            CancellationToken cancellationToken = default)
        {
            this.LastDeleteId = id;
            this.LastCancellationToken = cancellationToken;
            return Task.CompletedTask;
        }

        public override Task<TestEntityRecord> ArchiveAsync(
            long id,
            CancellationToken cancellationToken = default)
        {
            this.LastArchiveId = id;
            this.LastCancellationToken = cancellationToken;
            return Task.FromResult(
                this.EntityToReturn ?? new TestEntityRecord { Id = id });
        }
    }

    #endregion Private Classes
}