namespace Roadbed.Data;

using System.Data;

/// <summary>
/// Defines a factory for creating new instances of database connections.
/// </summary>
/// <remarks>
/// This is a DRY (don't repeat yourself) approach to database connection strings. You can learn more at:
/// https://dev.to/webjose/do-yourself-a-favor-when-writing-connection-strings-in-configuration-1emb.
/// </remarks>
public interface IDataConnectionFactory
{
    #region Public Properties

    /// <summary>
    /// Gets the connection string being used within this factory.
    /// </summary>
    DataConnecionString Connecion { get; }

    #endregion Public Properties

    #region Public Methods

    /// <summary>
    /// Creates and returns a new instance of a database connection.
    /// </summary>
    /// <returns>An <see cref="IDbConnection"/> representing the newly created database connection.
    /// The connection has been opened before it is returned.</returns>
    IDbConnection CreateOpenConnection();

    /// <summary>
    /// Creates and returns a new instance of a database connection.
    /// </summary>
    /// <param name="cancellationToken">Token to notify when an operation should be canceled.</param>
    /// <returns>An <see cref="IDbConnection"/> representing the newly created database connection.
    /// The connection has been opened before it is returned.</returns>
    Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken);

    #endregion Public Methods
}