namespace Roadbed.Test.Unit.Messaging;

using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Common;
using Roadbed.Messaging;

/// <summary>
/// Unit tests for the MessagingPublisher class.
/// </summary>
[TestClass]
public class MessagingPublisherTests
{
    #region Public Methods

    /// <summary>
    /// Verifies that the default constructor generates an Identifier with the expected UUIDv7 length of 36 characters.
    /// </summary>
    [TestMethod]
    public void Constructor_DefaultConstructor_GeneratesIdentifierWithCorrectLength()
    {
        // Act
        var publisher = new MessagingPublisher();

        // Assert
        Assert.AreEqual(36, publisher.Identifier.Length);
    }

    /// <summary>
    /// Verifies that the default constructor generates a non-empty Identifier.
    /// </summary>
    [TestMethod]
    public void Constructor_DefaultConstructor_GeneratesNonEmptyIdentifier()
    {
        // Act
        var publisher = new MessagingPublisher();

        // Assert
        Assert.IsFalse(string.IsNullOrWhiteSpace(publisher.Identifier));
    }

    /// <summary>
    /// Verifies that the default constructor generates a non-null Identifier.
    /// </summary>
    [TestMethod]
    public void Constructor_DefaultConstructor_GeneratesNonNullIdentifier()
    {
        // Act
        var publisher = new MessagingPublisher();

        // Assert
        Assert.IsNotNull(publisher.Identifier);
    }

    /// <summary>
    /// Verifies that the default constructor generates unique Identifiers for multiple instances.
    /// </summary>
    [TestMethod]
    public void Constructor_DefaultConstructor_GeneratesUniqueIdentifiers()
    {
        // Act
        var publisher1 = new MessagingPublisher();
        var publisher2 = new MessagingPublisher();

        // Assert
        Assert.AreNotEqual(publisher1.Identifier, publisher2.Identifier);
    }

    /// <summary>
    /// Verifies that the default constructor generates a valid UUIDv7 format for Identifier.
    /// </summary>
    [TestMethod]
    public void Constructor_DefaultConstructor_GeneratesValidUuidV7Identifier()
    {
        // Act
        var publisher = new MessagingPublisher();

        // Assert
        Assert.IsTrue(Guid.TryParse(publisher.Identifier, out _));
    }

    /// <summary>
    /// Verifies that the default constructor sets Name property to null.
    /// </summary>
    [TestMethod]
    public void Constructor_DefaultConstructor_SetsNameToNull()
    {
        // Act
        var publisher = new MessagingPublisher();

        // Assert
        Assert.IsNull(publisher.Name);
    }

    /// <summary>
    /// Verifies that the constructor with name and identifier parameters accepts a custom non-UUIDv7 identifier.
    /// </summary>
    [TestMethod]
    public void Constructor_WithNameAndIdentifier_AcceptsCustomIdentifier()
    {
        // Arrange
        string name = "TESTPUBLISHER";
        string identifier = "custom-guid-12345";

        // Act
        var publisher = new MessagingPublisher(CommonBusinessKey.FromString(name), identifier);

        // Assert
        Assert.AreEqual(name, publisher.Name!.Key);
        Assert.AreEqual(identifier, publisher.Identifier);
    }

    /// <summary>
    /// Verifies that the constructor with name and identifier parameters accepts null for the identifier.
    /// </summary>
    [TestMethod]
    public void Constructor_WithNameAndIdentifier_AcceptsNullIdentifier()
    {
        // Arrange
        string name = "TESTPUBLISHER";
        string? identifier = null;

        // Act
        var publisher = new MessagingPublisher(CommonBusinessKey.FromString(name), identifier!);

        // Assert
        Assert.AreEqual(name, publisher.Name!.Key);
        Assert.IsNull(publisher.Identifier);
    }

    /// <summary>
    /// Verifies that the constructor with name and identifier parameters accepts a valid UUIDv7 as the identifier.
    /// </summary>
    [TestMethod]
    public void Constructor_WithNameAndIdentifier_AcceptsValidUuidV7Identifier()
    {
        // Arrange
        string name = "TESTPUBLISHER";
        string identifier = Guid.CreateVersion7().ToString();

        // Act
        var publisher = new MessagingPublisher(CommonBusinessKey.FromString(name), identifier);

        // Assert
        Assert.AreEqual(name, publisher.Name!.Key);
        Assert.AreEqual(identifier, publisher.Identifier);
        Assert.IsTrue(Guid.TryParse(publisher.Identifier, out _));
    }

    /// <summary>
    /// Verifies that the constructor with name and identifier parameters sets both properties correctly.
    /// </summary>
    [TestMethod]
    public void Constructor_WithNameAndIdentifier_SetsBothProperties()
    {
        // Arrange
        string expectedName = "TESTPUBLISHER";
        string expectedIdentifier = "test-identifier-123";

        // Act
        var publisher = new MessagingPublisher(CommonBusinessKey.FromString(expectedName), expectedIdentifier);

        // Assert
        Assert.AreEqual(expectedName, publisher.Name!.Key);
        Assert.AreEqual(expectedIdentifier, publisher.Identifier);
    }

    /// <summary>
    /// Verifies that the constructor with name parameter generates unique Identifiers for multiple instances.
    /// </summary>
    [TestMethod]
    public void Constructor_WithNameParameter_GeneratesUniqueIdentifiers()
    {
        // Arrange
        string name = "TestPublisher";

        // Act
        var publisher1 = new MessagingPublisher(CommonBusinessKey.FromString(name, true));
        var publisher2 = new MessagingPublisher(CommonBusinessKey.FromString(name, true));

        // Assert
        Assert.AreNotEqual(publisher1.Identifier, publisher2.Identifier);
    }

    /// <summary>
    /// Verifies that the constructor with name parameter sets the Name property correctly.
    /// </summary>
    [TestMethod]
    public void Constructor_WithNameParameter_SetsNameProperty()
    {
        // Arrange
        string expectedName = "TESTPUBLISHER";

        // Act
        var publisher = new MessagingPublisher(CommonBusinessKey.FromString(expectedName));

        // Assert
        Assert.AreEqual(expectedName, publisher.Name!.Key);
    }

    /// <summary>
    /// Verifies that the Identifier property can be set to null.
    /// </summary>
    [TestMethod]
    public void IdentifierProperty_SetToNull_AcceptsNull()
    {
        // Arrange
        var publisher = new MessagingPublisher();

        // Act
        publisher.Identifier = null!;

        // Assert
        Assert.IsNull(publisher.Identifier);
    }

    /// <summary>
    /// Verifies that the Identifier property can be set to a new value.
    /// </summary>
    [TestMethod]
    public void IdentifierProperty_SetValue_UpdatesProperty()
    {
        // Arrange
        var publisher = new MessagingPublisher();
        string newIdentifier = "new-identifier-123";

        // Act
        publisher.Identifier = newIdentifier;

        // Assert
        Assert.AreEqual(newIdentifier, publisher.Identifier);
    }

    /// <summary>
    /// Verifies that the Identifier property can be updated multiple times.
    /// </summary>
    [TestMethod]
    public void IdentifierProperty_UpdateMultipleTimes_RetainsLatestValue()
    {
        // Arrange
        var publisher = new MessagingPublisher();
        string firstId = publisher.Identifier;

        // Act
        publisher.Identifier = "SecondId";
        publisher.Identifier = "ThirdId";

        // Assert
        Assert.AreEqual("ThirdId", publisher.Identifier);
        Assert.AreNotEqual(firstId, publisher.Identifier);
    }

    /// <summary>
    /// Verifies that the Name property can be set to a new value.
    /// </summary>
    [TestMethod]
    public void NameProperty_SetValue_UpdatesProperty()
    {
        // Arrange
        var publisher = new MessagingPublisher();
        string newName = "UPDATEDPUBLISHER";

        // Act
        publisher.Name = CommonBusinessKey.FromString(newName);

        // Assert
        Assert.AreEqual(newName, publisher.Name.Key);
    }

    #endregion Public Methods
}