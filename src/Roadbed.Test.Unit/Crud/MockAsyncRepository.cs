namespace Roadbed.Test.Unit.Crud;

using Roadbed.Crud;
using Roadbed.Crud.Repositories.Async;

/// <summary>
/// Configurable mock repository implementing the full async CRUDAL composite.
/// Satisfies all async repository composite interfaces via inheritance.
/// </summary>
public sealed class MockAsyncRepository
    : IAsyncCrudalRepository<TestEntityRecord, long>
{
    #region Public Properties

    /// <summary>
    /// Gets or sets the delegate invoked when CreateAsync is called.
    /// </summary>
    public Func<TestEntityRecord, CancellationToken, Task<TestEntityRecord>>?
        OnCreateAsync
    { get; set; }

    /// <summary>
    /// Gets or sets the delegate invoked when ReadAsync is called.
    /// </summary>
    public Func<long, CancellationToken, Task<TestEntityRecord?>>?
        OnReadAsync
    { get; set; }

    /// <summary>
    /// Gets or sets the delegate invoked when UpdateAsync is called.
    /// </summary>
    public Func<TestEntityRecord, CancellationToken, Task<TestEntityRecord>>?
        OnUpdateAsync
    { get; set; }

    /// <summary>
    /// Gets or sets the delegate invoked when DeleteAsync is called.
    /// </summary>
    public Func<long, CancellationToken, Task>?
        OnDeleteAsync
    { get; set; }

    /// <summary>
    /// Gets or sets the delegate invoked when ArchiveAsync is called.
    /// </summary>
    public Func<long, CancellationToken, Task<TestEntityRecord>>?
        OnArchiveAsync
    { get; set; }

    /// <summary>
    /// Gets or sets the delegate invoked when ListAsync is called.
    /// </summary>
    public Func<CancellationToken, Task<IList<TestEntityRecord>>>?
        OnListAsync
    { get; set; }

    /// <summary>
    /// Gets the number of times CreateAsync was called.
    /// </summary>
    public int CreateAsyncCallCount { get; private set; }

    /// <summary>
    /// Gets the number of times ReadAsync was called.
    /// </summary>
    public int ReadAsyncCallCount { get; private set; }

    /// <summary>
    /// Gets the number of times UpdateAsync was called.
    /// </summary>
    public int UpdateAsyncCallCount { get; private set; }

    /// <summary>
    /// Gets the number of times DeleteAsync was called.
    /// </summary>
    public int DeleteAsyncCallCount { get; private set; }

    /// <summary>
    /// Gets the number of times ArchiveAsync was called.
    /// </summary>
    public int ArchiveAsyncCallCount { get; private set; }

    /// <summary>
    /// Gets the number of times ListAsync was called.
    /// </summary>
    public int ListAsyncCallCount { get; private set; }

    /// <summary>
    /// Gets the last entity passed to CreateAsync.
    /// </summary>
    public TestEntityRecord? LastCreateEntity { get; private set; }

    /// <summary>
    /// Gets the last entity passed to UpdateAsync.
    /// </summary>
    public TestEntityRecord? LastUpdateEntity { get; private set; }

    /// <summary>
    /// Gets the last identifier passed to ReadAsync.
    /// </summary>
    public long? LastReadId { get; private set; }

    /// <summary>
    /// Gets the last identifier passed to DeleteAsync.
    /// </summary>
    public long? LastDeleteId { get; private set; }

    /// <summary>
    /// Gets the last identifier passed to ArchiveAsync.
    /// </summary>
    public long? LastArchiveId { get; private set; }

    #endregion Public Properties

    #region Public Methods

    /// <inheritdoc/>
    public async Task<TestEntityRecord> CreateAsync(
        TestEntityRecord entity,
        CancellationToken cancellationToken = default)
    {
        this.CreateAsyncCallCount++;
        this.LastCreateEntity = entity;

        if (this.OnCreateAsync is not null)
        {
            return await this.OnCreateAsync(entity, cancellationToken);
        }

        return entity;
    }

    /// <inheritdoc/>
    public async Task<TestEntityRecord?> ReadAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        this.ReadAsyncCallCount++;
        this.LastReadId = id;

        if (this.OnReadAsync is not null)
        {
            return await this.OnReadAsync(id, cancellationToken);
        }

        return null;
    }

    /// <inheritdoc/>
    public async Task<TestEntityRecord> UpdateAsync(
        TestEntityRecord entity,
        CancellationToken cancellationToken = default)
    {
        this.UpdateAsyncCallCount++;
        this.LastUpdateEntity = entity;

        if (this.OnUpdateAsync is not null)
        {
            return await this.OnUpdateAsync(entity, cancellationToken);
        }

        return entity;
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        this.DeleteAsyncCallCount++;
        this.LastDeleteId = id;

        if (this.OnDeleteAsync is not null)
        {
            await this.OnDeleteAsync(id, cancellationToken);
        }

        await Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<TestEntityRecord> ArchiveAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        this.ArchiveAsyncCallCount++;
        this.LastArchiveId = id;

        if (this.OnArchiveAsync is not null)
        {
            return await this.OnArchiveAsync(id, cancellationToken);
        }

        return new TestEntityRecord { Id = id };
    }

    /// <inheritdoc/>
    public async Task<IList<TestEntityRecord>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        this.ListAsyncCallCount++;

        if (this.OnListAsync is not null)
        {
            return await this.OnListAsync(cancellationToken);
        }

        return new List<TestEntityRecord>();
    }

    #endregion Public Methods
}