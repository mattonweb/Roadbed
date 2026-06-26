namespace Roadbed.Test.Unit.DbQueue;

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.DbQueue;

/// <summary>
/// Unit tests for <see cref="QueueNameValidator"/>.
/// </summary>
[TestClass]
public class QueueNameValidatorTests
{
    #region Public Methods

    /// <summary>
    /// Verifies that a typical lowercase-underscore queue name is accepted.
    /// </summary>
    [TestMethod]
    public void Validate_LowercaseUnderscoreDigits_DoesNotThrow()
    {
        // Arrange (Given) + Act (When) + Assert (Then)
        QueueNameValidator.Validate("foo_unsubscribe_42");
    }

    /// <summary>
    /// Verifies that the minimum-length single-character name is accepted.
    /// </summary>
    [TestMethod]
    public void Validate_SingleCharacter_DoesNotThrow()
    {
        // Arrange (Given) + Act (When) + Assert (Then)
        QueueNameValidator.Validate("a");
    }

    /// <summary>
    /// Verifies that a maximum-length valid name is accepted.
    /// </summary>
    [TestMethod]
    public void Validate_MaxLength_DoesNotThrow()
    {
        // Arrange (Given)
        string name = new ('a', QueueNameValidator.MaxLength);

        // Act (When) + Assert (Then)
        QueueNameValidator.Validate(name);
    }

    /// <summary>
    /// Verifies that null is rejected with <see cref="ArgumentException"/>.
    /// </summary>
    [TestMethod]
    public void Validate_Null_ThrowsArgumentException()
    {
        // Arrange (Given)
        string? name = null;
        bool thrown = false;

        // Act (When)
        try
        {
            QueueNameValidator.Validate(name!);
        }
        catch (ArgumentException)
        {
            thrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(thrown, "Null queue name must throw ArgumentException.");
    }

    /// <summary>
    /// Verifies that an empty string is rejected.
    /// </summary>
    [TestMethod]
    public void Validate_Empty_ThrowsArgumentException()
    {
        // Arrange (Given)
        bool thrown = false;

        // Act (When)
        try
        {
            QueueNameValidator.Validate(string.Empty);
        }
        catch (ArgumentException)
        {
            thrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(thrown, "Empty queue name must throw ArgumentException.");
    }

    /// <summary>
    /// Verifies that whitespace is rejected.
    /// </summary>
    [TestMethod]
    public void Validate_Whitespace_ThrowsArgumentException()
    {
        // Arrange (Given)
        bool thrown = false;

        // Act (When)
        try
        {
            QueueNameValidator.Validate("   ");
        }
        catch (ArgumentException)
        {
            thrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(thrown, "Whitespace queue name must throw ArgumentException.");
    }

    /// <summary>
    /// Verifies that an uppercase character is rejected.
    /// </summary>
    [TestMethod]
    public void Validate_UppercaseLetter_ThrowsArgumentException()
    {
        // Arrange (Given)
        bool thrown = false;

        // Act (When)
        try
        {
            QueueNameValidator.Validate("FooUnsubscribe");
        }
        catch (ArgumentException)
        {
            thrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(thrown, "Uppercase characters must be rejected by the whitelist.");
    }

    /// <summary>
    /// Verifies that a hyphen is rejected.
    /// </summary>
    [TestMethod]
    public void Validate_Hyphen_ThrowsArgumentException()
    {
        // Arrange (Given)
        bool thrown = false;

        // Act (When)
        try
        {
            QueueNameValidator.Validate("foo-unsubscribe");
        }
        catch (ArgumentException)
        {
            thrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(thrown, "Hyphen must be rejected by the whitelist.");
    }

    /// <summary>
    /// Verifies that a space is rejected.
    /// </summary>
    [TestMethod]
    public void Validate_InternalSpace_ThrowsArgumentException()
    {
        // Arrange (Given)
        bool thrown = false;

        // Act (When)
        try
        {
            QueueNameValidator.Validate("foo bar");
        }
        catch (ArgumentException)
        {
            thrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(thrown, "Internal whitespace must be rejected by the whitelist.");
    }

    /// <summary>
    /// Verifies that a SQL-statement terminator is rejected — the most
    /// obvious injection vector and the reason this validator exists.
    /// </summary>
    [TestMethod]
    public void Validate_Semicolon_ThrowsArgumentException()
    {
        // Arrange (Given)
        bool thrown = false;

        // Act (When)
        try
        {
            QueueNameValidator.Validate("foo;drop");
        }
        catch (ArgumentException)
        {
            thrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(thrown, "Semicolon must be rejected by the whitelist.");
    }

    /// <summary>
    /// Verifies that a backtick is rejected — the defense-in-depth quoting
    /// character we wrap identifiers in must not appear inside the name.
    /// </summary>
    [TestMethod]
    public void Validate_Backtick_ThrowsArgumentException()
    {
        // Arrange (Given)
        bool thrown = false;

        // Act (When)
        try
        {
            QueueNameValidator.Validate("foo`bar");
        }
        catch (ArgumentException)
        {
            thrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(thrown, "Backtick must be rejected by the whitelist.");
    }

    /// <summary>
    /// Verifies that exceeding <see cref="QueueNameValidator.MaxLength"/> is
    /// rejected.
    /// </summary>
    [TestMethod]
    public void Validate_OverMaxLength_ThrowsArgumentException()
    {
        // Arrange (Given)
        string name = new ('a', QueueNameValidator.MaxLength + 1);
        bool thrown = false;

        // Act (When)
        try
        {
            QueueNameValidator.Validate(name);
        }
        catch (ArgumentException)
        {
            thrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(thrown, "Length cap must be enforced.");
    }

    #endregion Public Methods
}
