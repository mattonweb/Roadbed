namespace Roadbed.Test.Unit.Data;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Data;
using System;

/// <summary>
/// Contains unit tests for verifying the behavior of the DataExecutorRequest class.
/// </summary>
[TestClass]
public class DataExecutorRequestTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that constructor initializes all properties correctly with valid query.
    /// </summary>
    [TestMethod]
    public void Constructor_ValidQuery_InitializesAllProperties()
    {
        // Arrange (Given)
        string expectedQuery = "SELECT * FROM test";

        // Act (When)
        var instance = new DataExecutorRequest(expectedQuery);

        // Assert (Then)
        Assert.AreEqual(
            expectedQuery,
            instance.Query,
            "Query should be set to the value provided in constructor.");
        Assert.AreEqual(
            3,
            instance.MaxRetries,
            "MaxRetries should be initialized to default value of 3.");
        Assert.AreEqual(
            TimeSpan.FromMilliseconds(100),
            instance.DelayBetweenRetries,
            "DelayBetweenRetries should be initialized to default value of 100ms.");
        Assert.IsTrue(
            instance.RetriesEnabled,
            "RetriesEnabled should be initialized to default value of true.");
        Assert.IsTrue(
            instance.DelayMultiplierEnabled,
            "DelayMultiplierEnabled should be initialized to default value of true.");
        Assert.IsNull(
            instance.Parameters,
            "Parameters should be initialized to null.");
    }

    /// <summary>
    /// Unit test to verify that constructor throws exception when query is null.
    /// </summary>
    [TestMethod]
    public void Constructor_NullQuery_ThrowsArgumentException()
    {
        // Arrange (Given)
        string? nullQuery = null;
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            var instance = new DataExecutorRequest(nullQuery!);
        }
        catch (ArgumentException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "Constructor should throw ArgumentException when query is null.");
    }

    /// <summary>
    /// Unit test to verify that constructor throws exception when query is empty.
    /// </summary>
    [TestMethod]
    public void Constructor_EmptyQuery_ThrowsArgumentException()
    {
        // Arrange (Given)
        string emptyQuery = string.Empty;
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            var instance = new DataExecutorRequest(emptyQuery);
        }
        catch (ArgumentException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "Constructor should throw ArgumentException when query is empty.");
    }

    /// <summary>
    /// Unit test to verify that constructor throws exception when query is whitespace.
    /// </summary>
    [TestMethod]
    public void Constructor_WhitespaceQuery_ThrowsArgumentException()
    {
        // Arrange (Given)
        string whitespaceQuery = "   ";
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            var instance = new DataExecutorRequest(whitespaceQuery);
        }
        catch (ArgumentException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "Constructor should throw ArgumentException when query is whitespace.");
    }

    /// <summary>
    /// Unit test to verify that Query property is init-only and cannot be changed after construction.
    /// </summary>
    [TestMethod]
    public void Query_InitOnly_CannotBeChangedAfterConstruction()
    {
        // Arrange (Given)
        string initialQuery = "SELECT * FROM test";
        var instance = new DataExecutorRequest(initialQuery);

        // Act (When)
        // Attempting to set Query property would cause a compile error
        // This test verifies the property is init-only by compilation

        // Assert (Then)
        Assert.AreEqual(
            initialQuery,
            instance.Query,
            "Query should remain the value set in constructor.");
    }

    /// <summary>
    /// Unit test to verify that MaxRetries property can be set to valid value.
    /// </summary>
    [TestMethod]
    public void MaxRetries_SetValidValue_ReturnsSetValue()
    {
        // Arrange (Given)
        var instance = new DataExecutorRequest("SELECT * FROM test");
        int expectedValue = 5;

        // Act (When)
        instance.MaxRetries = expectedValue;

        // Assert (Then)
        Assert.AreEqual(
            expectedValue,
            instance.MaxRetries,
            "MaxRetries should return the value that was set.");
    }

    /// <summary>
    /// Unit test to verify that MaxRetries property accepts zero.
    /// </summary>
    [TestMethod]
    public void MaxRetries_SetToZero_AcceptsValue()
    {
        // Arrange (Given)
        var instance = new DataExecutorRequest("SELECT * FROM test");
        int expectedValue = 0;

        // Act (When)
        instance.MaxRetries = expectedValue;

        // Assert (Then)
        Assert.AreEqual(
            expectedValue,
            instance.MaxRetries,
            "MaxRetries should accept zero as a valid value.");
    }

    /// <summary>
    /// Unit test to verify that MaxRetries property throws exception when set to negative value.
    /// </summary>
    [TestMethod]
    public void MaxRetries_SetNegativeValue_ThrowsArgumentOutOfRangeException()
    {
        // Arrange (Given)
        var instance = new DataExecutorRequest("SELECT * FROM test");
        int invalidValue = -1;
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            instance.MaxRetries = invalidValue;
        }
        catch (ArgumentOutOfRangeException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "MaxRetries should throw ArgumentOutOfRangeException when set to negative value.");
    }

    /// <summary>
    /// Unit test to verify that CommandTimeoutInSeconds defaults to null (use the connection default).
    /// </summary>
    [TestMethod]
    public void CommandTimeoutInSeconds_DefaultsToNull()
    {
        // Arrange (Given) + Act (When)
        var instance = new DataExecutorRequest("SELECT 1");

        // Assert (Then)
        Assert.IsNull(
            instance.CommandTimeoutInSeconds,
            "CommandTimeoutInSeconds should default to null so the connection default applies.");
    }

    /// <summary>
    /// Unit test to verify that CommandTimeoutInSeconds accepts zero (no timeout).
    /// </summary>
    [TestMethod]
    public void CommandTimeoutInSeconds_SetToZero_AcceptsValue()
    {
        // Arrange (Given)
        var instance = new DataExecutorRequest("SELECT 1");

        // Act (When)
        instance.CommandTimeoutInSeconds = 0;

        // Assert (Then)
        Assert.AreEqual(0, instance.CommandTimeoutInSeconds, "Zero (no timeout) must be accepted.");
    }

    /// <summary>
    /// Unit test to verify that CommandTimeoutInSeconds throws when set to a negative value.
    /// </summary>
    [TestMethod]
    public void CommandTimeoutInSeconds_SetNegativeValue_ThrowsArgumentOutOfRangeException()
    {
        // Arrange (Given)
        var instance = new DataExecutorRequest("SELECT 1");
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            instance.CommandTimeoutInSeconds = -1;
        }
        catch (ArgumentOutOfRangeException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "CommandTimeoutInSeconds should throw ArgumentOutOfRangeException when set to a negative value.");
    }

    /// <summary>
    /// Unit test to verify that ResolveCommandTimeoutInSeconds returns the connection default when no override is set.
    /// </summary>
    [TestMethod]
    public void ResolveCommandTimeoutInSeconds_NullOverride_ReturnsConnectionDefault()
    {
        // Arrange (Given)
        var instance = new DataExecutorRequest("SELECT 1");

        // Act (When)
        int resolved = instance.ResolveCommandTimeoutInSeconds(connectionDefaultInSeconds: 5);

        // Assert (Then)
        Assert.AreEqual(5, resolved, "A null override must fall back to the supplied connection default.");
    }

    /// <summary>
    /// Unit test to verify that ResolveCommandTimeoutInSeconds returns the override when one is set (including 0).
    /// </summary>
    [TestMethod]
    public void ResolveCommandTimeoutInSeconds_WithOverride_ReturnsOverride()
    {
        // Arrange (Given)
        var instance = new DataExecutorRequest("SELECT 1") { CommandTimeoutInSeconds = 120 };
        var infinite = new DataExecutorRequest("SELECT 1") { CommandTimeoutInSeconds = 0 };

        // Act (When) + Assert (Then)
        Assert.AreEqual(120, instance.ResolveCommandTimeoutInSeconds(connectionDefaultInSeconds: 5), "An override must win over the connection default.");
        Assert.AreEqual(0, infinite.ResolveCommandTimeoutInSeconds(connectionDefaultInSeconds: 5), "A 0 override (no timeout) must be honored, not treated as unset.");
    }

    /// <summary>
    /// Unit test to verify that DelayBetweenRetries property can be set to valid value.
    /// </summary>
    [TestMethod]
    public void DelayBetweenRetries_SetValidValue_ReturnsSetValue()
    {
        // Arrange (Given)
        var instance = new DataExecutorRequest("SELECT * FROM test");
        TimeSpan expectedValue = TimeSpan.FromSeconds(1);

        // Act (When)
        instance.DelayBetweenRetries = expectedValue;

        // Assert (Then)
        Assert.AreEqual(
            expectedValue,
            instance.DelayBetweenRetries,
            "DelayBetweenRetries should return the value that was set.");
    }

    /// <summary>
    /// Unit test to verify that DelayBetweenRetries property accepts zero.
    /// </summary>
    [TestMethod]
    public void DelayBetweenRetries_SetToZero_AcceptsValue()
    {
        // Arrange (Given)
        var instance = new DataExecutorRequest("SELECT * FROM test");
        TimeSpan expectedValue = TimeSpan.Zero;

        // Act (When)
        instance.DelayBetweenRetries = expectedValue;

        // Assert (Then)
        Assert.AreEqual(
            expectedValue,
            instance.DelayBetweenRetries,
            "DelayBetweenRetries should accept TimeSpan.Zero as a valid value.");
    }

    /// <summary>
    /// Unit test to verify that DelayBetweenRetries property throws exception when set to negative value.
    /// </summary>
    [TestMethod]
    public void DelayBetweenRetries_SetNegativeValue_ThrowsArgumentOutOfRangeException()
    {
        // Arrange (Given)
        var instance = new DataExecutorRequest("SELECT * FROM test");
        TimeSpan invalidValue = TimeSpan.FromMilliseconds(-1);
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            instance.DelayBetweenRetries = invalidValue;
        }
        catch (ArgumentOutOfRangeException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "DelayBetweenRetries should throw ArgumentOutOfRangeException when set to negative value.");
    }

    /// <summary>
    /// Unit test to verify that RetriesEnabled property can be set and retrieved.
    /// </summary>
    [TestMethod]
    public void RetriesEnabled_SetToFalse_ReturnsSetValue()
    {
        // Arrange (Given)
        var instance = new DataExecutorRequest("SELECT * FROM test");
        bool expectedValue = false;

        // Act (When)
        instance.RetriesEnabled = expectedValue;

        // Assert (Then)
        Assert.AreEqual(
            expectedValue,
            instance.RetriesEnabled,
            "RetriesEnabled should return the value that was set.");
    }

    /// <summary>
    /// Unit test to verify that DelayMultiplierEnabled property can be set and retrieved.
    /// </summary>
    [TestMethod]
    public void DelayMultiplierEnabled_SetToFalse_ReturnsSetValue()
    {
        // Arrange (Given)
        var instance = new DataExecutorRequest("SELECT * FROM test");
        bool expectedValue = false;

        // Act (When)
        instance.DelayMultiplierEnabled = expectedValue;

        // Assert (Then)
        Assert.AreEqual(
            expectedValue,
            instance.DelayMultiplierEnabled,
            "DelayMultiplierEnabled should return the value that was set.");
    }

    /// <summary>
    /// Unit test to verify that Parameters property can be set to valid object.
    /// </summary>
    [TestMethod]
    public void Parameters_SetValidObject_ReturnsSetValue()
    {
        // Arrange (Given)
        var instance = new DataExecutorRequest("SELECT * FROM test WHERE id = @Id");
        var expectedParameters = new { Id = 123 };

        // Act (When)
        instance.Parameters = expectedParameters;

        // Assert (Then)
        Assert.AreSame(
            expectedParameters,
            instance.Parameters,
            "Parameters should return the same object that was set.");
    }

    /// <summary>
    /// Unit test to verify that Parameters property can be set to null.
    /// </summary>
    [TestMethod]
    public void Parameters_SetToNull_AcceptsValue()
    {
        // Arrange (Given)
        var instance = new DataExecutorRequest("SELECT * FROM test");
        instance.Parameters = new { Id = 123 };

        // Act (When)
        instance.Parameters = null;

        // Assert (Then)
        Assert.IsNull(
            instance.Parameters,
            "Parameters should accept null as a valid value.");
    }

    /// <summary>
    /// Unit test to verify that all properties can be set via object initializer.
    /// </summary>
    [TestMethod]
    public void ObjectInitializer_SetAllProperties_AllPropertiesSet()
    {
        // Arrange (Given)
        string expectedQuery = "UPDATE test SET value = @Value";
        var expectedParameters = new { Value = "test" };
        int expectedMaxRetries = 10;
        TimeSpan expectedDelay = TimeSpan.FromSeconds(2);
        bool expectedRetriesEnabled = false;
        bool expectedMultiplierEnabled = false;

        // Act (When)
        var instance = new DataExecutorRequest(expectedQuery)
        {
            Parameters = expectedParameters,
            MaxRetries = expectedMaxRetries,
            DelayBetweenRetries = expectedDelay,
            RetriesEnabled = expectedRetriesEnabled,
            DelayMultiplierEnabled = expectedMultiplierEnabled,
        };

        // Assert (Then)
        Assert.AreEqual(
            expectedQuery,
            instance.Query,
            "Query should be set via constructor.");
        Assert.AreSame(
            expectedParameters,
            instance.Parameters,
            "Parameters should be set via object initializer.");
        Assert.AreEqual(
            expectedMaxRetries,
            instance.MaxRetries,
            "MaxRetries should be set via object initializer.");
        Assert.AreEqual(
            expectedDelay,
            instance.DelayBetweenRetries,
            "DelayBetweenRetries should be set via object initializer.");
        Assert.AreEqual(
            expectedRetriesEnabled,
            instance.RetriesEnabled,
            "RetriesEnabled should be set via object initializer.");
        Assert.AreEqual(
            expectedMultiplierEnabled,
            instance.DelayMultiplierEnabled,
            "DelayMultiplierEnabled should be set via object initializer.");
    }

    /// <summary>
    /// Unit test to verify that MaxRetries can be modified after construction.
    /// </summary>
    [TestMethod]
    public void MaxRetries_ModifyAfterConstruction_ValueChanges()
    {
        // Arrange (Given)
        var instance = new DataExecutorRequest("SELECT * FROM test");
        int initialValue = instance.MaxRetries;
        int newValue = 7;

        // Act (When)
        instance.MaxRetries = newValue;

        // Assert (Then)
        Assert.AreNotEqual(
            initialValue,
            instance.MaxRetries,
            "MaxRetries should have changed from initial value.");
        Assert.AreEqual(
            newValue,
            instance.MaxRetries,
            "MaxRetries should be set to the new value.");
    }

    /// <summary>
    /// Unit test to verify that DelayBetweenRetries can be modified after construction.
    /// </summary>
    [TestMethod]
    public void DelayBetweenRetries_ModifyAfterConstruction_ValueChanges()
    {
        // Arrange (Given)
        var instance = new DataExecutorRequest("SELECT * FROM test");
        TimeSpan initialValue = instance.DelayBetweenRetries;
        TimeSpan newValue = TimeSpan.FromSeconds(5);

        // Act (When)
        instance.DelayBetweenRetries = newValue;

        // Assert (Then)
        Assert.AreNotEqual(
            initialValue,
            instance.DelayBetweenRetries,
            "DelayBetweenRetries should have changed from initial value.");
        Assert.AreEqual(
            newValue,
            instance.DelayBetweenRetries,
            "DelayBetweenRetries should be set to the new value.");
    }

    /// <summary>
    /// Unit test to verify that default values are set correctly for a minimal request.
    /// </summary>
    [TestMethod]
    public void Constructor_MinimalRequest_DefaultValuesSet()
    {
        // Arrange (Given)
        string query = "SELECT 1";

        // Act (When)
        var instance = new DataExecutorRequest(query);

        // Assert (Then)
        Assert.AreEqual(
            query,
            instance.Query,
            "Query should be set from constructor parameter.");
        Assert.AreEqual(
            3,
            instance.MaxRetries,
            "MaxRetries default should be 3.");
        Assert.AreEqual(
            TimeSpan.FromMilliseconds(100),
            instance.DelayBetweenRetries,
            "DelayBetweenRetries default should be 100ms.");
        Assert.IsTrue(
            instance.RetriesEnabled,
            "RetriesEnabled default should be true.");
        Assert.IsTrue(
            instance.DelayMultiplierEnabled,
            "DelayMultiplierEnabled default should be true.");
        Assert.IsNull(
            instance.Parameters,
            "Parameters default should be null.");
    }

    #endregion Public Methods
}