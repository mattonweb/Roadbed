namespace Roadbed.Test.Unit.Net;

using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Net;

/// <summary>
/// Contains unit tests for verifying the behavior of the
/// <see cref="NetHttpResponse{T}"/> record.
/// </summary>
[TestClass]
public class NetHttpResponseTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that constructor sets HttpStatusCode.
    /// </summary>
    [TestMethod]
    public void Constructor_ValidParameters_SetsHttpStatusCode()
    {
        // Arrange (Given)
        int expectedStatusCode = 200;

        // Act (When)
        var instance = new NetHttpResponse<string>(
            expectedStatusCode, "OK", true, "data", string.Empty);

        // Assert (Then)
        Assert.AreEqual(
            expectedStatusCode,
            instance.HttpStatusCode,
            "HttpStatusCode should be set from the constructor parameter.");
    }

    /// <summary>
    /// Unit test to verify that constructor sets HttpStatusCodeDescription.
    /// </summary>
    [TestMethod]
    public void Constructor_ValidParameters_SetsHttpStatusCodeDescription()
    {
        // Arrange (Given)
        string expectedDescription = "OK";

        // Act (When)
        var instance = new NetHttpResponse<string>(
            200, expectedDescription, true, "data", string.Empty);

        // Assert (Then)
        Assert.AreEqual(
            expectedDescription,
            instance.HttpStatusCodeDescription,
            "HttpStatusCodeDescription should be set from the constructor parameter.");
    }

    /// <summary>
    /// Unit test to verify that constructor sets IsSuccessStatusCode.
    /// </summary>
    [TestMethod]
    public void Constructor_IsSuccessTrue_SetsIsSuccessStatusCodeTrue()
    {
        // Arrange (Given)

        // Act (When)
        var instance = new NetHttpResponse<string>(
            200, "OK", true, "data", string.Empty);

        // Assert (Then)
        Assert.IsTrue(
            instance.IsSuccessStatusCode,
            "IsSuccessStatusCode should be true when constructor receives true.");
    }

    /// <summary>
    /// Unit test to verify that constructor sets IsSuccessStatusCode to false.
    /// </summary>
    [TestMethod]
    public void Constructor_IsSuccessFalse_SetsIsSuccessStatusCodeFalse()
    {
        // Arrange (Given)

        // Act (When)
        var instance = new NetHttpResponse<string>(
            500, "Error", false, default!, "An error occurred.");

        // Assert (Then)
        Assert.IsFalse(
            instance.IsSuccessStatusCode,
            "IsSuccessStatusCode should be false when constructor receives false.");
    }

    /// <summary>
    /// Unit test to verify that constructor sets Data.
    /// </summary>
    [TestMethod]
    public void Constructor_ValidParameters_SetsData()
    {
        // Arrange (Given)
        string expectedData = "response body";

        // Act (When)
        var instance = new NetHttpResponse<string>(
            200, "OK", true, expectedData, string.Empty);

        // Assert (Then)
        Assert.AreEqual(
            expectedData,
            instance.Data,
            "Data should be set from the constructor parameter.");
    }

    /// <summary>
    /// Unit test to verify that constructor adds non-empty error to Errors list.
    /// </summary>
    [TestMethod]
    public void Constructor_WithErrorMessage_AddsToErrorsList()
    {
        // Arrange (Given)
        string expectedError = "Something went wrong.";

        // Act (When)
        var instance = new NetHttpResponse<string>(
            500, "Error", false, default!, expectedError);

        // Assert (Then)
        Assert.HasCount(
            1,
            instance.Errors,
            "Errors should contain exactly one entry when an error message is provided.");
        CollectionAssert.Contains(
            instance.Errors,
            expectedError,
            "Errors should contain the error message passed to the constructor.");
    }

    /// <summary>
    /// Unit test to verify that constructor does not add empty error to Errors list.
    /// </summary>
    [TestMethod]
    public void Constructor_WithEmptyError_DoesNotAddToErrorsList()
    {
        // Arrange (Given)

        // Act (When)
        var instance = new NetHttpResponse<string>(
            200, "OK", true, "data", string.Empty);

        // Assert (Then)
        Assert.HasCount(
            0,
            instance.Errors,
            "Errors should be empty when an empty error string is provided.");
    }

    /// <summary>
    /// Unit test to verify that constructor accepts null status description.
    /// </summary>
    [TestMethod]
    public void Constructor_NullStatusDescription_SetsNull()
    {
        // Arrange (Given)

        // Act (When)
        var instance = new NetHttpResponse<string>(
            200, null, true, "data", string.Empty);

        // Assert (Then)
        Assert.IsNull(
            instance.HttpStatusCodeDescription,
            "HttpStatusCodeDescription should be null when null is passed to the constructor.");
    }

    /// <summary>
    /// Unit test to verify that Success factory sets IsSuccessStatusCode to true.
    /// </summary>
    [TestMethod]
    public void Success_Called_SetsIsSuccessStatusCodeTrue()
    {
        // Arrange (Given)

        // Act (When)
        NetHttpResponse<string> instance =
            NetHttpResponse<string>.Success(200, "OK", "data");

        // Assert (Then)
        Assert.IsTrue(
            instance.IsSuccessStatusCode,
            "Success factory should set IsSuccessStatusCode to true.");
    }

    /// <summary>
    /// Unit test to verify that Success factory sets HttpStatusCode.
    /// </summary>
    [TestMethod]
    public void Success_Called_SetsHttpStatusCode()
    {
        // Arrange (Given)
        int expectedStatusCode = 201;

        // Act (When)
        NetHttpResponse<string> instance =
            NetHttpResponse<string>.Success(expectedStatusCode, "Created", "data");

        // Assert (Then)
        Assert.AreEqual(
            expectedStatusCode,
            instance.HttpStatusCode,
            "Success factory should set HttpStatusCode from the parameter.");
    }

    /// <summary>
    /// Unit test to verify that Success factory sets Data.
    /// </summary>
    [TestMethod]
    public void Success_Called_SetsData()
    {
        // Arrange (Given)
        string expectedData = "response payload";

        // Act (When)
        NetHttpResponse<string> instance =
            NetHttpResponse<string>.Success(200, "OK", expectedData);

        // Assert (Then)
        Assert.AreEqual(
            expectedData,
            instance.Data,
            "Success factory should set Data from the parameter.");
    }

    /// <summary>
    /// Unit test to verify that Success factory initializes Errors as empty.
    /// </summary>
    [TestMethod]
    public void Success_Called_InitializesErrorsAsEmpty()
    {
        // Arrange (Given)

        // Act (When)
        NetHttpResponse<string> instance =
            NetHttpResponse<string>.Success(200, "OK", "data");

        // Assert (Then)
        Assert.HasCount(
            0,
            instance.Errors,
            "Success factory should initialize Errors as an empty list.");
    }

    /// <summary>
    /// Unit test to verify that Failure factory sets IsSuccessStatusCode to false.
    /// </summary>
    [TestMethod]
    public void Failure_Called_SetsIsSuccessStatusCodeFalse()
    {
        // Arrange (Given)

        // Act (When)
        NetHttpResponse<string> instance =
            NetHttpResponse<string>.Failure(500, "Error", "Something failed.");

        // Assert (Then)
        Assert.IsFalse(
            instance.IsSuccessStatusCode,
            "Failure factory should set IsSuccessStatusCode to false.");
    }

    /// <summary>
    /// Unit test to verify that Failure factory sets HttpStatusCode.
    /// </summary>
    [TestMethod]
    public void Failure_Called_SetsHttpStatusCode()
    {
        // Arrange (Given)
        int expectedStatusCode = 404;

        // Act (When)
        NetHttpResponse<string> instance =
            NetHttpResponse<string>.Failure(expectedStatusCode, "Not Found", "Resource missing.");

        // Assert (Then)
        Assert.AreEqual(
            expectedStatusCode,
            instance.HttpStatusCode,
            "Failure factory should set HttpStatusCode from the parameter.");
    }

    /// <summary>
    /// Unit test to verify that Failure factory adds error to Errors list.
    /// </summary>
    [TestMethod]
    public void Failure_Called_AddsErrorToErrorsList()
    {
        // Arrange (Given)
        string expectedError = "Request timed out.";

        // Act (When)
        NetHttpResponse<string> instance =
            NetHttpResponse<string>.Failure(408, "Timeout", expectedError);

        // Assert (Then)
        Assert.HasCount(
            1,
            instance.Errors,
            "Failure factory should add exactly one error to the Errors list.");
        CollectionAssert.Contains(
            instance.Errors,
            expectedError,
            "Failure factory should add the error message to the Errors list.");
    }

    /// <summary>
    /// Unit test to verify that Failure factory sets Data to default.
    /// </summary>
    [TestMethod]
    public void Failure_WithStringType_SetsDataToDefault()
    {
        // Arrange (Given)

        // Act (When)
        NetHttpResponse<string> instance =
            NetHttpResponse<string>.Failure(500, "Error", "Failed.");

        // Assert (Then)
        Assert.IsNull(
            instance.Data,
            "Failure factory should set Data to default (null for reference types).");
    }

    /// <summary>
    /// Unit test to verify that Success factory works with a complex type.
    /// </summary>
    [TestMethod]
    public void Success_WithComplexType_SetsData()
    {
        // Arrange (Given)
        var expectedData = new List<int> { 1, 2, 3 };

        // Act (When)
        NetHttpResponse<List<int>> instance =
            NetHttpResponse<List<int>>.Success(200, "OK", expectedData);

        // Assert (Then)
        Assert.AreSame(
            expectedData,
            instance.Data,
            "Success factory should set Data to the complex type instance.");
        Assert.IsTrue(
            instance.IsSuccessStatusCode,
            "Success factory should set IsSuccessStatusCode to true for complex types.");
    }

    /// <summary>
    /// Unit test to verify that Failure factory works with a complex type.
    /// </summary>
    [TestMethod]
    public void Failure_WithComplexType_SetsDataToDefault()
    {
        // Arrange (Given)

        // Act (When)
        NetHttpResponse<List<int>> instance =
            NetHttpResponse<List<int>>.Failure(500, "Error", "Failed.");

        // Assert (Then)
        Assert.IsNull(
            instance.Data,
            "Failure factory should set Data to default (null) for complex types.");
    }

    #endregion Public Methods
}