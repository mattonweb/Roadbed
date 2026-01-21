namespace Roadbed.Test.Unit.Crud;

using Roadbed.Crud;
using Roadbed.Test.Unit.Crud.Mocks;

/// <summary>
/// Contains unit tests for verifying the behavior of the BaseEntityWithCrud class.
/// </summary>
[TestClass]
public class BaseEntityWithCrudTests
{
    #region Public Methods

    /// <summary>
    /// Verifies that constructor with ILogger initializes successfully.
    /// </summary>
    [TestMethod]
    public void Constructor_WithLogger_InitializesSuccessfully()
    {
        // Arrange
        var repository = new MockCrudRepository();
        var logger = new MockLogger();

        // Act
        var entity = new MockCrudEntity(repository, logger);

        // Assert
        Assert.IsNotNull(entity, "Entity should be initialized.");
        Assert.IsNotNull(entity.Repository, "Repository should be set.");
    }

    /// <summary>
    /// Verifies that constructor with ILogger throws when repository is null.
    /// </summary>
    [TestMethod]
    public void Constructor_WithLoggerAndNullRepository_ThrowsArgumentNullException()
    {
        // Arrange
        IBaseRepositoryWithCrud<MockDto, int>? nullRepository = null;
        var logger = new MockLogger();

        // Act
        bool threwException = false;
        try
        {
            var entity = new MockCrudEntity(nullRepository!, logger);
        }
        catch (ArgumentNullException)
        {
            threwException = true;
        }

        // Assert
        Assert.IsTrue(threwException, "Should throw ArgumentNullException when repository is null.");
    }

    /// <summary>
    /// Verifies that constructor with ILoggerFactory initializes successfully.
    /// </summary>
    [TestMethod]
    public void Constructor_WithLoggerFactory_InitializesSuccessfully()
    {
        // Arrange
        var repository = new MockCrudRepository();
        var loggerFactory = new MockLoggerFactory();

        // Act
        var entity = new MockCrudEntity(repository, loggerFactory);

        // Assert
        Assert.IsNotNull(entity, "Entity should be initialized.");
        Assert.IsNotNull(entity.Repository, "Repository should be set.");
    }

    /// <summary>
    /// Verifies that constructor with ILoggerFactory throws when repository is null.
    /// </summary>
    [TestMethod]
    public void Constructor_WithLoggerFactoryAndNullRepository_ThrowsArgumentNullException()
    {
        // Arrange
        IBaseRepositoryWithCrud<MockDto, int>? nullRepository = null;
        var loggerFactory = new MockLoggerFactory();

        // Act
        bool threwException = false;
        try
        {
            var entity = new MockCrudEntity(nullRepository!, loggerFactory);
        }
        catch (ArgumentNullException)
        {
            threwException = true;
        }

        // Assert
        Assert.IsTrue(threwException, "Should throw ArgumentNullException when repository is null.");
    }

    /// <summary>
    /// Verifies that CreateAsync uses cancellation token correctly.
    /// </summary>
    /// <returns>A task representing the asynchronous unit test operation.</returns>
    [TestMethod]
    public async Task CreateAsync_WithCancellationToken_PassesTokenToRepository()
    {
        // Arrange
        var repository = new MockCrudRepository();
        var logger = new MockLogger();
        var entity = new MockCrudEntity(repository, logger);
        var dto = new MockDto { Id = 100, Name = "Test" };
        var cts = new CancellationTokenSource();

        // Act
        await entity.CreateAsync(dto, cts.Token);

        // Assert
        Assert.AreEqual(cts.Token, repository.LastCancellationToken, "CancellationToken should be passed to repository.");
    }

    /// <summary>
    /// Verifies that CreateAsync creates a DTO successfully with valid data.
    /// </summary>
    /// <returns>A task representing the asynchronous unit test operation.</returns>
    [TestMethod]
    public async Task CreateAsync_WithValidDto_CreatesSuccessfully()
    {
        // Arrange
        var repository = new MockCrudRepository();
        var logger = new MockLogger();
        var entity = new MockCrudEntity(repository, logger);
        var dto = new MockDto { Id = 100, Name = "Test" };

        // Act
        var result = await entity.CreateAsync(dto);

        // Assert
        Assert.AreEqual(100, result, "CreateAsync should return the created ID.");
        Assert.IsTrue(repository.CreateCalled, "Repository CreateAsync should be called.");
    }

    /// <summary>
    /// Verifies that DeleteAsync uses cancellation token correctly.
    /// </summary>
    /// <returns>A task representing the asynchronous unit test operation.</returns>
    [TestMethod]
    public async Task DeleteAsync_WithCancellationToken_PassesTokenToRepository()
    {
        // Arrange
        var repository = new MockCrudRepository();
        var logger = new MockLogger();
        var entity = new MockCrudEntity(repository, logger);
        int id = 100;
        var cts = new CancellationTokenSource();

        // Act
        await entity.DeleteAsync(id, cts.Token);

        // Assert
        Assert.AreEqual(cts.Token, repository.LastCancellationToken, "CancellationToken should be passed to repository.");
    }

    /// <summary>
    /// Verifies that DeleteAsync returns early when ID is default.
    /// </summary>
    /// <returns>A task representing the asynchronous unit test operation.</returns>
    [TestMethod]
    public async Task DeleteAsync_WithDefaultId_ReturnsEarly()
    {
        // Arrange
        var repository = new MockCrudRepository();
        var logger = new MockLogger();
        var entity = new MockCrudEntity(repository, logger);
        int defaultId = default(int);

        // Act
        await entity.DeleteAsync(defaultId);

        // Assert
        Assert.IsFalse(repository.DeleteCalled, "Repository DeleteAsync should not be called when ID is default.");
    }

    /// <summary>
    /// Verifies that DeleteAsync deletes successfully with valid ID.
    /// </summary>
    /// <returns>A task representing the asynchronous unit test operation.</returns>
    [TestMethod]
    public async Task DeleteAsync_WithValidId_DeletesSuccessfully()
    {
        // Arrange
        var repository = new MockCrudRepository();
        var logger = new MockLogger();
        var entity = new MockCrudEntity(repository, logger);
        int id = 100;

        // Act
        await entity.DeleteAsync(id);

        // Assert
        Assert.IsTrue(repository.DeleteCalled, "Repository DeleteAsync should be called.");
    }

    /// <summary>
    /// Verifies that ReadAsync uses cancellation token correctly.
    /// </summary>
    /// <returns>A task representing the asynchronous unit test operation.</returns>
    [TestMethod]
    public async Task ReadAsync_WithCancellationToken_PassesTokenToRepository()
    {
        // Arrange
        var repository = new MockCrudRepository();
        var logger = new MockLogger();
        var entity = new MockCrudEntity(repository, logger);
        int id = 100;
        var cts = new CancellationTokenSource();

        // Act
        await entity.ReadAsync(id, cts.Token);

        // Assert
        Assert.AreEqual(cts.Token, repository.LastCancellationToken, "CancellationToken should be passed to repository.");
    }

    /// <summary>
    /// Verifies that ReadAsync returns default when ID is default.
    /// </summary>
    /// <returns>A task representing the asynchronous unit test operation.</returns>
    [TestMethod]
    public async Task ReadAsync_WithDefaultId_ReturnsDefault()
    {
        // Arrange
        var repository = new MockCrudRepository();
        var logger = new MockLogger();
        var entity = new MockCrudEntity(repository, logger);
        int defaultId = default(int);

        // Act
        var result = await entity.ReadAsync(defaultId);

        // Assert
        Assert.IsNull(result, "ReadAsync should return default when ID is default.");
        Assert.IsFalse(repository.ReadCalled, "Repository ReadAsync should not be called when ID is default.");
    }

    /// <summary>
    /// Verifies that ReadAsync reads DTO successfully with valid ID.
    /// </summary>
    /// <returns>A task representing the asynchronous unit test operation.</returns>
    [TestMethod]
    public async Task ReadAsync_WithValidId_ReadsSuccessfully()
    {
        // Arrange
        var repository = new MockCrudRepository();
        var logger = new MockLogger();
        var entity = new MockCrudEntity(repository, logger);
        int id = 100;

        // Act
        var result = await entity.ReadAsync(id);

        // Assert
        Assert.IsNotNull(result, "ReadAsync should return a DTO.");
        Assert.IsTrue(repository.ReadCalled, "Repository ReadAsync should be called.");
    }

    /// <summary>
    /// Verifies that Repository property returns the injected repository.
    /// </summary>
    [TestMethod]
    public void RepositoryProperty_ReturnsInjectedRepository()
    {
        // Arrange
        var repository = new MockCrudRepository();
        var logger = new MockLogger();
        var entity = new MockCrudEntity(repository, logger);

        // Act
        var retrievedRepository = entity.Repository;

        // Assert
        Assert.AreSame(repository, retrievedRepository, "Repository property should return the injected repository.");
    }

    /// <summary>
    /// Verifies that UpdateAsync uses cancellation token correctly.
    /// </summary>
    /// <returns>A task representing the asynchronous unit test operation.</returns>
    [TestMethod]
    public async Task UpdateAsync_WithCancellationToken_PassesTokenToRepository()
    {
        // Arrange
        var repository = new MockCrudRepository();
        var logger = new MockLogger();
        var entity = new MockCrudEntity(repository, logger);
        var dto = new MockDto { Id = 100, Name = "Updated" };
        var cts = new CancellationTokenSource();

        // Act
        await entity.UpdateAsync(dto, cts.Token);

        // Assert
        Assert.AreEqual(cts.Token, repository.LastCancellationToken, "CancellationToken should be passed to repository.");
    }

    /// <summary>
    /// Verifies that UpdateAsync returns early when DTO is null.
    /// </summary>
    /// <returns>A task representing the asynchronous unit test operation.</returns>
    [TestMethod]
    public async Task UpdateAsync_WithNullDto_ReturnsEarly()
    {
        // Arrange
        var repository = new MockCrudRepository();
        var logger = new MockLogger();
        var entity = new MockCrudEntity(repository, logger);
        MockDto? nullDto = null;

        // Act
        await entity.UpdateAsync(nullDto!);

        // Assert
        Assert.IsFalse(repository.UpdateCalled, "Repository UpdateAsync should not be called when DTO is null.");
    }

    /// <summary>
    /// Verifies that UpdateAsync updates DTO successfully with valid data.
    /// </summary>
    /// <returns>A task representing the asynchronous unit test operation.</returns>
    [TestMethod]
    public async Task UpdateAsync_WithValidDto_UpdatesSuccessfully()
    {
        // Arrange
        var repository = new MockCrudRepository();
        var logger = new MockLogger();
        var entity = new MockCrudEntity(repository, logger);
        var dto = new MockDto { Id = 100, Name = "Updated" };

        // Act
        await entity.UpdateAsync(dto);

        // Assert
        Assert.IsTrue(repository.UpdateCalled, "Repository UpdateAsync should be called.");
    }

    #endregion Public Methods
}