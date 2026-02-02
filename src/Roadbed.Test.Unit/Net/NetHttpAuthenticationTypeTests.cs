namespace Roadbed.Test.Unit.Net;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Net;

/// <summary>
/// Contains unit tests for verifying the behavior of the
/// <see cref="NetHttpAuthenticationType"/> enumeration.
/// </summary>
[TestClass]
public class NetHttpAuthenticationTypeTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that Unknown has the expected integer value.
    /// </summary>
    [TestMethod]
    public void Unknown_IntegerValue_IsZero()
    {
        // Arrange (Given)
        int expectedValue = 0;

        // Act (When)
        int actualValue = (int)NetHttpAuthenticationType.Unknown;

        // Assert (Then)
        Assert.AreEqual(
            expectedValue,
            actualValue,
            "Unknown should have an integer value of 0.");
    }

    /// <summary>
    /// Unit test to verify that Basic has the expected integer value.
    /// </summary>
    [TestMethod]
    public void Basic_IntegerValue_IsOne()
    {
        // Arrange (Given)
        int expectedValue = 1;

        // Act (When)
        int actualValue = (int)NetHttpAuthenticationType.Basic;

        // Assert (Then)
        Assert.AreEqual(
            expectedValue,
            actualValue,
            "Basic should have an integer value of 1.");
    }

    /// <summary>
    /// Unit test to verify that Bearer has the expected integer value.
    /// </summary>
    [TestMethod]
    public void Bearer_IntegerValue_IsTwo()
    {
        // Arrange (Given)
        int expectedValue = 2;

        // Act (When)
        int actualValue = (int)NetHttpAuthenticationType.Bearer;

        // Assert (Then)
        Assert.AreEqual(
            expectedValue,
            actualValue,
            "Bearer should have an integer value of 2.");
    }

    /// <summary>
    /// Unit test to verify that the default value of the enumeration is Unknown.
    /// </summary>
    [TestMethod]
    public void Default_Uninitialized_IsUnknown()
    {
        // Arrange (Given)
        NetHttpAuthenticationType expectedDefault = NetHttpAuthenticationType.Unknown;

        // Act (When)
        NetHttpAuthenticationType actualDefault = default;

        // Assert (Then)
        Assert.AreEqual(
            expectedDefault,
            actualDefault,
            "The default value of NetHttpAuthenticationType should be Unknown.");
    }

    #endregion Public Methods
}