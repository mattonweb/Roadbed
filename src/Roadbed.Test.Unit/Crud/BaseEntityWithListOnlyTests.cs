namespace Roadbed.Test.Unit.Crud;

using Roadbed.Crud;
using Roadbed.Test.Unit.Crud.Mocks;

/// <summary>
/// Contains unit tests for verifying the behavior of the BaseEntityWithListOnly class.
/// </summary>
[TestClass]
public class BaseEntityWithListOnlyTests
{
    #region Public Methods

    /// <summary>
    /// Verifies that constructor with ILogger initializes successfully.
    /// </summary>
    [TestMethod]
    public void Constructor_WithLogger_InitializesSuccessfully()
    {
        // Arrange
        var repository = new MockListOnlyRepository();
        var logger = new MockLogger();

        // Act
        var entity = new MockListOnlyEntity(repository, logger);

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
        IBaseRepositoryWithListOnly<MockDto, int>? nullRepository = null;
        var logger = new MockLogger();

        // Act
        bool threwException = false;
        try
        {
            var entity = new MockListOnlyEntity(nullRepository!, logger);
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
        var repository = new MockListOnlyRepository();
        var loggerFactory = new MockLoggerFactory();

        // Act
        var entity = new MockListOnlyEntity(repository, loggerFactory);

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
        IBaseRepositoryWithListOnly<MockDto, int>? nullRepository = null;
        var loggerFactory = new MockLoggerFactory();

        // Act
        bool threwException = false;
        try
        {
            var entity = new MockListOnlyEntity(nullRepository!, loggerFactory);
        }
        catch (ArgumentNullException)
        {
            threwException = true;
        }

        // Assert
        Assert.IsTrue(threwException, "Should throw ArgumentNullException when repository is null.");
    }

    /// <summary>
    /// Verifies that ListAsync returns list of DTOs successfully.
    /// </summary>
    /// <returns>A task representing the asynchronous unit test operation.</returns>
    [TestMethod]
    public async Task ListAsync_ReturnsListSuccessfully()
    {
        // Arrange
        var repository = new MockListOnlyRepository();
        var logger = new MockLogger();
        var entity = new MockListOnlyEntity(repository, logger);

        // Act
        var result = await entity.ListAsync();

        // Assert
        Assert.IsNotNull(result, "ListAsync should return a list.");
        Assert.IsTrue(repository.ListCalled, "Repository ListAsync should be called.");
    }

    /// <summary>
    /// Verifies that ListAsync uses cancellation token correctly.
    /// </summary>
    /// <returns>A task representing the asynchronous unit test operation.</returns>
    [TestMethod]
    public async Task ListAsync_WithCancellationToken_PassesTokenToRepository()
    {
        // Arrange
        var repository = new MockListOnlyRepository();
        var logger = new MockLogger();
        var entity = new MockListOnlyEntity(repository, logger);
        var cts = new CancellationTokenSource();

        // Act
        await entity.ListAsync(cts.Token);

        // Assert
        Assert.AreEqual(cts.Token, repository.LastCancellationToken, "CancellationToken should be passed to repository.");
    }

    /// <summary>
    /// Verifies that Repository property returns the injected repository.
    /// </summary>
    [TestMethod]
    public void RepositoryProperty_ReturnsInjectedRepository()
    {
        // Arrange
        var repository = new MockListOnlyRepository();
        var logger = new MockLogger();
        var entity = new MockListOnlyEntity(repository, logger);

        // Act
        var retrievedRepository = entity.Repository;

        // Assert
        Assert.AreSame(repository, retrievedRepository, "Repository property should return the injected repository.");
    }

    #endregion Public Methods
}