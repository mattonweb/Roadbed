namespace Roadbed.Test.Unit.Crud;

using Roadbed.Crud;

/// <summary>
/// Concrete class entity used across all Roadbed.Crud unit tests.
/// </summary>
public sealed class TestEntityClass : BaseEntityClass<long>
{
    /// <summary>
    /// Gets or sets the entity name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the entity description.
    /// </summary>
    public string? Description { get; set; }
}