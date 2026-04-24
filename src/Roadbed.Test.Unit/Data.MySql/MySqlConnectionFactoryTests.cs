namespace Roadbed.Test.Unit.Data.MySql;

using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MySqlConnector;
using Roadbed.Data;
using Roadbed.Data.MySql;

/// <summary>
/// Contains unit tests for verifying the behavior of the MySqlConnectionFactory class.
/// </summary>
[TestClass]
public class MySqlConnectionFactoryTests
{
    /// <summary>
    /// Gets or sets object used to store information that is provided to unit tests.
    /// </summary>
    public TestContext TestContext { get; set; }

    #region Public Methods

    #region Constructor Tests

    /// <summary>
    /// Unit test to verify that the constructor with connection string initializes properties correctly.
    /// </summary>
    [TestMethod]
    public void Constructor_WithConnectionString_InitializesProperties()
    {
        // Arrange (Given)
        var connectionString = CreateConnectionString();

        // Act (When)
        var instance = new MySqlConnectionFactory(connectionString);

        // Assert (Then)
        Assert.IsNotNull(
            instance,
            "Instance should be created successfully.");
        Assert.IsNotNull(
            instance.Connecion,
            "Connecion property should be initialized.");
        Assert.AreSame(
            connectionString,
            instance.Connecion,
            "Connecion property should reference the same object that was passed to constructor.");
    }

    /// <summary>
    /// Unit test to verify that the constructor with null connection string throws ArgumentNullException.
    /// </summary>
    [TestMethod]
    public void Constructor_WithNullConnectionString_ThrowsArgumentNullException()
    {
        // Arrange (Given)
        DataConnecionString? nullConnectionString = null;
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            var instance = new MySqlConnectionFactory(nullConnectionString!);
        }
        catch (ArgumentNullException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "Constructor should throw ArgumentNullException when connection string is null.");
    }

    /// <summary>
    /// Unit test to verify that the constructor accepts a custom MySQL connection string.
    /// </summary>
    [TestMethod]
    public void Constructor_WithCustomConnectionString_InitializesCorrectly()
    {
        // Arrange (Given)
        string customConnectionString = "Server=db.example.com;Port=3306;Database=mydb;User ID=admin;Password=secret";
        var connectionString = new DataConnecionString(
            DataConnectionStringType.MySQL,
            customConnectionString);

        // Act (When)
        var instance = new MySqlConnectionFactory(connectionString);

        // Assert (Then)
        Assert.IsNotNull(
            instance,
            "Instance should be created successfully with custom connection string.");
        Assert.AreSame(
            connectionString,
            instance.Connecion,
            "Connecion property should reference the connection string object.");
        StringAssert.Contains(
            instance.Connecion.ConnectionString,
            "db.example.com",
            "Connection string should contain the custom server.");
    }

    #endregion Constructor Tests

    #region Property Tests

    /// <summary>
    /// Unit test to verify that Connecion property returns the connection string that was set.
    /// </summary>
    [TestMethod]
    public void Connecion_Get_ReturnsConnectionString()
    {
        // Arrange (Given)
        var connectionString = CreateConnectionString();
        var instance = new MySqlConnectionFactory(connectionString);

        // Act (When)
        var result = instance.Connecion;

        // Assert (Then)
        Assert.IsNotNull(
            result,
            "Connecion property should not be null.");
        Assert.AreSame(
            connectionString,
            result,
            "Connecion property should return the same object that was passed to constructor.");
    }

    /// <summary>
    /// Unit test to verify that Connecion property is read-only after initialization.
    /// </summary>
    [TestMethod]
    public void Connecion_Init_IsReadOnlyAfterConstruction()
    {
        // Arrange (Given)
        var connectionString = CreateConnectionString();
        var instance = new MySqlConnectionFactory(connectionString);

        // Act (When)
        var result = instance.Connecion;

        // Assert (Then)
        Assert.AreSame(
            connectionString,
            result,
            "Connecion property should be immutable after construction.");
    }

    /// <summary>
    /// Unit test to verify that Connecion property has the correct ConnectionStringType.
    /// </summary>
    [TestMethod]
    public void Connecion_AfterConstruction_HasCorrectConnectionStringType()
    {
        // Arrange (Given)
        var connectionString = CreateConnectionString();

        // Act (When)
        var instance = new MySqlConnectionFactory(connectionString);

        // Assert (Then)
        Assert.AreEqual(
            DataConnectionStringType.MySQL,
            instance.Connecion.ConnectionStringType,
            "Connection string type should be MySQL.");
    }

    #endregion Property Tests

    #region CreateOpenConnection Tests (Synchronous)

    /// <summary>
    /// Unit test to verify that CreateOpenConnection throws MySqlException
    /// when the connection cannot be opened.
    /// </summary>
    [TestMethod]
    public void CreateOpenConnection_InvalidHost_ThrowsMySqlException()
    {
        // Arrange (Given)
        var connectionString = CreateUnreachableConnectionString();
        var instance = new MySqlConnectionFactory(connectionString);
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            IDbConnection connection = instance.CreateOpenConnection();
            connection?.Dispose();
        }
        catch (MySqlException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "CreateOpenConnection should throw MySqlException when the connection cannot be opened.");
    }

    #endregion CreateOpenConnection Tests (Synchronous)

    #region CreateOpenConnectionAsync Tests (Asynchronous)

    /// <summary>
    /// Unit test to verify that CreateOpenConnectionAsync throws MySqlException
    /// when the connection cannot be opened.
    /// </summary>
    /// <returns>Task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task CreateOpenConnectionAsync_InvalidHost_ThrowsMySqlException()
    {
        // Arrange (Given)
        var connectionString = CreateUnreachableConnectionString();
        var instance = new MySqlConnectionFactory(connectionString);
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            IDbConnection connection = await instance.CreateOpenConnectionAsync(
                this.TestContext.CancellationToken);
            connection?.Dispose();
        }
        catch (MySqlException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "CreateOpenConnectionAsync should throw MySqlException when the connection cannot be opened.");
    }

    /// <summary>
    /// Unit test to verify that CreateOpenConnectionAsync respects cancellation.
    /// </summary>
    /// <returns>Task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task CreateOpenConnectionAsync_CancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange (Given)
        var connectionString = CreateUnreachableConnectionString();
        var instance = new MySqlConnectionFactory(connectionString);
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            await instance.CreateOpenConnectionAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "CreateOpenConnectionAsync should throw OperationCanceledException when the token is cancelled.");
    }

    #endregion CreateOpenConnectionAsync Tests (Asynchronous)

    #region Interface Implementation Tests

    /// <summary>
    /// Unit test to verify that MySqlConnectionFactory implements IDataConnectionFactory.
    /// </summary>
    [TestMethod]
    public void MySqlConnectionFactory_ImplementsIDataConnectionFactory()
    {
        // Arrange (Given)
        var connectionString = CreateConnectionString();

        // Act (When)
        IDataConnectionFactory instance = new MySqlConnectionFactory(connectionString);

        // Assert (Then)
        Assert.IsNotNull(
            instance,
            "MySqlConnectionFactory should implement IDataConnectionFactory.");
        Assert.IsInstanceOfType(
            instance,
            typeof(IDataConnectionFactory),
            "Instance should be assignable to IDataConnectionFactory.");
    }

    #endregion Interface Implementation Tests

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates a <see cref="DataConnecionString"/> with a valid MySQL connection string
    /// for property-level tests that do not open a connection.
    /// </summary>
    /// <returns>A configured <see cref="DataConnecionString"/> instance.</returns>
    private static DataConnecionString CreateConnectionString()
    {
        return new DataConnecionString(
            DataConnectionStringType.MySQL,
            "Server=localhost;Port=3306;Database=testdb;User ID=testuser;Password=testpass");
    }

    /// <summary>
    /// Creates a <see cref="DataConnecionString"/> with an unreachable host
    /// and a minimal timeout to ensure connection attempts fail quickly.
    /// Port 1 is used because it is reserved and will not have a MySQL instance.
    /// </summary>
    /// <returns>A configured <see cref="DataConnecionString"/> instance.</returns>
    private static DataConnecionString CreateUnreachableConnectionString()
    {
        return new DataConnecionString(
            DataConnectionStringType.MySQL,
            "Server=localhost;Port=1;Database=testdb;User ID=testuser;Password=testpass;Connection Timeout=1");
    }

    #endregion Private Methods
}
