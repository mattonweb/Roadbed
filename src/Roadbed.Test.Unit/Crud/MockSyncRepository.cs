namespace Roadbed.Test.Unit.Crud;

using Roadbed.Crud;
using Roadbed.Crud.Repositories.Sync;

/// <summary>
/// Configurable mock repository implementing the full sync CRUDAL composite.
/// Satisfies all sync repository composite interfaces via inheritance.
/// </summary>
public sealed class MockSyncRepository
    : ISyncCrudalRepository<TestEntityRecord, long>
{
    #region Public Properties

    /// <summary>
    /// Gets or sets the delegate invoked when Create is called.
    /// </summary>
    public Func<TestEntityRecord, TestEntityRecord>?
        OnCreate
    { get; set; }

    /// <summary>
    /// Gets or sets the delegate invoked when Read is called.
    /// </summary>
    public Func<long, TestEntityRecord?>?
        OnRead
    { get; set; }

    /// <summary>
    /// Gets or sets the delegate invoked when Update is called.
    /// </summary>
    public Func<TestEntityRecord, TestEntityRecord>?
        OnUpdate
    { get; set; }

    /// <summary>
    /// Gets or sets the delegate invoked when Delete is called.
    /// </summary>
    public Action<long>?
        OnDelete
    { get; set; }

    /// <summary>
    /// Gets or sets the delegate invoked when Archive is called.
    /// </summary>
    public Func<long, TestEntityRecord>?
        OnArchive
    { get; set; }

    /// <summary>
    /// Gets or sets the delegate invoked when List is called.
    /// </summary>
    public Func<IList<TestEntityRecord>>?
        OnList
    { get; set; }

    /// <summary>
    /// Gets the number of times Create was called.
    /// </summary>
    public int CreateCallCount { get; private set; }

    /// <summary>
    /// Gets the number of times Read was called.
    /// </summary>
    public int ReadCallCount { get; private set; }

    /// <summary>
    /// Gets the number of times Update was called.
    /// </summary>
    public int UpdateCallCount { get; private set; }

    /// <summary>
    /// Gets the number of times Delete was called.
    /// </summary>
    public int DeleteCallCount { get; private set; }

    /// <summary>
    /// Gets the number of times Archive was called.
    /// </summary>
    public int ArchiveCallCount { get; private set; }

    /// <summary>
    /// Gets the number of times List was called.
    /// </summary>
    public int ListCallCount { get; private set; }

    /// <summary>
    /// Gets the last entity passed to Create.
    /// </summary>
    public TestEntityRecord? LastCreateEntity { get; private set; }

    /// <summary>
    /// Gets the last entity passed to Update.
    /// </summary>
    public TestEntityRecord? LastUpdateEntity { get; private set; }

    /// <summary>
    /// Gets the last identifier passed to Read.
    /// </summary>
    public long? LastReadId { get; private set; }

    /// <summary>
    /// Gets the last identifier passed to Delete.
    /// </summary>
    public long? LastDeleteId { get; private set; }

    /// <summary>
    /// Gets the last identifier passed to Archive.
    /// </summary>
    public long? LastArchiveId { get; private set; }

    #endregion Public Properties

    #region Public Methods

    /// <inheritdoc/>
    public TestEntityRecord Create(TestEntityRecord entity)
    {
        this.CreateCallCount++;
        this.LastCreateEntity = entity;

        if (this.OnCreate is not null)
        {
            return this.OnCreate(entity);
        }

        return entity;
    }

    /// <inheritdoc/>
    public TestEntityRecord? Read(long id)
    {
        this.ReadCallCount++;
        this.LastReadId = id;

        if (this.OnRead is not null)
        {
            return this.OnRead(id);
        }

        return null;
    }

    /// <inheritdoc/>
    public TestEntityRecord Update(TestEntityRecord entity)
    {
        this.UpdateCallCount++;
        this.LastUpdateEntity = entity;

        if (this.OnUpdate is not null)
        {
            return this.OnUpdate(entity);
        }

        return entity;
    }

    /// <inheritdoc/>
    public void Delete(long id)
    {
        this.DeleteCallCount++;
        this.LastDeleteId = id;

        if (this.OnDelete is not null)
        {
            this.OnDelete(id);
        }
    }

    /// <inheritdoc/>
    public TestEntityRecord Archive(long id)
    {
        this.ArchiveCallCount++;
        this.LastArchiveId = id;

        if (this.OnArchive is not null)
        {
            return this.OnArchive(id);
        }

        return new TestEntityRecord { Id = id };
    }

    /// <inheritdoc/>
    public IList<TestEntityRecord> List()
    {
        this.ListCallCount++;

        if (this.OnList is not null)
        {
            return this.OnList();
        }

        return new List<TestEntityRecord>();
    }

    #endregion Public Methods
}