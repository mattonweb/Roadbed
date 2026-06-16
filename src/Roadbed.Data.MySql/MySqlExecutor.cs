namespace Roadbed.Data.MySql;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MySqlConnector;

/// <summary>
/// Provides methods for executing MySQL database operations using Dapper.
/// </summary>
public static class MySqlExecutor
{
    #region Public Methods

    /// <summary>
    /// Executes a non-query command asynchronously with optional retry logic.
    /// </summary>
    /// <param name="request">The request containing query, parameters, and retry configuration.</param>
    /// <param name="connectionFactory">Connection factory for database access.</param>
    /// <param name="logger">Logger for diagnostics and retry tracking. Defaults to NullLogger if not provided.</param>
    /// <param name="cancellationToken">Token to notify when an operation should be canceled.</param>
    /// <returns>The number of rows affected.</returns>
    /// <exception cref="ArgumentNullException">Thrown if request or connectionFactory is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if all retries are exhausted.</exception>
    public static async Task<int> ExecuteAsync(
        DataExecutorRequest request,
        IDataConnectionFactory connectionFactory,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(connectionFactory);

        logger ??= NullLogger.Instance;

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Executing command: {Query}", TruncateQuery(request.Query));
        }

        if (!request.RetriesEnabled)
        {
            // No retry logic - execute once
            using (var dbConnection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false))
            {
                return await dbConnection
                    .ExecuteAsync(CreateCommand(request, connectionFactory))
                    .ConfigureAwait(false);
            }
        }

        // Execute with retry logic - pass logger
        return await ExecuteWithRetryAsync(request, connectionFactory, logger, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a query and returns the results asynchronously with optional retry logic.
    /// </summary>
    /// <typeparam name="T">The type of objects to return.</typeparam>
    /// <param name="request">The request containing query, parameters, and retry configuration.</param>
    /// <param name="connectionFactory">Connection factory for database access.</param>
    /// <param name="logger">Logger for diagnostics and retry tracking. Defaults to NullLogger if not provided.</param>
    /// <param name="cancellationToken">Token to notify when an operation should be canceled.</param>
    /// <returns>A collection of results.</returns>
    /// <exception cref="ArgumentNullException">Thrown if request or connectionFactory is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if all retries are exhausted.</exception>
    public static async Task<IEnumerable<T>> QueryAsync<T>(
        DataExecutorRequest request,
        IDataConnectionFactory connectionFactory,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(connectionFactory);

        logger ??= NullLogger.Instance;

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Executing command: {Query}", TruncateQuery(request.Query));
        }

        if (!request.RetriesEnabled)
        {
            // No retry logic - execute once
            using (var dbConnection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false))
            {
                return await dbConnection
                    .QueryAsync<T>(CreateCommand(request, connectionFactory))
                    .ConfigureAwait(false);
            }
        }

        // Execute with retry logic - pass logger
        return await QueryWithRetryAsync<T>(request, connectionFactory, logger, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a query and returns a single result asynchronously with optional retry logic.
    /// </summary>
    /// <typeparam name="T">The type of object to return.</typeparam>
    /// <param name="request">The request containing query, parameters, and retry configuration.</param>
    /// <param name="connectionFactory">Connection factory for database access.</param>
    /// <param name="logger">Logger for diagnostics and retry tracking. Defaults to NullLogger if not provided.</param>
    /// <param name="cancellationToken">Token to notify when an operation should be canceled.</param>
    /// <returns>A single result or default if not found.</returns>
    /// <exception cref="ArgumentNullException">Thrown if request or connectionFactory is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if all retries are exhausted.</exception>
    public static async Task<T?> QuerySingleOrDefaultAsync<T>(
        DataExecutorRequest request,
        IDataConnectionFactory connectionFactory,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(connectionFactory);

        logger ??= NullLogger.Instance;

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Executing command: {Query}", TruncateQuery(request.Query));
        }

        if (!request.RetriesEnabled)
        {
            // No retry logic - execute once
            using (var dbConnection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false))
            {
                return await dbConnection
                    .QuerySingleOrDefaultAsync<T>(CreateCommand(request, connectionFactory))
                    .ConfigureAwait(false);
            }
        }

        // Execute with retry logic - pass logger
        return await QuerySingleOrDefaultWithRetryAsync<T>(request, connectionFactory, logger, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a query and returns a scalar value asynchronously with optional retry logic.
    /// </summary>
    /// <typeparam name="T">The type of scalar value to return.</typeparam>
    /// <param name="request">The request containing query, parameters, and retry configuration.</param>
    /// <param name="connectionFactory">Connection factory for database access.</param>
    /// <param name="logger">Logger for diagnostics and retry tracking. Defaults to NullLogger if not provided.</param>
    /// <param name="cancellationToken">Token to notify when an operation should be canceled.</param>
    /// <returns>The scalar value from the first column of the first row.</returns>
    /// <exception cref="ArgumentNullException">Thrown if request or connectionFactory is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if all retries are exhausted.</exception>
    public static async Task<T?> ExecuteScalarAsync<T>(
        DataExecutorRequest request,
        IDataConnectionFactory connectionFactory,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(connectionFactory);

        logger ??= NullLogger.Instance;

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Executing command: {Query}", TruncateQuery(request.Query));
        }

        if (!request.RetriesEnabled)
        {
            // No retry logic - execute once
            using (var dbConnection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false))
            {
                return await dbConnection
                    .ExecuteScalarAsync<T>(CreateCommand(request, connectionFactory))
                    .ConfigureAwait(false);
            }
        }

        // Execute with retry logic - pass logger
        return await ExecuteScalarWithRetryAsync<T>(request, connectionFactory, logger, cancellationToken).ConfigureAwait(false);
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Executes a non-query command with retry logic.
    /// </summary>
    [SuppressMessage(
        "Minor Code Smell",
        "S6667:Logging in a catch clause should pass the caught exception as a parameter.",
        Justification = "Last exception logged as Error outside of catch block.")]
    private static async Task<int> ExecuteWithRetryAsync(
        DataExecutorRequest request,
        IDataConnectionFactory connectionFactory,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        int attempt = 0;
        Exception? lastException = null;

        while (attempt <= request.MaxRetries)
        {
            try
            {
                using (var dbConnection = await connectionFactory
                    .CreateOpenConnectionAsync(cancellationToken)
                    .ConfigureAwait(false))
                {
                    int result = await dbConnection
                        .ExecuteAsync(CreateCommand(request, connectionFactory))
                        .ConfigureAwait(false);

                    if (attempt > 0)
                    {
                        if (logger.IsEnabled(LogLevel.Debug))
                        {
                            logger.LogDebug(
                                "Command succeeded on attempt {Attempt}. Rows affected: {Rows}",
                                attempt + 1,
                                result);
                        }
                    }

                    return result;
                }
            }
            catch (MySqlException ex) when (IsTransientError(ex) && attempt < request.MaxRetries)
            {
                lastException = ex;
                attempt++;

                var delay = CalculateDelay(request, attempt);

                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.LogDebug(
                        "Transient error on attempt {Attempt}: {ErrorCode} - {Message}. Retrying in {DelayMs}ms...",
                        attempt,
                        ex.Number,
                        ex.Message,
                        delay.TotalMilliseconds);
                }

                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }

        if (logger.IsEnabled(LogLevel.Error))
        {
            logger.LogError(
                lastException,
                "Command failed after {Attempts} attempts",
                request.MaxRetries + 1);
        }

        throw new InvalidOperationException(
            $"Failed to execute command after {request.MaxRetries} retries.",
            lastException);
    }

    /// <summary>
    /// Executes a query with retry logic.
    /// </summary>
    [SuppressMessage(
        "Minor Code Smell",
        "S6667:Logging in a catch clause should pass the caught exception as a parameter.",
        Justification = "Last exception logged as Error outside of catch block.")]
    private static async Task<IEnumerable<T>> QueryWithRetryAsync<T>(
        DataExecutorRequest request,
        IDataConnectionFactory connectionFactory,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        int attempt = 0;
        Exception? lastException = null;

        while (attempt <= request.MaxRetries)
        {
            try
            {
                using (var dbConnection = await connectionFactory
                    .CreateOpenConnectionAsync(cancellationToken)
                    .ConfigureAwait(false))
                {
                    var result = await dbConnection
                        .QueryAsync<T>(CreateCommand(request, connectionFactory))
                        .ConfigureAwait(false);

                    if (attempt > 0)
                    {
                        if (logger.IsEnabled(LogLevel.Debug))
                        {
                            logger.LogDebug(
                                "Query succeeded on attempt {Attempt}. Rows returned: {Count}",
                                attempt + 1,
                                result.Count());
                        }
                    }

                    return result;
                }
            }
            catch (MySqlException ex) when (IsTransientError(ex) && attempt < request.MaxRetries)
            {
                lastException = ex;
                attempt++;

                var delay = CalculateDelay(request, attempt);

                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.LogDebug(
                        "Transient error on attempt {Attempt}: {ErrorCode} - {Message}. Retrying in {DelayMs}ms...",
                        attempt,
                        ex.Number,
                        ex.Message,
                        delay.TotalMilliseconds);
                }

                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }

        if (logger.IsEnabled(LogLevel.Error))
        {
            logger.LogError(
                lastException,
                "Query failed after {Attempts} attempts",
                request.MaxRetries + 1);
        }

        throw new InvalidOperationException(
            $"Failed to execute query after {request.MaxRetries} retries.",
            lastException);
    }

    /// <summary>
    /// Executes a single-result query with retry logic.
    /// </summary>
    [SuppressMessage(
        "Minor Code Smell",
        "S6667:Logging in a catch clause should pass the caught exception as a parameter.",
        Justification = "Last exception logged as Error outside of catch block.")]
    private static async Task<T?> QuerySingleOrDefaultWithRetryAsync<T>(
        DataExecutorRequest request,
        IDataConnectionFactory connectionFactory,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        int attempt = 0;
        Exception? lastException = null;

        while (attempt <= request.MaxRetries)
        {
            try
            {
                using (var dbConnection = await connectionFactory
                    .CreateOpenConnectionAsync(cancellationToken)
                    .ConfigureAwait(false))
                {
                    var result = await dbConnection
                        .QuerySingleOrDefaultAsync<T>(CreateCommand(request, connectionFactory))
                        .ConfigureAwait(false);

                    if (attempt > 0)
                    {
                        if (logger.IsEnabled(LogLevel.Debug))
                        {
                            logger.LogDebug(
                                "Query succeeded on attempt {Attempt}. Result: {Found}",
                                attempt + 1,
                                !Equals(result, default(T)) ? "found" : "not found");
                        }
                    }

                    return result;
                }
            }
            catch (MySqlException ex) when (IsTransientError(ex) && attempt < request.MaxRetries)
            {
                lastException = ex;
                attempt++;

                var delay = CalculateDelay(request, attempt);

                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.LogDebug(
                        "Transient error on attempt {Attempt}: {ErrorCode} - {Message}. Retrying in {DelayMs}ms...",
                        attempt,
                        ex.Number,
                        ex.Message,
                        delay.TotalMilliseconds);
                }

                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }

        if (logger.IsEnabled(LogLevel.Error))
        {
            logger.LogError(
                lastException,
                "Query failed after {Attempts} attempts",
                request.MaxRetries + 1);
        }

        throw new InvalidOperationException(
            $"Failed to execute query after {request.MaxRetries} retries.",
            lastException);
    }

    /// <summary>
    /// Executes a scalar query with retry logic.
    /// </summary>
    [SuppressMessage(
        "Minor Code Smell",
        "S6667:Logging in a catch clause should pass the caught exception as a parameter.",
        Justification = "Last exception logged as Error outside of catch block.")]
    private static async Task<T?> ExecuteScalarWithRetryAsync<T>(
        DataExecutorRequest request,
        IDataConnectionFactory connectionFactory,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        int attempt = 0;
        Exception? lastException = null;

        while (attempt <= request.MaxRetries)
        {
            try
            {
                using (var dbConnection = await connectionFactory
                    .CreateOpenConnectionAsync(cancellationToken)
                    .ConfigureAwait(false))
                {
                    var result = await dbConnection
                        .ExecuteScalarAsync<T>(CreateCommand(request, connectionFactory))
                        .ConfigureAwait(false);

                    if (attempt > 0)
                    {
                        if (logger.IsEnabled(LogLevel.Debug))
                        {
                            logger.LogDebug(
                                "Query succeeded on attempt {Attempt}. Scalar result: {Result}",
                                attempt + 1,
                                result);
                        }
                    }

                    return result;
                }
            }
            catch (MySqlException ex) when (IsTransientError(ex) && attempt < request.MaxRetries)
            {
                lastException = ex;
                attempt++;

                var delay = CalculateDelay(request, attempt);

                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.LogDebug(
                        "Transient error on attempt {Attempt}: {ErrorCode} - {Message}. Retrying in {DelayMs}ms...",
                        attempt,
                        ex.Number,
                        ex.Message,
                        delay.TotalMilliseconds);
                }

                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }

        if (logger.IsEnabled(LogLevel.Error))
        {
            logger.LogError(
                lastException,
                "Query failed after {Attempts} attempts",
                request.MaxRetries + 1);
        }

        throw new InvalidOperationException(
            $"Failed to execute scalar query after {request.MaxRetries} retries.",
            lastException);
    }

    /// <summary>
    /// Builds the Dapper command for an execution, applying the resolved command
    /// timeout — the per-execution <see cref="DataExecutorRequest.CommandTimeoutInSeconds"/>
    /// override when set, otherwise the connection's
    /// <see cref="DataConnecionString.CommandTimeoutInSeconds"/> default.
    /// </summary>
    /// <param name="request">The execution request carrying the SQL, parameters, and any timeout override.</param>
    /// <param name="connectionFactory">Connection factory supplying the default command timeout.</param>
    /// <returns>A <see cref="CommandDefinition"/> ready to pass to Dapper.</returns>
    private static CommandDefinition CreateCommand(
        DataExecutorRequest request,
        IDataConnectionFactory connectionFactory)
    {
        return new CommandDefinition(
            request.Query,
            request.Parameters,
            commandTimeout: request.ResolveCommandTimeoutInSeconds(connectionFactory.Connecion.CommandTimeoutInSeconds));
    }

    /// <summary>
    /// Calculates the delay for retry based on configuration.
    /// </summary>
    private static TimeSpan CalculateDelay(DataExecutorRequest request, int attempt)
    {
        return request.DelayMultiplierEnabled
            ? TimeSpan.FromMilliseconds(request.DelayBetweenRetries.TotalMilliseconds * attempt)
            : request.DelayBetweenRetries;
    }

    /// <summary>
    /// Determines if a MySQL exception is transient and worth retrying.
    /// </summary>
    /// <remarks>
    /// MySqlConnector exposes the MySQL error number via <see cref="MySqlException.Number"/>.
    /// Transient errors are grouped by category:
    ///
    /// Server-side connection / resource errors (1xxx):
    ///   1040 = ER_CON_COUNT_ERROR (too many connections)
    ///   1042 = ER_BAD_HOST_ERROR (cannot resolve hostname)
    ///   1043 = ER_HANDSHAKE_ERROR (bad handshake)
    ///   1077 = ER_NORMAL_SHUTDOWN (server is shutting down)
    ///   1129 = ER_HOST_IS_BLOCKED (too many connection errors from host)
    ///   1158 = ER_NET_READ_ERROR_FROM_PIPE
    ///   1159 = ER_NET_READ_INTERRUPTED
    ///   1160 = ER_NET_ERROR_ON_WRITE
    ///   1161 = ER_NET_WRITE_INTERRUPTED
    ///   1184 = ER_NEW_ABORTING_CONNECTION
    ///
    /// Lock / deadlock errors:
    ///   1205 = ER_LOCK_WAIT_TIMEOUT (lock wait timeout exceeded)
    ///   1213 = ER_LOCK_DEADLOCK (deadlock found, transaction was rolled back)
    ///
    /// Client-side connection errors (2xxx):
    ///   2002 = CR_CONNECTION_ERROR (cannot connect through socket)
    ///   2003 = CR_CONN_HOST_ERROR (cannot connect to MySQL server)
    ///   2006 = CR_SERVER_GONE_ERROR (server has gone away)
    ///   2013 = CR_SERVER_LOST (lost connection to server during query).
    /// </remarks>
    [SuppressMessage(
        "StyleCop.CSharp.DocumentationRules",
        "SA1629:Documentation text should end with a period",
        Justification = "The list in the comment doesn't need a period.")]
    private static bool IsTransientError(MySqlException ex)
    {
        return ex.Number switch
        {
            // Server-side connection / resource errors
            1040 => true,   // too_many_connections
            1042 => true,   // bad_host
            1043 => true,   // handshake_error
            1077 => true,   // normal_shutdown
            1129 => true,   // host_is_blocked
            1158 => true,   // net_read_error_from_pipe
            1159 => true,   // net_read_interrupted
            1160 => true,   // net_error_on_write
            1161 => true,   // net_write_interrupted
            1184 => true,   // new_aborting_connection

            // Lock / deadlock
            1205 => true,   // lock_wait_timeout
            1213 => true,   // deadlock

            // Client-side connection errors
            2002 => true,   // connection_error
            2003 => true,   // conn_host_error
            2006 => true,   // server_gone_error
            2013 => true,   // server_lost

            _ => false,
        };
    }

    /// <summary>
    /// Truncates a SQL query string for logging purposes.
    /// </summary>
    private static string TruncateQuery(string query, int maxLength = 200)
    {
        if (string.IsNullOrEmpty(query))
        {
            return string.Empty;
        }

        return query.Length <= maxLength
            ? query
            : query.Substring(0, maxLength) + "...";
    }

    #endregion Private Methods
}
