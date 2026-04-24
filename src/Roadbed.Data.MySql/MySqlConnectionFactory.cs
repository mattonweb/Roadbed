namespace Roadbed.Data.MySql;

using System.Data;
using MySqlConnector;

/// <summary>
/// Factory to create instances of <see cref="MySqlConnection"/>.
/// </summary>
public class MySqlConnectionFactory : IDataConnectionFactory
{
    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="MySqlConnectionFactory"/> class.
    /// </summary>
    /// <param name="connectionString">Database connection string to use to create instances of <see cref="MySqlConnection"/>.</param>
    public MySqlConnectionFactory(DataConnecionString connectionString)
    {
        ArgumentNullException.ThrowIfNull(connectionString);

        this.Connecion = connectionString;
    }

    #endregion Public Constructors

    #region Public Properties

    /// <inheritdoc/>
    public DataConnecionString Connecion { get; init; }

    #endregion Public Properties

    #region Public Methods

    /// <inheritdoc/>
    /// <remarks>
    /// The returned connection is already open. Callers are responsible for disposing the connection
    /// after use, typically with a using statement or using declaration.
    /// </remarks>
    /// <exception cref="MySqlException">Thrown when the connection cannot be opened.</exception>
    public IDbConnection CreateOpenConnection()
    {
        var connection = new MySqlConnection(this.Connecion.ConnectionString);

        connection.Open();

        return connection;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The returned connection is already open. Callers are responsible for disposing the connection
    /// after use, typically with a using statement or using declaration.
    /// </remarks>
    /// <exception cref="MySqlException">Thrown when the connection cannot be opened.</exception>
    public async Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new MySqlConnection(this.Connecion.ConnectionString);

        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        return connection;
    }

    #endregion Public Methods
}
