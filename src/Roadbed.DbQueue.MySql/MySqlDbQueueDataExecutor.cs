namespace Roadbed.DbQueue.MySql;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Roadbed.Data;
using Roadbed.Data.MySql;

/// <summary>
/// MySQL/MariaDB implementation of <see cref="IDbQueueDataExecutor"/>: a thin,
/// stateless adapter over <see cref="MySqlExecutor"/>.
/// </summary>
/// <remarks>
/// Mirrors the <c>MySqlLoggingDataExecutor</c> shape. The per-queue
/// <see cref="IDataConnectionFactory"/> arrives via each method's
/// <c>factory</c> argument (sourced from
/// <see cref="QueueDefinition{T}.ConnectionFactory"/>), so the same executor
/// singleton serves every queue regardless of which schema each queue lives
/// in.
/// </remarks>
internal sealed class MySqlDbQueueDataExecutor : IDbQueueDataExecutor
{
    #region Public Methods

    /// <inheritdoc/>
    public Task<int> ExecuteAsync(
        DataExecutorRequest request,
        IDataConnectionFactory factory,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        return MySqlExecutor.ExecuteAsync(request, factory, logger, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<TRow>> QueryAsync<TRow>(
        DataExecutorRequest request,
        IDataConnectionFactory factory,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        return MySqlExecutor.QueryAsync<TRow>(request, factory, logger, cancellationToken);
    }

    #endregion Public Methods
}
