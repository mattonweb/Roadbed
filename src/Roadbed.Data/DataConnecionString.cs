namespace Roadbed.Data;

using System.Data.Common;

/// <summary>
/// Entity for data connection string.
/// </summary>
public class DataConnecionString
{
    /// <summary>
    /// Default SQL command timeout, in seconds, applied when neither the request
    /// nor the connection overrides it. Deliberately short: a query that needs
    /// more is a signal to optimize the SQL or rethink the design, and any
    /// override should carry a code comment saying why.
    /// </summary>
    public const int DefaultCommandTimeoutInSeconds = 5;

    /// <summary>
    /// Container for the original connection string.
    /// </summary>
    private readonly string originalConnectionString = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataConnecionString"/> class.
    /// </summary>
    /// <param name="connectionStringType">Type of connection string.</param>
    public DataConnecionString(DataConnectionStringType connectionStringType)
    {
        this.ConnectionStringType = connectionStringType;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataConnecionString"/> class.
    /// </summary>
    /// <param name="connectionStringType">Type of connection string.</param>
    /// <param name="connectionString">Custom connection string.</param>
    public DataConnecionString(
        DataConnectionStringType connectionStringType,
        string connectionString)
    {
        this.ConnectionStringType = connectionStringType;
        this.originalConnectionString = connectionString;
    }

    /// <summary>
    /// Gets or sets the default SQL command (statement) timeout, in seconds, for
    /// executions against this connection. Defaults to
    /// <see cref="DefaultCommandTimeoutInSeconds"/>. A per-execution
    /// <c>DataExecutorRequest.CommandTimeoutInSeconds</c> overrides it.
    /// </summary>
    /// <remarks>
    /// This is the command timeout (how long a statement may run), distinct from
    /// <see cref="TimeoutInSeconds"/>, which is the connection-open timeout.
    /// </remarks>
    public int CommandTimeoutInSeconds { get; set; } = DefaultCommandTimeoutInSeconds;

    /// <summary>
    /// Gets the connection string.
    /// </summary>
    public string ConnectionString
    {
        get
        {
            return this.CreateConnectionString();
        }
    }

    /// <summary>
    /// Gets a <see cref="DbConnectionStringBuilder"/> for the connection string.
    /// </summary>
    public DbConnectionStringBuilder ConnectionStringBuilder
    {
        get
        {
            return new DbConnectionStringBuilder()
            {
                ConnectionString = this.ConnectionString,
            };
        }
    }

    /// <summary>
    /// Gets the Type of connection string.
    /// </summary>
    public DataConnectionStringType ConnectionStringType
    {
        get;
        internal set;
    }

    /// <summary>
    /// Gets or sets the Database Source for the connection string.
    /// </summary>
    public string? DatabaseSource
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the Password for the connection string.
    /// </summary>
    public string? Password
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the Server Name for the connection string.
    /// </summary>
    public string? ServerName
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the connection-open (connect) timeout for the connection
    /// string, in seconds.
    /// </summary>
    /// <remarks>
    /// This is the time allowed to <em>establish</em> a connection — distinct from
    /// <see cref="CommandTimeoutInSeconds"/>, which bounds how long a statement may
    /// run. It maps to <c>Connection Timeout</c> (MySQL) / <c>Timeout</c>
    /// (PostgreSQL); on SQLite it maps to <c>Default Timeout</c>.
    /// </remarks>
    public int TimeoutInSeconds { get; set; } = 20;

    /// <summary>
    /// Gets or sets the Username for the connection string.
    /// </summary>
    public string? Username
    {
        get;
        set;
    }

    /// <summary>
    /// Creates the connection string based on the property values.
    /// </summary>
    /// <returns>Connection string used to access a data source.</returns>
    private string CreateConnectionString()
    {
        string result = string.Empty;

        if (string.IsNullOrEmpty(this.originalConnectionString))
        {
            if (this.ConnectionStringType == DataConnectionStringType.SQLite)
            {
                result = this.CreateConnectionStringForSqlite();
            }
            else if (this.ConnectionStringType == DataConnectionStringType.SQLiteInMemory)
            {
                result = this.CreateConnectionStringForSqliteInMemory();
            }
            else if (this.ConnectionStringType == DataConnectionStringType.PostgreSQL)
            {
                result = this.CreateConnectionStringForPostgresql();
            }
            else if (this.ConnectionStringType == DataConnectionStringType.MySQL)
            {
                result = this.CreateConnectionStringForMySql();
            }
        }
        else
        {
            result = this.originalConnectionString;
        }

        return result;
    }

    /// <summary>
    /// Creates a connection string for SQLite database.
    /// </summary>
    /// <returns>Connection string used to access a database.</returns>
    private string CreateConnectionStringForSqlite()
    {
        string template = $$"""
            Data Source={{this.DatabaseSource}};
            Foreign Keys=true;
            Pooling=true;
            Default Timeout={{this.TimeoutInSeconds}};
            """;

        return template;
    }

    /// <summary>
    /// Creates a connection string for a PostgreSQL database.
    /// </summary>
    /// <returns>Connection string used to access a database.</returns>
    private string CreateConnectionStringForPostgresql()
    {
        string template = $$"""
            Host={{this.ServerName}};
            Database={{this.DatabaseSource}};
            Username={{this.Username}};
            Password={{this.Password}};
            Timeout={{this.TimeoutInSeconds}};
            """;

        return template;
    }

    /// <summary>
    /// Creates a connection string for a MySQL database.
    /// </summary>
    /// <returns>Connection string used to access a database.</returns>
    /// <remarks>
    /// AutoEnlist=true causes MySqlConnector to automatically enlist the connection
    /// in the ambient <see cref="System.Transactions.Transaction"/>. This is the default for
    /// MySqlConnector, but is set explicitly here to signal intent.
    /// </remarks>
    private string CreateConnectionStringForMySql()
    {
        string template = $$"""
            Server={{this.ServerName}};
            Database={{this.DatabaseSource}};
            User ID={{this.Username}};
            Password={{this.Password}};
            Connection Timeout={{this.TimeoutInSeconds}};
            AutoEnlist=true;
            """;

        return template;
    }

    /// <summary>
    /// Creates a connection string for an in-memmory SQLite database.
    /// </summary>
    /// <returns>Connection string used to access a database.</returns>
    private string CreateConnectionStringForSqliteInMemory()
    {
        // Use DatabaseSource if provided, otherwise use default name
        string dataSource = string.IsNullOrWhiteSpace(this.DatabaseSource)
            ? "DefaultInMemory"
            : this.DatabaseSource;

        string template = $$"""
            Data Source={{dataSource}};
            Mode=Memory;
            Cache=Shared;
            Foreign Keys=true;
            Pooling=true;
            Default Timeout={{this.TimeoutInSeconds}};
            """;

        return template;
    }
}
