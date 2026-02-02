namespace Roadbed.Test.Unit.Net;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Net;

/// <summary>
/// Contains unit tests for verifying the behavior of the
/// <see cref="NetHttpAuthentication"/> class.
/// </summary>
[TestClass]
public class NetHttpAuthenticationTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that constructor initializes AuthenticationType with default value.
    /// </summary>
    [TestMethod]
    public void Constructor_NoParameters_InitializesAuthenticationTypeWithDefault()
    {
        // Arrange (Given)

        // Act (When)
        var instance = new NetHttpAuthentication();

        // Assert (Then)
        Assert.AreEqual(
            NetHttpAuthenticationType.Unknown,
            instance.AuthenticationType,
            "AuthenticationType should default to Unknown when no value is provided.");
    }

    /// <summary>
    /// Unit test to verify that constructor initializes Value with null.
    /// </summary>
    [TestMethod]
    public void Constructor_NoParameters_InitializesValueWithNull()
    {
        // Arrange (Given)

        // Act (When)
        var instance = new NetHttpAuthentication();

        // Assert (Then)
        Assert.IsNull(
            instance.Value,
            "Value should be null when no value is provided at construction.");
    }

    /// <summary>
    /// Unit test to verify that AuthenticationType property can be set and retrieved.
    /// </summary>
    [TestMethod]
    public void AuthenticationType_SetToBasic_ReturnsBasic()
    {
        // Arrange (Given)
        var instance = new NetHttpAuthentication();

        // Act (When)
        instance.AuthenticationType = NetHttpAuthenticationType.Basic;

        // Assert (Then)
        Assert.AreEqual(
            NetHttpAuthenticationType.Basic,
            instance.AuthenticationType,
            "AuthenticationType should return the value that was set.");
    }

    /// <summary>
    /// Unit test to verify that AuthenticationType property can be set to Bearer.
    /// </summary>
    [TestMethod]
    public void AuthenticationType_SetToBearer_ReturnsBearer()
    {
        // Arrange (Given)
        var instance = new NetHttpAuthentication();

        // Act (When)
        instance.AuthenticationType = NetHttpAuthenticationType.Bearer;

        // Assert (Then)
        Assert.AreEqual(
            NetHttpAuthenticationType.Bearer,
            instance.AuthenticationType,
            "AuthenticationType should return the value that was set.");
    }

    /// <summary>
    /// Unit test to verify that Value property can be set and retrieved.
    /// </summary>
    [TestMethod]
    public void Value_SetValidString_ReturnsSetValue()
    {
        // Arrange (Given)
        var instance = new NetHttpAuthentication();
        string expectedValue = "my-secret-token";

        // Act (When)
        instance.Value = expectedValue;

        // Assert (Then)
        Assert.AreEqual(
            expectedValue,
            instance.Value,
            "Value should return the string that was set.");
    }

    #endregion Public Methods
}