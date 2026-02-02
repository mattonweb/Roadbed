namespace Roadbed.Test.Unit.Crud;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Crud;

/// <summary>
/// Contains unit tests for verifying the behavior of the
/// <see cref="BaseEntityRecord{TId}"/> abstract record type
/// through the <see cref="TestEntityRecord"/> concrete implementation.
/// </summary>
[TestClass]
public class BaseEntityRecordTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that default constructor initializes Name with null.
    /// </summary>
    [TestMethod]
    public void Constructor_NoParameters_InitializesNameWithNull()
    {
        // Arrange (Given)

        // Act (When)
        var instance = new TestEntityRecord();

        // Assert (Then)
        Assert.IsNull(
            instance.Name,
            "Name should be null when no value is provided at construction.");
    }

    /// <summary>
    /// Unit test to verify that default constructor initializes Description with null.
    /// </summary>
    [TestMethod]
    public void Constructor_NoParameters_InitializesDescriptionWithNull()
    {
        // Arrange (Given)

        // Act (When)
        var instance = new TestEntityRecord();

        // Assert (Then)
        Assert.IsNull(
            instance.Description,
            "Description should be null when no value is provided at construction.");
    }

    /// <summary>
    /// Unit test to verify that Id property can be set and retrieved.
    /// </summary>
    [TestMethod]
    public void Id_SetValidValue_ReturnsSetValue()
    {
        // Arrange (Given)
        var instance = new TestEntityRecord();
        long expectedId = 42;

        // Act (When)
        instance.Id = expectedId;

        // Assert (Then)
        Assert.AreEqual(
            expectedId,
            instance.Id,
            "Id should return the value that was set.");
    }

    /// <summary>
    /// Unit test to verify that Id can be set via object initializer syntax.
    /// </summary>
    [TestMethod]
    public void Id_ObjectInitializer_ReturnsSetValue()
    {
        // Arrange (Given)
        long expectedId = 99;

        // Act (When)
        var instance = new TestEntityRecord { Id = expectedId };

        // Assert (Then)
        Assert.AreEqual(
            expectedId,
            instance.Id,
            "Id should return the value assigned via object initializer.");
    }

    /// <summary>
    /// Unit test to verify that instance implements IEntity interface.
    /// </summary>
    [TestMethod]
    public void Instance_TypeCheck_ImplementsIEntity()
    {
        // Arrange (Given)

        // Act (When)
        var instance = new TestEntityRecord();

        // Assert (Then)
        Assert.IsInstanceOfType<IEntity<long>>(
            instance,
            "TestEntityRecord should implement IEntity<long>.");
    }

    /// <summary>
    /// Unit test to verify that Id is accessible through the IEntity interface.
    /// </summary>
    [TestMethod]
    public void Id_CastToIEntity_RetainsIdValue()
    {
        // Arrange (Given)
        long expectedId = 7;
        var instance = new TestEntityRecord { Id = expectedId };

        // Act (When)
        IEntity<long> entityRef = instance;

        // Assert (Then)
        Assert.AreEqual(
            expectedId,
            entityRef.Id,
            "Id should be accessible and retain its value through the IEntity interface.");
    }

    /// <summary>
    /// Unit test to verify that record equality compares by value.
    /// </summary>
    [TestMethod]
    public void Equals_SamePropertyValues_ReturnsTrue()
    {
        // Arrange (Given)
        var instance1 = new TestEntityRecord
        {
            Id = 1,
            Name = "Test",
            Description = "Description",
        };

        var instance2 = new TestEntityRecord
        {
            Id = 1,
            Name = "Test",
            Description = "Description",
        };

        // Act (When)
        bool areEqual = instance1.Equals(instance2);

        // Assert (Then)
        Assert.IsTrue(
            areEqual,
            "Two record instances with identical property values should be equal.");
    }

    /// <summary>
    /// Unit test to verify that record equality detects different Id values.
    /// </summary>
    [TestMethod]
    public void Equals_DifferentIdValues_ReturnsFalse()
    {
        // Arrange (Given)
        var instance1 = new TestEntityRecord
        {
            Id = 1,
            Name = "Test",
            Description = "Description",
        };

        var instance2 = new TestEntityRecord
        {
            Id = 2,
            Name = "Test",
            Description = "Description",
        };

        // Act (When)
        bool areEqual = instance1.Equals(instance2);

        // Assert (Then)
        Assert.IsFalse(
            areEqual,
            "Two record instances with different Id values should not be equal.");
    }

    /// <summary>
    /// Unit test to verify that record equality detects different Name values.
    /// </summary>
    [TestMethod]
    public void Equals_DifferentNameValues_ReturnsFalse()
    {
        // Arrange (Given)
        var instance1 = new TestEntityRecord
        {
            Id = 1,
            Name = "Alpha",
            Description = "Description",
        };

        var instance2 = new TestEntityRecord
        {
            Id = 1,
            Name = "Beta",
            Description = "Description",
        };

        // Act (When)
        bool areEqual = instance1.Equals(instance2);

        // Assert (Then)
        Assert.IsFalse(
            areEqual,
            "Two record instances with different Name values should not be equal.");
    }

    /// <summary>
    /// Unit test to verify that GetHashCode is consistent for equal records.
    /// </summary>
    [TestMethod]
    public void GetHashCode_SamePropertyValues_ReturnsSameHashCode()
    {
        // Arrange (Given)
        var instance1 = new TestEntityRecord
        {
            Id = 1,
            Name = "Test",
            Description = "Description",
        };

        var instance2 = new TestEntityRecord
        {
            Id = 1,
            Name = "Test",
            Description = "Description",
        };

        // Act (When)
        int hash1 = instance1.GetHashCode();
        int hash2 = instance2.GetHashCode();

        // Assert (Then)
        Assert.AreEqual(
            hash1,
            hash2,
            "Two record instances with identical property values should produce the same hash code.");
    }

    #endregion Public Methods
}