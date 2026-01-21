namespace Roadbed.Test.Unit.Crud.Mocks;

using System.Collections.Generic;
using Roadbed.Crud;

/// <summary>
/// Mock repository for testing.
/// </summary>
public class MockCrudlRepository
    : IBaseRepositoryWithCrudl<MockDto, int>
{
    #region Public Properties

    /// <summary>
    /// Gets a value indicating whether CreateAsync was called.
    /// </summary>
    public bool CreateCalled { get; private set; }

    /// <summary>
    /// Gets a value indicating whether DeleteAsync was called.
    /// </summary>
    public bool DeleteCalled { get; private set; }

    /// <summary>
    /// Gets a value indicating the last CancellationToken passed.
    /// </summary>
    public CancellationToken LastCancellationToken { get; private set; }

    /// <summary>
    /// Gets a value indicating whether ListAsync was called.
    /// </summary>
    public bool ListCalled { get; private set; }

    /// <summary>
    /// Gets a value indicating whether ReadAsync was called.
    /// </summary>
    public bool ReadCalled { get; private set; }

    /// <summary>
    /// Gets a value indicating whether UpdateAsync was called.
    /// </summary>
    public bool UpdateCalled { get; private set; }

    #endregion Public Properties

    #region Public Methods

    /// <inheritdoc />
    public async Task<int> CreateAsync(MockDto dto, CancellationToken cancellationToken = default)
    {
        this.CreateCalled = true;
        this.LastCancellationToken = cancellationToken;

        return await Task.FromResult(dto.Id);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        this.DeleteCalled = true;
        this.LastCancellationToken = cancellationToken;

        bool validID = false;

        if (id > 0)
        {
            validID = true;
        }

        return await Task.FromResult(validID);
    }

    /// <inheritdoc />
    public async Task<IList<MockDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        this.ListCalled = true;
        this.LastCancellationToken = cancellationToken;

        IList<MockDto> list = new List<MockDto>()
            {
                new MockDto { Id = 1, Name = "Test" },
            };

        return list;
    }

    /// <inheritdoc />
    public async Task<MockDto> ReadAsync(int id, CancellationToken cancellationToken = default)
    {
        this.ReadCalled = true;
        this.LastCancellationToken = cancellationToken;

        MockDto mock = new MockDto()
        {
            Id = id,
            Name = "Test",
        };

        return mock;
    }

    /// <inheritdoc />
    public async Task<bool> UpdateAsync(MockDto dto, CancellationToken cancellationToken = default)
    {
        this.UpdateCalled = true;
        this.LastCancellationToken = cancellationToken;

        bool validID = false;

        if (dto.Id > 0)
        {
            validID = true;
        }

        return await Task.FromResult(validID);
    }

    #endregion Public Methods
}