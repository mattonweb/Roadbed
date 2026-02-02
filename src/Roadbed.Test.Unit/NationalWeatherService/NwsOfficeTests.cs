namespace Roadbed.Test.Unit.Sdk.NationalWeatherService;

using System;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Roadbed.Common;
using Roadbed.Messaging;
using Roadbed.Sdk.NationalWeatherService;
using Roadbed.Sdk.NationalWeatherService.Dtos;
using Roadbed.Sdk.NationalWeatherService.Repositories;

/// <summary>
/// Contains unit tests for verifying the behavior of the NwsOffice class.
/// </summary>
[TestClass]
public class NwsOfficeTests
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
    /// Unit test to verify that constructor throws exception when id is empty.
    /// </summary>
    [TestMethod]
    public void Constructor_EmptyId_ThrowsArgumentException()
    {
        // Arrange (Given)
        string emptyId = string.Empty;
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            var office = new NwsOffice(emptyId, MessagingRequest);
        }
        catch (ArgumentException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "Constructor should throw ArgumentException when id is empty.");
    }

    /// <summary>
    /// Unit test to verify that constructor throws exception when id is null.
    /// </summary>
    [TestMethod]
    public void Constructor_NullId_ThrowsArgumentException()
    {
        // Arrange (Given)
        string? nullId = null;
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            var office = new NwsOffice(nullId!, MessagingRequest);
        }
        catch (ArgumentException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "Constructor should throw ArgumentException when id is null.");
    }

    /// <summary>
    /// Unit test to verify that constructor with logger throws exception when logger is null.
    /// </summary>
    [TestMethod]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange (Given)
        string officeId = "HNX";
        var mockRepository = new Mock<INwsOfficeRepository>();
        ILogger? nullLogger = null;
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            var office = new NwsOffice(officeId, MessagingRequest, mockRepository.Object, nullLogger!);
        }
        catch (ArgumentNullException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "Constructor should throw ArgumentNullException when logger is null.");
    }

    /// <summary>
    /// Unit test to verify that constructor with loggerFactory throws exception when loggerFactory is null.
    /// </summary>
    [TestMethod]
    public void Constructor_NullLoggerFactory_ThrowsArgumentNullException()
    {
        // Arrange (Given)
        string officeId = "HNX";
        var mockRepository = new Mock<INwsOfficeRepository>();
        ILoggerFactory? nullLoggerFactory = null;
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            var office = new NwsOffice(officeId, MessagingRequest, mockRepository.Object, nullLoggerFactory!);
        }
        catch (ArgumentNullException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "Constructor should throw ArgumentNullException when loggerFactory is null.");
    }

    /// <summary>
    /// Unit test to verify that constructor throws exception when messagingRequest is null.
    /// </summary>
    [TestMethod]
    public void Constructor_NullMessagingRequest_ThrowsArgumentNullException()
    {
        // Arrange (Given)
        string officeId = "HNX";
        MessagingMessageRequest<CommonKeyValuePair<string, string>>? nullMessagingRequest = null;
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            var office = new NwsOffice(officeId, nullMessagingRequest!);
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

    /// <summary>
    /// Unit test to verify that constructor with repository throws exception when repository is null.
    /// </summary>
    [TestMethod]
    public void Constructor_NullRepository_ThrowsArgumentNullException()
    {
        // Arrange (Given)
        string officeId = "HNX";
        INwsOfficeRepository? nullRepository = null;
        var mockLogger = new Mock<ILogger>();
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            var office = new NwsOffice(officeId, MessagingRequest, nullRepository!, mockLogger.Object);
        }
        catch (ArgumentNullException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "Constructor should throw ArgumentNullException when repository is null.");
    }

    /// <summary>
    /// Unit test to verify that constructor throws exception when id is whitespace.
    /// </summary>
    [TestMethod]
    public void Constructor_WhitespaceId_ThrowsArgumentException()
    {
        // Arrange (Given)
        string whitespaceId = "   ";
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            var office = new NwsOffice(whitespaceId, MessagingRequest);
        }
        catch (ArgumentException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "Constructor should throw ArgumentException when id is whitespace.");
    }

    #endregion Public Methods
}