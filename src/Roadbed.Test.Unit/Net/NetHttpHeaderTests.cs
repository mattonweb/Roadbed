namespace Roadbed.Test.Unit.Net;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Net;

/// <summary>
/// Contains unit tests for verifying the behavior of the
/// <see cref="NetHttpHeader"/> record.
/// </summary>
[TestClass]
public class NetHttpHeaderTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that parameterless constructor initializes Name with null.
    /// </summary>
    [TestMethod]
    public void Constructor_NoParameters_InitializesNameWithNull()
    {
        // Arrange (Given)

        // Act (When)
        var instance = new NetHttpHeader();

        // Assert (Then)
        Assert.IsNull(
            instance.Name,
            "Name should be null when no value is provided at construction.");
    }

    /// <summary>
    /// Unit test to verify that parameterless constructor initializes Value with null.
    /// </summary>
    [TestMethod]
    public void Constructor_NoParameters_InitializesValueWithNull()
    {
        // Arrange (Given)

        // Act (When)
        var instance = new NetHttpHeader();

        // Assert (Then)
        Assert.IsNull(
            instance.Value,
            "Value should be null when no value is provided at construction.");
    }

    /// <summary>
    /// Unit test to verify that parameterized constructor sets Name.
    /// </summary>
    [TestMethod]
    public void Constructor_WithNameAndValue_SetsName()
    {
        // Arrange (Given)
        string expectedName = "Content-Type";
        string expectedValue = "application/json";

        // Act (When)
        var instance = new NetHttpHeader(expectedName, expectedValue);

        // Assert (Then)
        Assert.AreEqual(
            expectedName,
            instance.Name,
            "Name should return the value passed to the constructor.");
    }

    /// <summary>
    /// Unit test to verify that parameterized constructor sets Value.
    /// </summary>
    [TestMethod]
    public void Constructor_WithNameAndValue_SetsValue()
    {
        // Arrange (Given)
        string expectedName = "Content-Type";
        string expectedValue = "application/json";

        // Act (When)
        var instance = new NetHttpHeader(expectedName, expectedValue);

        // Assert (Then)
        Assert.AreEqual(
            expectedValue,
            instance.Value,
            "Value should return the value passed to the constructor.");
    }

    /// <summary>
    /// Unit test to verify that Name property can be set and retrieved.
    /// </summary>
    [TestMethod]
    public void Name_SetValidValue_ReturnsSetValue()
    {
        // Arrange (Given)
        var instance = new NetHttpHeader();
        string expectedName = "Authorization";

        // Act (When)
        instance.Name = expectedName;

        // Assert (Then)
        Assert.AreEqual(
            expectedName,
            instance.Name,
            "Name should return the value that was set.");
    }

    /// <summary>
    /// Unit test to verify that Value property can be set and retrieved.
    /// </summary>
    [TestMethod]
    public void Value_SetValidValue_ReturnsSetValue()
    {
        // Arrange (Given)
        var instance = new NetHttpHeader();
        string expectedValue = "Bearer token123";

        // Act (When)
        instance.Value = expectedValue;

        // Assert (Then)
        Assert.AreEqual(
            expectedValue,
            instance.Value,
            "Value should return the value that was set.");
    }

    /// <summary>
    /// Unit test to verify that record equality compares by value.
    /// </summary>
    [TestMethod]
    public void Equals_SamePropertyValues_ReturnsTrue()
    {
        // Arrange (Given)
        var instance1 = new NetHttpHeader("Accept", "text/html");
        var instance2 = new NetHttpHeader("Accept", "text/html");

        // Act (When)
        bool areEqual = instance1.Equals(instance2);

        // Assert (Then)
        Assert.IsTrue(
            areEqual,
            "Two record instances with identical property values should be equal.");
    }

    /// <summary>
    /// Unit test to verify that record equality detects different Name values.
    /// </summary>
    [TestMethod]
    public void Equals_DifferentNameValues_ReturnsFalse()
    {
        // Arrange (Given)
        var instance1 = new NetHttpHeader("Accept", "text/html");
        var instance2 = new NetHttpHeader("Content-Type", "text/html");

        // Act (When)
        bool areEqual = instance1.Equals(instance2);

        // Assert (Then)
        Assert.IsFalse(
            areEqual,
            "Two record instances with different Name values should not be equal.");
    }

    /// <summary>
    /// Unit test to verify that record equality detects different Value values.
    /// </summary>
    [TestMethod]
    public void Equals_DifferentValueValues_ReturnsFalse()
    {
        // Arrange (Given)
        var instance1 = new NetHttpHeader("Accept", "text/html");
        var instance2 = new NetHttpHeader("Accept", "application/json");

        // Act (When)
        bool areEqual = instance1.Equals(instance2);

        // Assert (Then)
        Assert.IsFalse(
            areEqual,
            "Two record instances with different Value values should not be equal.");
    }

    #endregion Public Methods
}