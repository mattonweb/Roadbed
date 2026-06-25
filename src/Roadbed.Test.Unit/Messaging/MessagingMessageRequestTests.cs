namespace Roadbed.Test.Unit.Messaging;

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Common;
using Roadbed.Messaging;

/// <summary>
/// Unit tests for the MessagingMessageRequest class.
/// </summary>
[TestClass]
public class MessagingMessageRequestTests
{
    #region Public Methods

    /// <summary>
    /// Verifies that the constructor with all parameters accepts empty string for data.
    /// </summary>
    [TestMethod]
    public void Constructor_WithAllParameters_AcceptsEmptyStringData()
    {
        // Arrange
        var publisher = new MessagingPublisher(CommonBusinessKey.FromString("TestPublisher", true));
        string typeCodename = "test.message.request";
        string identifier = "test-identifier-123";
        string data = string.Empty;

        // Act
        var request = new MessagingMessageRequest<string>(publisher, typeCodename, identifier, data);

        // Assert
        Assert.AreEqual(string.Empty, request.Data);
    }

    /// <summary>
    /// Verifies that the constructor with all parameters accepts empty string for identifier.
    /// </summary>
    [TestMethod]
    public void Constructor_WithAllParameters_AcceptsEmptyStringIdentifier()
    {
        // Arrange
        var publisher = new MessagingPublisher(CommonBusinessKey.FromString("TestPublisher", true));
        string typeCodename = "test.message.request";
        string identifier = string.Empty;
        string data = "Test data payload";

        // Act
        var request = new MessagingMessageRequest<string>(publisher, typeCodename, identifier, data);

        // Assert
        Assert.AreEqual(string.Empty, request.Identifier);
    }

    /// <summary>
    /// Verifies that the constructor with all parameters accepts empty string for typeCodename.
    /// </summary>
    [TestMethod]
    public void Constructor_WithAllParameters_AcceptsEmptyStringTypeCodename()
    {
        // Arrange
        var publisher = new MessagingPublisher(CommonBusinessKey.FromString("TestPublisher", true));
        string typeCodename = string.Empty;
        string identifier = "test-identifier-123";
        string data = "Test data payload";

        // Act
        var request = new MessagingMessageRequest<string>(publisher, typeCodename, identifier, data);

        // Assert
        Assert.AreEqual(string.Empty, request.MessageTypeCodename);
    }

    /// <summary>
    /// Verifies that the constructor with all parameters accepts null for data.
    /// </summary>
    [TestMethod]
    public void Constructor_WithAllParameters_AcceptsNullData()
    {
        // Arrange
        var publisher = new MessagingPublisher(CommonBusinessKey.FromString("TestPublisher", true));
        string typeCodename = "test.message.request";
        string identifier = "test-identifier-123";
        string? data = null;

        // Act
        var request = new MessagingMessageRequest<string>(publisher, typeCodename, identifier, data!);

        // Assert
        Assert.IsNull(request.Data);
        Assert.AreEqual(identifier, request.Identifier);
    }

    /// <summary>
    /// Verifies that the constructor with all parameters accepts null for identifier.
    /// </summary>
    [TestMethod]
    public void Constructor_WithAllParameters_AcceptsNullIdentifier()
    {
        // Arrange
        var publisher = new MessagingPublisher(CommonBusinessKey.FromString("TestPublisher", true));
        string typeCodename = "test.message.request";
        string? identifier = null;
        string data = "Test data payload";

        // Act
        var request = new MessagingMessageRequest<string>(publisher, typeCodename, identifier!, data);

        // Assert
        Assert.IsNull(request.Identifier);
        Assert.AreEqual(data, request.Data);
    }

    /// <summary>
    /// Verifies that the constructor with all parameters accepts null for typeCodename.
    /// </summary>
    [TestMethod]
    public void Constructor_WithAllParameters_AcceptsNullTypeCodename()
    {
        // Arrange
        var publisher = new MessagingPublisher(CommonBusinessKey.FromString("TestPublisher", true));
        string? typeCodename = null;
        string identifier = "test-identifier-123";
        string data = "Test data payload";

        // Act
        var request = new MessagingMessageRequest<string>(publisher, typeCodename!, identifier, data);

        // Assert
        Assert.IsNull(request.MessageTypeCodename);
        Assert.AreEqual(identifier, request.Identifier);
        Assert.AreEqual(data, request.Data);
    }

    /// <summary>
    /// Verifies that the constructor with all parameters accepts special characters in parameters.
    /// </summary>
    [TestMethod]
    public void Constructor_WithAllParameters_AcceptsSpecialCharacters()
    {
        // Arrange
        var publisher = new MessagingPublisher(CommonBusinessKey.FromString("TestPublisher", true));
        string typeCodename = "test.message-request_v1.0";
        string identifier = "test-id_123!@#";
        string data = "Data with special chars: !@#$%";

        // Act
        var request = new MessagingMessageRequest<string>(publisher, typeCodename, identifier, data);

        // Assert
        Assert.AreEqual(typeCodename, request.MessageTypeCodename);
        Assert.AreEqual(identifier, request.Identifier);
        Assert.AreEqual(data, request.Data);
    }

    /// <summary>
    /// Verifies that the constructor with all parameters accepts a valid UUIDv7 as the identifier.
    /// </summary>
    [TestMethod]
    public void Constructor_WithAllParameters_AcceptsValidUuidV7Identifier()
    {
        // Arrange
        var publisher = new MessagingPublisher(CommonBusinessKey.FromString("TestPublisher", true));
        string typeCodename = "test.message.request";
        string identifier = Guid.CreateVersion7().ToString();
        string data = "Test data payload";

        // Act
        var request = new MessagingMessageRequest<string>(publisher, typeCodename, identifier, data);

        // Assert
        Assert.AreEqual(identifier, request.Identifier);
        Assert.IsTrue(Guid.TryParse(request.Identifier, out _));
    }

    /// <summary>
    /// Verifies that the constructor with all parameters sets all properties correctly.
    /// </summary>
    [TestMethod]
    public void Constructor_WithAllParameters_SetsAllProperties()
    {
        // Arrange
        var publisher = new MessagingPublisher(CommonBusinessKey.FromString("TestPublisher", true));
        string typeCodename = "test.message.request";
        string identifier = "test-identifier-123";
        string data = "Test data payload";

        // Act
        var request = new MessagingMessageRequest<string>(publisher, typeCodename, identifier, data);

        // Assert
        Assert.AreEqual(publisher, request.Publisher);
        Assert.AreEqual(typeCodename, request.MessageTypeCodename);
        Assert.AreEqual(identifier, request.Identifier);
        Assert.AreEqual(data, request.Data);
    }

    /// <summary>
    /// Verifies that the constructor with all parameters sets CreatedOn to current UTC time.
    /// </summary>
    [TestMethod]
    public void Constructor_WithAllParameters_SetsCreatedOnToUtcNow()
    {
        // Arrange
        var publisher = new MessagingPublisher(CommonBusinessKey.FromString("TestPublisher", true));
        string typeCodename = "test.message.request";
        string identifier = "test-identifier-123";
        string data = "Test data payload";
        var beforeCreation = DateTimeOffset.UtcNow;

        // Act
        var request = new MessagingMessageRequest<string>(publisher, typeCodename, identifier, data);
        var afterCreation = DateTimeOffset.UtcNow;

        // Assert
        Assert.IsTrue(request.CreatedOn >= beforeCreation);
        Assert.IsTrue(request.CreatedOn <= afterCreation);
    }

    /// <summary>
    /// Verifies that the constructor with all parameters sets SourceCreatedOn to current UTC time.
    /// </summary>
    [TestMethod]
    public void Constructor_WithAllParameters_SetsSourceCreatedOnToUtcNow()
    {
        // Arrange
        var publisher = new MessagingPublisher(CommonBusinessKey.FromString("TestPublisher", true));
        string typeCodename = "test.message.request";
        string identifier = "test-identifier-123";
        string data = "Test data payload";
        var beforeCreation = DateTimeOffset.UtcNow;

        // Act
        var request = new MessagingMessageRequest<string>(publisher, typeCodename, identifier, data);
        var afterCreation = DateTimeOffset.UtcNow;

        // Assert
        Assert.IsTrue(request.SourceCreatedOn >= beforeCreation);
        Assert.IsTrue(request.SourceCreatedOn <= afterCreation);
    }

    /// <summary>
    /// Verifies that the constructor with all parameters works with complex generic types.
    /// </summary>
    [TestMethod]
    public void Constructor_WithAllParameters_WorksWithComplexGenericType()
    {
        // Arrange
        var publisher = new MessagingPublisher(CommonBusinessKey.FromString("TestPublisher", true));
        string typeCodename = "test.message.request";
        string identifier = "test-identifier-123";
        var data = new TestData { Name = "Test", Value = 42 };

        // Act
        var request = new MessagingMessageRequest<TestData>(publisher, typeCodename, identifier, data);

        // Assert
        Assert.AreEqual(publisher, request.Publisher);
        Assert.AreEqual(typeCodename, request.MessageTypeCodename);
        Assert.AreEqual(identifier, request.Identifier);
        Assert.AreEqual(data, request.Data);
        Assert.AreEqual("Test", request.Data!.Name);
        Assert.AreEqual(42, request.Data.Value);
    }

    /// <summary>
    /// Verifies that the constructor with publisher parameter generates a non-empty Identifier.
    /// </summary>
    [TestMethod]
    public void Constructor_WithPublisher_GeneratesNonEmptyIdentifier()
    {
        // Arrange
        var publisher = new MessagingPublisher(CommonBusinessKey.FromString("TestPublisher", true));

        // Act
        var request = new MessagingMessageRequest<string>(publisher);

        // Assert
        Assert.IsFalse(string.IsNullOrWhiteSpace(request.Identifier));
    }

    /// <summary>
    /// Verifies that the constructor with publisher parameter generates a non-null Identifier.
    /// </summary>
    [TestMethod]
    public void Constructor_WithPublisher_GeneratesNonNullIdentifier()
    {
        // Arrange
        var publisher = new MessagingPublisher(CommonBusinessKey.FromString("TestPublisher", true));

        // Act
        var request = new MessagingMessageRequest<string>(publisher);

        // Assert
        Assert.IsNotNull(request.Identifier);
    }

    /// <summary>
    /// Verifies that the constructor with publisher parameter generates unique Identifiers for multiple instances.
    /// </summary>
    [TestMethod]
    public void Constructor_WithPublisher_GeneratesUniqueIdentifiers()
    {
        // Arrange
        var publisher = new MessagingPublisher(CommonBusinessKey.FromString("TestPublisher", true));

        // Act
        var request1 = new MessagingMessageRequest<string>(publisher);
        var request2 = new MessagingMessageRequest<string>(publisher);

        // Assert
        Assert.AreNotEqual(request1.Identifier, request2.Identifier);
    }

    /// <summary>
    /// Verifies that the constructor with publisher parameter generates a valid UUIDv7 format for Identifier.
    /// </summary>
    [TestMethod]
    public void Constructor_WithPublisher_GeneratesValidUuidV7Identifier()
    {
        // Arrange
        var publisher = new MessagingPublisher(CommonBusinessKey.FromString("TestPublisher", true));

        // Act
        var request = new MessagingMessageRequest<string>(publisher);

        // Assert
        Assert.IsTrue(Guid.TryParse(request.Identifier, out _));
    }

    /// <summary>
    /// Verifies that the constructor with publisher parameter leaves Data property null.
    /// </summary>
    [TestMethod]
    public void Constructor_WithPublisher_LeavesDataNull()
    {
        // Arrange
        var publisher = new MessagingPublisher(CommonBusinessKey.FromString("TestPublisher", true));

        // Act
        var request = new MessagingMessageRequest<string>(publisher);

        // Assert
        Assert.IsNull(request.Data);
    }

    /// <summary>
    /// Verifies that the constructor with publisher parameter leaves MessageTypeCodename property null.
    /// </summary>
    [TestMethod]
    public void Constructor_WithPublisher_LeavesMessageTypeCodenameNull()
    {
        // Arrange
        var publisher = new MessagingPublisher(CommonBusinessKey.FromString("TestPublisher", true));

        // Act
        var request = new MessagingMessageRequest<string>(publisher);

        // Assert
        Assert.IsNull(request.MessageTypeCodename);
    }

    /// <summary>
    /// Verifies that the constructor with publisher parameter sets CreatedOn to a non-null value.
    /// </summary>
    [TestMethod]
    public void Constructor_WithPublisher_SetsCreatedOnToNonNull()
    {
        // Arrange
        var publisher = new MessagingPublisher(CommonBusinessKey.FromString("TestPublisher", true));

        // Act
        var request = new MessagingMessageRequest<string>(publisher);

        // Assert
        Assert.IsNotNull(request.CreatedOn);
    }

    /// <summary>
    /// Verifies that the constructor with publisher parameter sets CreatedOn to current UTC time.
    /// </summary>
    [TestMethod]
    public void Constructor_WithPublisher_SetsCreatedOnToUtcNow()
    {
        // Arrange
        var publisher = new MessagingPublisher(CommonBusinessKey.FromString("TestPublisher", true));
        var beforeCreation = DateTimeOffset.UtcNow;

        // Act
        var request = new MessagingMessageRequest<string>(publisher);
        var afterCreation = DateTimeOffset.UtcNow;

        // Assert
        Assert.IsTrue(request.CreatedOn >= beforeCreation);
        Assert.IsTrue(request.CreatedOn <= afterCreation);
    }

    /// <summary>
    /// Verifies that the constructor with publisher parameter sets the Publisher property correctly.
    /// </summary>
    [TestMethod]
    public void Constructor_WithPublisher_SetsPublisherProperty()
    {
        // Arrange
        var publisher = new MessagingPublisher(CommonBusinessKey.FromString("TestPublisher", true));

        // Act
        var request = new MessagingMessageRequest<string>(publisher);

        // Assert
        Assert.AreEqual(publisher, request.Publisher);
    }

    /// <summary>
    /// Verifies that the constructor with publisher parameter sets SourceCreatedOn to a non-null value.
    /// </summary>
    [TestMethod]
    public void Constructor_WithPublisher_SetsSourceCreatedOnToNonNull()
    {
        // Arrange
        var publisher = new MessagingPublisher(CommonBusinessKey.FromString("TestPublisher", true));

        // Act
        var request = new MessagingMessageRequest<string>(publisher);

        // Assert
        Assert.IsNotNull(request.SourceCreatedOn);
    }

    /// <summary>
    /// Verifies that the constructor with publisher parameter sets SourceCreatedOn to current UTC time.
    /// </summary>
    [TestMethod]
    public void Constructor_WithPublisher_SetsSourceCreatedOnToUtcNow()
    {
        // Arrange
        var publisher = new MessagingPublisher(CommonBusinessKey.FromString("TestPublisher", true));
        var beforeCreation = DateTimeOffset.UtcNow;

        // Act
        var request = new MessagingMessageRequest<string>(publisher);
        var afterCreation = DateTimeOffset.UtcNow;

        // Assert
        Assert.IsTrue(request.SourceCreatedOn >= beforeCreation);
        Assert.IsTrue(request.SourceCreatedOn <= afterCreation);
    }

    /// <summary>
    /// Verifies that the constructor with publisher parameter works with complex generic types.
    /// </summary>
    [TestMethod]
    public void Constructor_WithPublisher_WorksWithComplexGenericType()
    {
        // Arrange
        var publisher = new MessagingPublisher(CommonBusinessKey.FromString("TestPublisher", true));

        // Act
        var request = new MessagingMessageRequest<TestData>(publisher);

        // Assert
        Assert.IsNotNull(request);
        Assert.AreEqual(publisher, request.Publisher);
        Assert.IsNotNull(request.Identifier);
    }

    /// <summary>
    /// Verifies that the constructor with publisher and typeCodename parameters accepts codenames with special characters.
    /// </summary>
    [TestMethod]
    public void Constructor_WithPublisherAndTypeCodename_AcceptsCodenameWithSpecialCharacters()
    {
        // Arrange
        var publisher = new MessagingPublisher(CommonBusinessKey.FromString("TestPublisher", true));
        string typeCodename = "test.message-request_v1";

        // Act
        var request = new MessagingMessageRequest<string>(publisher, typeCodename);

        // Assert
        Assert.AreEqual(typeCodename, request.MessageTypeCodename);
    }

    /// <summary>
    /// Verifies that the constructor with publisher and typeCodename parameters accepts empty string for typeCodename.
    /// </summary>
    [TestMethod]
    public void Constructor_WithPublisherAndTypeCodename_AcceptsEmptyStringTypeCodename()
    {
        // Arrange
        var publisher = new MessagingPublisher(CommonBusinessKey.FromString("TestPublisher", true));
        string typeCodename = string.Empty;

        // Act
        var request = new MessagingMessageRequest<string>(publisher, typeCodename);

        // Assert
        Assert.AreEqual(string.Empty, request.MessageTypeCodename);
        Assert.IsNotNull(request.Identifier);
    }

    /// <summary>
    /// Verifies that the constructor with publisher and typeCodename parameters accepts null for typeCodename.
    /// </summary>
    [TestMethod]
    public void Constructor_WithPublisherAndTypeCodename_AcceptsNullTypeCodename()
    {
        // Arrange
        var publisher = new MessagingPublisher(CommonBusinessKey.FromString("TestPublisher", true));
        string? typeCodename = null;

        // Act
        var request = new MessagingMessageRequest<string>(publisher, typeCodename!);

        // Assert
        Assert.IsNull(request.MessageTypeCodename);
        Assert.IsNotNull(request.Identifier);
    }

    /// <summary>
    /// Verifies that the constructor with publisher and typeCodename parameters generates unique Identifiers for multiple instances.
    /// </summary>
    [TestMethod]
    public void Constructor_WithPublisherAndTypeCodename_GeneratesUniqueIdentifiers()
    {
        // Arrange
        var publisher = new MessagingPublisher(CommonBusinessKey.FromString("TestPublisher", true));
        string typeCodename = "test.message.request";

        // Act
        var request1 = new MessagingMessageRequest<string>(publisher, typeCodename);
        var request2 = new MessagingMessageRequest<string>(publisher, typeCodename);

        // Assert
        Assert.AreNotEqual(request1.Identifier, request2.Identifier);
    }

    /// <summary>
    /// Verifies that the constructor with publisher and typeCodename parameters generates a valid UUIDv7 Identifier.
    /// </summary>
    [TestMethod]
    public void Constructor_WithPublisherAndTypeCodename_GeneratesValidUuidV7Identifier()
    {
        // Arrange
        var publisher = new MessagingPublisher(CommonBusinessKey.FromString("TestPublisher", true));
        string typeCodename = "test.message.request";

        // Act
        var request = new MessagingMessageRequest<string>(publisher, typeCodename);

        // Assert
        Assert.IsTrue(Guid.TryParse(request.Identifier, out _));
    }

    /// <summary>
    /// Verifies that the constructor with publisher and typeCodename parameters leaves Data property null.
    /// </summary>
    [TestMethod]
    public void Constructor_WithPublisherAndTypeCodename_LeavesDataNull()
    {
        // Arrange
        var publisher = new MessagingPublisher(CommonBusinessKey.FromString("TestPublisher", true));
        string typeCodename = "test.message.request";

        // Act
        var request = new MessagingMessageRequest<string>(publisher, typeCodename);

        // Assert
        Assert.IsNull(request.Data);
    }

    /// <summary>
    /// Verifies that the constructor with publisher and typeCodename parameters sets both properties correctly.
    /// </summary>
    [TestMethod]
    public void Constructor_WithPublisherAndTypeCodename_SetsBothProperties()
    {
        // Arrange
        var publisher = new MessagingPublisher(CommonBusinessKey.FromString("TestPublisher", true));
        string typeCodename = "test.message.request";

        // Act
        var request = new MessagingMessageRequest<string>(publisher, typeCodename);

        // Assert
        Assert.AreEqual(publisher, request.Publisher);
        Assert.AreEqual(typeCodename, request.MessageTypeCodename);
    }

    /// <summary>
    /// Verifies that the constructor with publisher and typeCodename parameters sets CreatedOn to current UTC time.
    /// </summary>
    [TestMethod]
    public void Constructor_WithPublisherAndTypeCodename_SetsCreatedOnToUtcNow()
    {
        // Arrange
        var publisher = new MessagingPublisher(CommonBusinessKey.FromString("TestPublisher", true));
        string typeCodename = "test.message.request";
        var beforeCreation = DateTimeOffset.UtcNow;

        // Act
        var request = new MessagingMessageRequest<string>(publisher, typeCodename);
        var afterCreation = DateTimeOffset.UtcNow;

        // Assert
        Assert.IsTrue(request.CreatedOn >= beforeCreation);
        Assert.IsTrue(request.CreatedOn <= afterCreation);
    }

    /// <summary>
    /// Verifies that the constructor with publisher and typeCodename parameters sets SourceCreatedOn to current UTC time.
    /// </summary>
    [TestMethod]
    public void Constructor_WithPublisherAndTypeCodename_SetsSourceCreatedOnToUtcNow()
    {
        // Arrange
        var publisher = new MessagingPublisher(CommonBusinessKey.FromString("TestPublisher", true));
        string typeCodename = "test.message.request";
        var beforeCreation = DateTimeOffset.UtcNow;

        // Act
        var request = new MessagingMessageRequest<string>(publisher, typeCodename);
        var afterCreation = DateTimeOffset.UtcNow;

        // Assert
        Assert.IsTrue(request.SourceCreatedOn >= beforeCreation);
        Assert.IsTrue(request.SourceCreatedOn <= afterCreation);
    }

    /// <summary>
    /// Verifies that the Data property can be updated after construction.
    /// </summary>
    [TestMethod]
    public void DataProperty_SetValue_UpdatesProperty()
    {
        // Arrange
        var publisher = new MessagingPublisher(CommonBusinessKey.FromString("TestPublisher", true));
        var request = new MessagingMessageRequest<string>(publisher);
        string newData = "Updated data";

        // Act
        request.Data = newData;

        // Assert
        Assert.AreEqual(newData, request.Data);
    }

    /// <summary>
    /// Verifies that requests support different generic type parameters.
    /// </summary>
    [TestMethod]
    public void GenericTypeParameter_DifferentTypes_WorkCorrectly()
    {
        // Arrange
        var publisher = new MessagingPublisher(CommonBusinessKey.FromString("TestPublisher", true));

        // Act
        var stringRequest = new MessagingMessageRequest<string>(publisher, "string.type", "id1", "string data");
        var intRequest = new MessagingMessageRequest<int>(publisher, "int.type", "id2", 42);
        var complexRequest = new MessagingMessageRequest<TestData>(publisher, "complex.type", "id3", new TestData { Name = "Test", Value = 100 });

        // Assert
        Assert.AreEqual("string data", stringRequest.Data);
        Assert.AreEqual(42, intRequest.Data);
        Assert.AreEqual("Test", complexRequest.Data!.Name);
        Assert.AreEqual(100, complexRequest.Data.Value);
    }

    /// <summary>
    /// Verifies that the MessageTypeCodename property can be updated after construction.
    /// </summary>
    [TestMethod]
    public void MessageTypeCodenameProperty_SetValue_UpdatesProperty()
    {
        // Arrange
        var publisher = new MessagingPublisher(CommonBusinessKey.FromString("TestPublisher", true));
        var request = new MessagingMessageRequest<string>(publisher);
        string newTypeCodename = "updated.message.type";

        // Act
        request.MessageTypeCodename = newTypeCodename;

        // Assert
        Assert.AreEqual(newTypeCodename, request.MessageTypeCodename);
    }

    /// <summary>
    /// Verifies that multiple requests created with the same publisher have unique identifiers.
    /// </summary>
    [TestMethod]
    public void MultipleRequests_SamePublisher_HaveUniqueIdentifiers()
    {
        // Arrange
        var publisher = new MessagingPublisher(CommonBusinessKey.FromString("TestPublisher", true));

        // Act
        var request1 = new MessagingMessageRequest<string>(publisher, "test.type");
        var request2 = new MessagingMessageRequest<string>(publisher, "test.type");
        var request3 = new MessagingMessageRequest<string>(publisher, "test.type");

        // Assert
        Assert.AreNotEqual(request1.Identifier, request2.Identifier);
        Assert.AreNotEqual(request2.Identifier, request3.Identifier);
        Assert.AreNotEqual(request1.Identifier, request3.Identifier);
    }

    /// <summary>
    /// Verifies that properties can be set to null where nullable.
    /// </summary>
    [TestMethod]
    public void NullableProperties_SetToNull_AcceptNull()
    {
        // Arrange
        var publisher = new MessagingPublisher(CommonBusinessKey.FromString("TestPublisher", true));
        var request = new MessagingMessageRequest<string>(publisher, "test.type", "test-id", "test-data");

        // Act
        request.Data = null;
        request.MessageTypeCodename = null;
        request.SourceCreatedOn = null;

        // Assert
        Assert.IsNull(request.Data);
        Assert.IsNull(request.MessageTypeCodename);
        Assert.IsNull(request.SourceCreatedOn);
    }

    /// <summary>
    /// Verifies that the Publisher property can be updated after construction.
    /// </summary>
    [TestMethod]
    public void PublisherProperty_SetValue_UpdatesProperty()
    {
        // Arrange
        var initialPublisher = new MessagingPublisher(CommonBusinessKey.FromString("InitialPublisher", true));
        var request = new MessagingMessageRequest<string>(initialPublisher);
        var newPublisher = new MessagingPublisher(CommonBusinessKey.FromString("NewPublisher", true));

        // Act
        request.Publisher = newPublisher;

        // Assert
        Assert.AreEqual(newPublisher, request.Publisher);
        Assert.AreNotEqual(initialPublisher, request.Publisher);
    }

    /// <summary>
    /// Verifies that the SourceCreatedOn property can be updated after construction.
    /// </summary>
    [TestMethod]
    public void SourceCreatedOnProperty_SetValue_UpdatesProperty()
    {
        // Arrange
        var publisher = new MessagingPublisher(CommonBusinessKey.FromString("TestPublisher", true));
        var request = new MessagingMessageRequest<string>(publisher);
        var newSourceCreatedOn = DateTimeOffset.UtcNow.AddDays(-1);

        // Act
        request.SourceCreatedOn = newSourceCreatedOn;

        // Assert
        Assert.AreEqual(newSourceCreatedOn, request.SourceCreatedOn);
    }

    #endregion Public Methods

    #region Private Classes

    /// <summary>
    /// Complex test data class for testing with custom types.
    /// </summary>
    private class TestData
    {
        #region Public Properties

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        public int Value { get; set; }

        #endregion Public Properties
    }

    #endregion Private Classes
}