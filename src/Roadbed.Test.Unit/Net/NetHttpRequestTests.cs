namespace Roadbed.Test.Unit.Net;

using System;
using System.Net.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Net;

/// <summary>
/// Contains unit tests for verifying the behavior of the
/// <see cref="NetHttpRequest"/> class.
/// </summary>
[TestClass]
public class NetHttpRequestTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that constructor initializes Method with HttpMethod.Get.
    /// </summary>
    [TestMethod]
    public void Constructor_NoParameters_InitializesMethodWithGet()
    {
        // Arrange (Given)

        // Act (When)
        var instance = new NetHttpRequest();

        // Assert (Then)
        Assert.AreEqual(
            HttpMethod.Get,
            instance.Method,
            "Method should default to HttpMethod.Get.");
    }

    /// <summary>
    /// Unit test to verify that constructor initializes EnableCompression with true.
    /// </summary>
    [TestMethod]
    public void Constructor_NoParameters_InitializesEnableCompressionWithTrue()
    {
        // Arrange (Given)

        // Act (When)
        var instance = new NetHttpRequest();

        // Assert (Then)
        Assert.IsTrue(
            instance.EnableCompression,
            "EnableCompression should default to true.");
    }

    /// <summary>
    /// Unit test to verify that constructor initializes TimeoutInSecondsPerAttempt with 15.
    /// </summary>
    [TestMethod]
    public void Constructor_NoParameters_InitializesTimeoutInSecondsPerAttemptWith15()
    {
        // Arrange (Given)

        // Act (When)
        var instance = new NetHttpRequest();

        // Assert (Then)
        Assert.AreEqual(
            15,
            instance.TimeoutInSecondsPerAttempt,
            "TimeoutInSecondsPerAttempt should default to 15 seconds.");
    }

    /// <summary>
    /// Unit test to verify that constructor initializes RetryPattern with MaxAttempts of 3.
    /// </summary>
    [TestMethod]
    public void Constructor_NoParameters_InitializesRetryPatternMaxAttemptsWith3()
    {
        // Arrange (Given)

        // Act (When)
        var instance = new NetHttpRequest();

        // Assert (Then)
        Assert.IsNotNull(
            instance.RetryPattern,
            "RetryPattern should not be null after construction.");
        Assert.AreEqual(
            3,
            instance.RetryPattern.MaxAttempts,
            "RetryPattern.MaxAttempts should default to 3.");
    }

    /// <summary>
    /// Unit test to verify that constructor initializes RetryPattern with DelayMultiplierInSeconds of 5.
    /// </summary>
    [TestMethod]
    public void Constructor_NoParameters_InitializesRetryPatternDelayMultiplierWith5()
    {
        // Arrange (Given)

        // Act (When)
        var instance = new NetHttpRequest();

        // Assert (Then)
        Assert.AreEqual(
            5,
            instance.RetryPattern.DelayMultiplierInSeconds,
            "RetryPattern.DelayMultiplierInSeconds should default to 5.");
    }

    /// <summary>
    /// Unit test to verify that constructor initializes HttpHeaders with empty list.
    /// </summary>
    [TestMethod]
    public void Constructor_NoParameters_InitializesHttpHeadersWithEmptyList()
    {
        // Arrange (Given)

        // Act (When)
        var instance = new NetHttpRequest();

        // Assert (Then)
        Assert.IsNotNull(
            instance.HttpHeaders,
            "HttpHeaders should not be null after construction.");
        Assert.HasCount(
            0,
            instance.HttpHeaders,
            "HttpHeaders should be an empty list after construction.");
    }

    /// <summary>
    /// Unit test to verify that constructor initializes Authentication with null.
    /// </summary>
    [TestMethod]
    public void Constructor_NoParameters_InitializesAuthenticationWithNull()
    {
        // Arrange (Given)

        // Act (When)
        var instance = new NetHttpRequest();

        // Assert (Then)
        Assert.IsNull(
            instance.Authentication,
            "Authentication should be null when no value is provided at construction.");
    }

    /// <summary>
    /// Unit test to verify that constructor initializes Content with null.
    /// </summary>
    [TestMethod]
    public void Constructor_NoParameters_InitializesContentWithNull()
    {
        // Arrange (Given)

        // Act (When)
        var instance = new NetHttpRequest();

        // Assert (Then)
        Assert.IsNull(
            instance.Content,
            "Content should be null when no value is provided at construction.");
    }

    /// <summary>
    /// Unit test to verify that constructor initializes HttpEndPoint with null.
    /// </summary>
    [TestMethod]
    public void Constructor_NoParameters_InitializesHttpEndPointWithNull()
    {
        // Arrange (Given)

        // Act (When)
        var instance = new NetHttpRequest();

        // Assert (Then)
        Assert.IsNull(
            instance.HttpEndPoint,
            "HttpEndPoint should be null when no value is provided at construction.");
    }

    /// <summary>
    /// Unit test to verify that Method property can be set and retrieved.
    /// </summary>
    [TestMethod]
    public void Method_SetToPost_ReturnsPost()
    {
        // Arrange (Given)
        var instance = new NetHttpRequest();

        // Act (When)
        instance.Method = HttpMethod.Post;

        // Assert (Then)
        Assert.AreEqual(
            HttpMethod.Post,
            instance.Method,
            "Method should return the value that was set.");
    }

    /// <summary>
    /// Unit test to verify that HttpEndPoint property can be set and retrieved.
    /// </summary>
    [TestMethod]
    public void HttpEndPoint_SetValidUri_ReturnsSetValue()
    {
        // Arrange (Given)
        var instance = new NetHttpRequest();
        var expectedUri = new Uri("https://api.example.com/data");

        // Act (When)
        instance.HttpEndPoint = expectedUri;

        // Assert (Then)
        Assert.AreEqual(
            expectedUri,
            instance.HttpEndPoint,
            "HttpEndPoint should return the URI that was set.");
    }

    /// <summary>
    /// Unit test to verify that EnableCompression property can be set to false.
    /// </summary>
    [TestMethod]
    public void EnableCompression_SetToFalse_ReturnsFalse()
    {
        // Arrange (Given)
        var instance = new NetHttpRequest();

        // Act (When)
        instance.EnableCompression = false;

        // Assert (Then)
        Assert.IsFalse(
            instance.EnableCompression,
            "EnableCompression should return false after being set to false.");
    }

    /// <summary>
    /// Unit test to verify that Authentication property can be set and retrieved.
    /// </summary>
    [TestMethod]
    public void Authentication_SetValidValue_ReturnsSetValue()
    {
        // Arrange (Given)
        var instance = new NetHttpRequest();
        var expectedAuth = new NetHttpAuthentication
        {
            AuthenticationType = NetHttpAuthenticationType.Bearer,
            Value = "token123",
        };

        // Act (When)
        instance.Authentication = expectedAuth;

        // Assert (Then)
        Assert.AreSame(
            expectedAuth,
            instance.Authentication,
            "Authentication should return the value that was set.");
    }

    /// <summary>
    /// Unit test to verify that TimeoutInSecondsPerAttempt property can be set and retrieved.
    /// </summary>
    [TestMethod]
    public void TimeoutInSecondsPerAttempt_SetValidValue_ReturnsSetValue()
    {
        // Arrange (Given)
        var instance = new NetHttpRequest();
        int expectedTimeout = 30;

        // Act (When)
        instance.TimeoutInSecondsPerAttempt = expectedTimeout;

        // Assert (Then)
        Assert.AreEqual(
            expectedTimeout,
            instance.TimeoutInSecondsPerAttempt,
            "TimeoutInSecondsPerAttempt should return the value that was set.");
    }

    #endregion Public Methods
}