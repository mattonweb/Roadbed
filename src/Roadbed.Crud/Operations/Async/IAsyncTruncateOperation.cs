namespace Roadbed.Crud.Operations.Async;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Defines the asynchronous Truncate operation for an entity.
/// </summary>
/// <typeparam name="TEntity">Type of entity.</typeparam>
/// <typeparam name="TId">Data type for the identifier.</typeparam>
/// <remarks>
/// Removes every row from the table that backs <typeparamref name="TEntity"/>.
/// Designed for the snapshot-reload pattern: truncate, then bulk-insert the
/// full set in a single load cycle. Implementations typically issue a
/// <c>TRUNCATE TABLE</c> (which usually reports zero affected rows on most
/// backends); the operation returns no value because any count would be
/// meaningless across providers.
/// </remarks>
[SuppressMessage(
    "Major Code Smell",
    "S2326:Unused type parameters should be removed",
    Justification = "Keeping TEntity / TId to remain consistent with other operation interfaces.")]
public interface IAsyncTruncateOperation<TEntity, TId>
    where TEntity : IEntity<TId>
{
    #region Public Methods

    /// <summary>
    /// Truncates the table backing <typeparamref name="TEntity"/>.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that completes when the truncation has finished.</returns>
    Task TruncateAsync(
        CancellationToken cancellationToken = default);

    #endregion Public Methods
}
