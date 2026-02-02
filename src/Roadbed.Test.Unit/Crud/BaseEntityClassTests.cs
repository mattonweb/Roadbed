namespace Roadbed.Test.Unit.Crud;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Crud;

/// <summary>
/// Contains unit tests for verifying the behavior of the
/// <see cref="BaseEntityClass{TId}"/> abstract class type
/// through the <see cref="TestEntityClass"/> concrete implementation.
/// </summary>
[TestClass]
public class BaseEntityClassTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that default constructor initializes Id with default value.
    /// </summary>
    [TestMethod]
    public void Constructor_NoParameters_InitializesIdWithDefaultValue()
    {
        // Arrange (Given)

        // Act (When)
        var instance = new TestEntityClass();

        // Assert (Then)
        Assert.AreEqual(
            default(long),
            instance.Id,
            "Id should be the default value for long when no value is provided at construction.");
    }

    /// <summary>
    /// Unit test to verify that default constructor initializes Name with null.
    /// </summary>
    [TestMethod]
    public void Constructor_NoParameters_InitializesNameWithNull()
    {
        // Arrange (Given)

        // Act (When)
        var instance = new TestEntityClass();

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
        var instance = new TestEntityClass();

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
        var instance = new TestEntityClass();
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
        var instance = new TestEntityClass { Id = expectedId };

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
        var instance = new TestEntityClass();

        // Assert (Then)
        Assert.IsInstanceOfType<IEntity<long>>(
            instance,
            "TestEntityClass should implement IEntity<long>.");
    }

    /// <summary>
    /// Unit test to verify that Id is accessible through the IEntity interface.
    /// </summary>
    [TestMethod]
    public void Id_CastToIEntity_RetainsIdValue()
    {
        // Arrange (Given)
        long expectedId = 7;
        var instance = new TestEntityClass { Id = expectedId };

        // Act (When)
        IEntity<long> entityRef = instance;

        // Assert (Then)
        Assert.AreEqual(
            expectedId,
            entityRef.Id,
            "Id should be accessible and retain its value through the IEntity interface.");
    }

    /// <summary>
    /// Unit test to verify that class equality uses reference equality, not value equality.
    /// </summary>
    [TestMethod]
    public void Equals_SamePropertyValuesDifferentInstances_ReturnsFalse()
    {
        // Arrange (Given)
        var instance1 = new TestEntityClass
        {
            Id = 1,
            Name = "Test",
            Description = "Description",
        };

        var instance2 = new TestEntityClass
        {
            Id = 1,
            Name = "Test",
            Description = "Description",
        };

        // Act (When)
        bool areEqual = instance1.Equals(instance2);

        // Assert (Then)
        Assert.IsFalse(
            areEqual,
            "Two class instances with identical property values should not be equal because classes use reference equality.");
    }

    /// <summary>
    /// Unit test to verify that class equality returns true for the same reference.
    /// </summary>
    [TestMethod]
    public void Equals_SameReference_ReturnsTrue()
    {
        // Arrange (Given)
        var instance = new TestEntityClass
        {
            Id = 1,
            Name = "Test",
            Description = "Description",
        };

        // Act (When)
        bool areEqual = instance.Equals(instance);

        // Assert (Then)
        Assert.IsTrue(
            areEqual,
            "A class instance compared to itself should return true (reference equality).");
    }

    /// <summary>
    /// Unit test to verify that Name property can be set and retrieved.
    /// </summary>
    [TestMethod]
    public void Name_SetValidValue_ReturnsSetValue()
    {
        // Arrange (Given)
        var instance = new TestEntityClass();
        string expectedName = "TestName";

        // Act (When)
        instance.Name = expectedName;

        // Assert (Then)
        Assert.AreEqual(
            expectedName,
            instance.Name,
            "Name should return the value that was set.");
    }

    /// <summary>
    /// Unit test to verify that Description property can be set and retrieved.
    /// </summary>
    [TestMethod]
    public void Description_SetValidValue_ReturnsSetValue()
    {
        // Arrange (Given)
        var instance = new TestEntityClass();
        string expectedDescription = "TestDescription";

        // Act (When)
        instance.Description = expectedDescription;

        // Assert (Then)
        Assert.AreEqual(
            expectedDescription,
            instance.Description,
            "Description should return the value that was set.");
    }

    #endregion Public Methods
}