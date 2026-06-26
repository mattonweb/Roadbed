namespace Roadbed.DbQueue;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Roadbed.Data;

/// <summary>
/// Provider-neutral execution port used by <see cref="QueueProcessor{T}"/>
/// to talk to a database without binding the core assembly to a specific
/// ADO.NET client.
/// </summary>
/// <remarks>
/// <para>
/// This mirrors <c>Roadbed.Logging</c>'s <c>ILoggingDataExecutor</c>: the
/// core <c>Roadbed.DbQueue</c> assembly references no MySqlConnector
/// (or any other driver), and a provider satellite — for example
/// <c>Roadbed.DbQueue.MySql</c> — supplies a thin adapter that forwards each
/// call to the matching <c>Roadbed.Data.*</c> executor. The host
/// registers the implementation through its satellite installer.
/// </para>
/// <para>
/// <strong>Connection per queue.</strong> The
/// <see cref="IDataConnectionFactory"/> is passed <em>per call</em> from
/// <see cref="QueueDefinition{T}.ConnectionFactory"/> rather than captured
/// once per assembly, so queues that live in different business schemas
/// resolve their own factory each time.
/// </para>
/// <para>
/// Implementations are stateless and thread-safe — they own no
/// connection lifetime themselves, and the <see cref="ILogger"/> passed in
/// carries the calling <see cref="QueueProcessor{T}"/>'s category so retry
/// diagnostics surface under the right logger name.
/// </para>
/// </remarks>
internal interface IDbQueueDataExecutor
{
    #region Public Methods

    /// <summary>
    /// Executes a non-query command (INSERT) and returns the number of rows
    /// affected.
    /// </summary>
    /// <param name="request">SQL plus parameters and retry knobs.</param>
    /// <param name="factory">The per-queue connection factory taken from <see cref="QueueDefinition{T}.ConnectionFactory"/>.</param>
    /// <param name="logger">Logger used for retry diagnostics; carries the calling <see cref="QueueProcessor{T}"/> category.</param>
    /// <param name="cancellationToken">Token observed by the call.</param>
    /// <returns>The rows-affected count.</returns>
    Task<int> ExecuteAsync(
        DataExecutorRequest request,
        IDataConnectionFactory factory,
        ILogger logger,
        CancellationToken cancellationToken);

    /// <summary>
    /// Executes a SELECT and materializes its rows as
    /// <typeparamref name="TRow"/>.
    /// </summary>
    /// <typeparam name="TRow">The row projection type. <see cref="QueueProcessor{T}"/> uses an internal claim-row POCO with column-aliased property names.</typeparam>
    /// <param name="request">SQL plus parameters and retry knobs.</param>
    /// <param name="factory">The per-queue connection factory taken from <see cref="QueueDefinition{T}.ConnectionFactory"/>.</param>
    /// <param name="logger">Logger used for retry diagnostics; carries the calling <see cref="QueueProcessor{T}"/> category.</param>
    /// <param name="cancellationToken">Token observed by the call.</param>
    /// <returns>The materialized rows.</returns>
    Task<IEnumerable<TRow>> QueryAsync<TRow>(
        DataExecutorRequest request,
        IDataConnectionFactory factory,
        ILogger logger,
        CancellationToken cancellationToken);

    #endregion Public Methods
}
