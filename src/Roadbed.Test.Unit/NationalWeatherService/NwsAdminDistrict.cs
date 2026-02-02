namespace Roadbed.Test.Unit.Sdk.NationalWeatherService;

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Common;
using Roadbed.Messaging;
using Roadbed.Sdk.NationalWeatherService;

/// <summary>
/// Contains unit tests for verifying the behavior of the NwsAdminDistrict class.
/// </summary>
[TestClass]
public class NwsAdminDistrictTests
{
    #region Private Fields

    /// <summary>
    /// Reusable messaging request for unit tests.
    /// </summary>
    private static readonly MessagingMessageRequest<CommonKeyValuePair<string, string>> MessagingRequest =
        new MessagingMessageRequest<CommonKeyValuePair<string, string>>(
            new MessagingPublisher(CommonBusinessKey.FromString("Unit Test", true)),
            "UNIT-TEST");

    #endregion Private Fields

    #region Public Methods

    /// <summary>
    /// Unit test to verify that constructor throws exception when messagingRequest is null.
    /// </summary>
    [TestMethod]
    public void Constructor_NullMessagingRequest_ThrowsArgumentNullException()
    {
        // Arrange (Given)
        MessagingMessageRequest<CommonKeyValuePair<string, string>>? nullMessagingRequest = null;
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            var adminDistrict = new NwsAdminDistrict(nullMessagingRequest!);
        }
        catch (ArgumentNullException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "Constructor should throw ArgumentNullException when messagingRequest is null.");
    }

    #endregion Public Methods
}