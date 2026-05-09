namespace Roadbed.Test.Unit.Secrets.KeePass;

using System;
using System.IO;
using global::KeePassLib;
using global::KeePassLib.Keys;
using global::KeePassLib.Security;
using global::KeePassLib.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Secrets.KeePass;

/// <summary>
/// Contains unit tests for verifying the behavior of the
/// <see cref="KeePassReader"/> class. The tests run against a programmatically
/// generated KeePass database written into the test output directory at
/// class-initialization time (no committed binary fixture).
/// </summary>
[TestClass]
public sealed class KeePassReaderTests
{
    #region Private Fields

    private const string TestMasterKey = "test-master-key";

    private const string PopulatedEntryTitle = "Test Entry";
    private const string PopulatedEntryUserName = "test-user";
    private const string PopulatedEntryPassword = "test-password-123";
    private const string PopulatedEntryUrl = "https://test.example.com";
    private const string PopulatedEntryNotes = "test notes";

    private const string EmptyEntryTitle = "Empty Entry";

    private static string? _testDatabasePath;

    private KeePassReader? _reader;

    #endregion Private Fields

    #region Public Methods

    /// <summary>
    /// Generates the test KeePass database (containing two known entries) into
    /// the test output directory before any test method runs. The file is
    /// re-generated on every test-assembly load so the contents are always in
    /// sync with what the tests below expect.
    /// </summary>
    /// <param name="context">Provided by MSTest; not used here.</param>
    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var resourcesDirectory = Path.Combine(AppContext.BaseDirectory, "Resources");
        Directory.CreateDirectory(resourcesDirectory);

        _testDatabasePath = Path.Combine(resourcesDirectory, "roadbed-keepass-test.kdbx");
        if (File.Exists(_testDatabasePath))
        {
            File.Delete(_testDatabasePath);
        }

        CreateTestDatabase(_testDatabasePath, TestMasterKey);
    }

    /// <summary>
    /// Constructs a fresh <see cref="KeePassReader"/> pointing at the generated
    /// test database before every test method. Tests that exercise construction
    /// failures construct their own reader with bad inputs and ignore the
    /// shared instance.
    /// </summary>
    [TestInitialize]
    public void TestInitialize()
    {
        var options = new TestKeePassOptions
        {
            MasterKey = TestMasterKey,
            DatabasePath = _testDatabasePath!,
        };

        this._reader = new KeePassReader(
            options,
            NullLogger<KeePassReader>.Instance);
    }

    /// <summary>
    /// Unit test to verify that Read returns all five standard fields when the
    /// entry exists and every field is populated.
    /// </summary>
    [TestMethod]
    public void Read_ExistingEntry_ReturnsAllStandardFields()
    {
        // Arrange (Given)
        // The shared reader is initialized in TestInitialize and points at the
        // generated test database.

        // Act (When)
        var secret = this._reader!.Read(PopulatedEntryTitle);

        // Assert (Then)
        Assert.IsNotNull(
            secret,
            "Read should never return null for a known-existing entry.");
        Assert.AreEqual(
            PopulatedEntryTitle,
            secret.Title,
            "Read should round-trip the entry's Title field.");
        Assert.AreEqual(
            PopulatedEntryUserName,
            secret.UserName,
            "Read should round-trip the entry's UserName field.");
        Assert.AreEqual(
            PopulatedEntryPassword,
            secret.Password,
            "Read should round-trip the entry's Password field.");
        Assert.AreEqual(
            PopulatedEntryUrl,
            secret.Url,
            "Read should round-trip the entry's URL field.");
        Assert.AreEqual(
            PopulatedEntryNotes,
            secret.Notes,
            "Read should round-trip the entry's Notes field.");
    }

    /// <summary>
    /// Unit test to verify that Read returns empty strings for every field
    /// when the entry exists but no fields are populated.
    /// </summary>
    [TestMethod]
    public void Read_EmptyEntry_ReturnsAllEmptyStrings()
    {
        // Arrange (Given)
        // The "Empty Entry" row in the generated database has Title set but
        // every other field left blank.

        // Act (When)
        var secret = this._reader!.Read(EmptyEntryTitle);

        // Assert (Then)
        Assert.AreEqual(
            EmptyEntryTitle,
            secret.Title,
            "Read should round-trip the entry's Title even when other fields are blank.");
        Assert.AreEqual(
            string.Empty,
            secret.UserName,
            "An entry with no UserName should surface as an empty string.");
        Assert.AreEqual(
            string.Empty,
            secret.Password,
            "An entry with no Password should surface as an empty string.");
        Assert.AreEqual(
            string.Empty,
            secret.Url,
            "An entry with no URL should surface as an empty string.");
        Assert.AreEqual(
            string.Empty,
            secret.Notes,
            "An entry with no Notes should surface as an empty string.");
    }

    /// <summary>
    /// Unit test to verify that Read throws InvalidOperationException when the
    /// requested entry does not exist in the cache.
    /// </summary>
    [TestMethod]
    public void Read_MissingEntry_ThrowsInvalidOperationException()
    {
        // Arrange (Given)
        const string missingTitle = "No Such Entry";
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            this._reader!.Read(missingTitle);
        }
        catch (InvalidOperationException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "Read should throw InvalidOperationException when no entry matches the requested title.");
    }

    /// <summary>
    /// Unit test to verify that Read throws ArgumentException when the
    /// entryTitle argument is null, empty, or whitespace.
    /// </summary>
    /// <param name="entryTitle">Blank entryTitle value supplied by DataRow.</param>
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void Read_BlankEntryTitle_ThrowsArgumentException(string? entryTitle)
    {
        // Arrange (Given)
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            this._reader!.Read(entryTitle!);
        }
        catch (ArgumentException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "Read should throw ArgumentException when entryTitle is null, empty, or whitespace.");
    }

    /// <summary>
    /// Unit test to verify that the constructor throws InvalidOperationException
    /// when the configured database file does not exist on disk.
    /// </summary>
    [TestMethod]
    public void Constructor_MissingDatabaseFile_ThrowsInvalidOperationException()
    {
        // Arrange (Given)
        var options = new TestKeePassOptions
        {
            MasterKey = TestMasterKey,
            DatabasePath = Path.Combine(
                AppContext.BaseDirectory,
                "Resources",
                "does-not-exist.kdbx"),
        };
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            _ = new KeePassReader(options, NullLogger<KeePassReader>.Instance);
        }
        catch (InvalidOperationException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "Constructor should throw InvalidOperationException when the database file does not exist.");
    }

    /// <summary>
    /// Unit test to verify that the constructor throws InvalidOperationException
    /// when the configured database path is null, empty, or whitespace.
    /// </summary>
    /// <param name="databasePath">Blank database path value supplied by DataRow.</param>
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void Constructor_BlankDatabasePath_ThrowsInvalidOperationException(string? databasePath)
    {
        // Arrange (Given)
        var options = new TestKeePassOptions
        {
            MasterKey = TestMasterKey,
            DatabasePath = databasePath ?? string.Empty,
        };
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            _ = new KeePassReader(options, NullLogger<KeePassReader>.Instance);
        }
        catch (InvalidOperationException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "Constructor should throw InvalidOperationException when the database path is blank.");
    }

    /// <summary>
    /// Unit test to verify that the constructor throws ArgumentNullException
    /// when the settings argument is null.
    /// </summary>
    [TestMethod]
    public void Constructor_NullSettings_ThrowsArgumentNullException()
    {
        // Arrange (Given)
        IKeePassOptions? nullOptions = null;
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            _ = new KeePassReader(nullOptions!, NullLogger<KeePassReader>.Instance);
        }
        catch (ArgumentNullException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "Constructor should throw ArgumentNullException when settings is null.");
    }

    /// <summary>
    /// Unit test to verify that the constructor throws InvalidOperationException
    /// when the configured master key is null, empty, or whitespace.
    /// </summary>
    /// <param name="masterKey">Blank master key value supplied by DataRow.</param>
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void Constructor_BlankMasterKey_ThrowsInvalidOperationException(string? masterKey)
    {
        // Arrange (Given)
        var options = new TestKeePassOptions
        {
            MasterKey = masterKey ?? string.Empty,
            DatabasePath = _testDatabasePath!,
        };
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            _ = new KeePassReader(options, NullLogger<KeePassReader>.Instance);
        }
        catch (InvalidOperationException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "Constructor should throw InvalidOperationException when masterKey is null, empty, or whitespace.");
    }

    /// <summary>
    /// Unit test to verify that the constructor loads every known entry into
    /// the cache so subsequent Read calls on every title succeed.
    /// </summary>
    [TestMethod]
    public void Constructor_ValidDatabase_LoadsAllEntriesIntoCache()
    {
        // Arrange (Given)
        // The shared reader is initialized in TestInitialize against the
        // generated database which contains exactly two entries.

        // Act (When)
        var populated = this._reader!.Read(PopulatedEntryTitle);
        var empty = this._reader!.Read(EmptyEntryTitle);

        // Assert (Then)
        Assert.AreEqual(
            PopulatedEntryUserName,
            populated.UserName,
            "Read should return the populated entry from the cache.");
        Assert.AreEqual(
            EmptyEntryTitle,
            empty.Title,
            "Read should return the empty entry from the cache.");
    }

    /// <summary>
    /// Unit test to verify that a subclass using a marker subinterface of
    /// <see cref="IKeePassOptions"/> and its own typed
    /// <see cref="Microsoft.Extensions.Logging.ILogger{TCategoryName}"/>
    /// constructs and reads identically to the base class. This is the
    /// canonical multi-database wire-up shape documented in the
    /// <c>code-roadbed-csharp</c> skill reference.
    /// </summary>
    [TestMethod]
    public void Subclass_WithMarkerOptionsAndTypedLogger_ReadsExpectedEntry()
    {
        // Arrange (Given)
        var options = new FooKeePassOptions
        {
            MasterKey = TestMasterKey,
            DatabasePath = _testDatabasePath!,
        };
        var fooReader = new FooKeePassReader(
            options,
            NullLogger<FooKeePassReader>.Instance);

        // Act (When)
        var secret = fooReader.Read(PopulatedEntryTitle);

        // Assert (Then)
        Assert.AreEqual(
            PopulatedEntryUserName,
            secret.UserName,
            "Subclassed reader should round-trip the populated entry from the cache.");
    }

    #endregion Public Methods

    #region Private Methods

    private static void CreateTestDatabase(string path, string masterKey)
    {
        var compositeKey = new CompositeKey();
        compositeKey.AddUserKey(new KcpPassword(masterKey));

        var connectionInfo = new IOConnectionInfo { Path = path };

        var database = new PwDatabase();
        try
        {
            database.New(connectionInfo, compositeKey);

            database.RootGroup.AddEntry(
                BuildEntry(
                    title: PopulatedEntryTitle,
                    userName: PopulatedEntryUserName,
                    password: PopulatedEntryPassword,
                    url: PopulatedEntryUrl,
                    notes: PopulatedEntryNotes),
                bTakeOwnership: true);

            database.RootGroup.AddEntry(
                BuildEntry(
                    title: EmptyEntryTitle,
                    userName: string.Empty,
                    password: string.Empty,
                    url: string.Empty,
                    notes: string.Empty),
                bTakeOwnership: true);

            database.Save(null);
        }
        finally
        {
            if (database.IsOpen)
            {
                database.Close();
            }
        }
    }

    private static PwEntry BuildEntry(
        string title,
        string userName,
        string password,
        string url,
        string notes)
    {
        var entry = new PwEntry(true, true);
        entry.Strings.Set("Title", new ProtectedString(false, title));
        entry.Strings.Set("UserName", new ProtectedString(false, userName));
        entry.Strings.Set("Password", new ProtectedString(true, password));
        entry.Strings.Set("URL", new ProtectedString(false, url));
        entry.Strings.Set("Notes", new ProtectedString(false, notes));
        return entry;
    }

    #endregion Private Methods

    #region Private Types

    private sealed class TestKeePassOptions : IKeePassOptions
    {
        public string MasterKey { get; init; } = string.Empty;

        public string DatabasePath { get; init; } = string.Empty;
    }

    /// <summary>
    /// Marker subinterface of <see cref="IKeePassOptions"/>. Exists so the
    /// DI container (and the subclassed reader below) can distinguish one
    /// KeePass database registration from another.
    /// </summary>
    private interface IFooKeePassOptions : IKeePassOptions
    {
    }

    private sealed class FooKeePassOptions : IFooKeePassOptions
    {
        public string MasterKey { get; init; } = string.Empty;

        public string DatabasePath { get; init; } = string.Empty;
    }

    /// <summary>
    /// Sample subclass that pairs a marker options interface with a typed
    /// logger category. Used by the subclass round-trip test to prove the
    /// protected constructor on <see cref="KeePassReader"/> is reachable
    /// through inheritance.
    /// </summary>
    private sealed class FooKeePassReader : KeePassReader
    {
        public FooKeePassReader(
            IFooKeePassOptions options,
            ILogger<FooKeePassReader> logger)
            : base(options, logger)
        {
        }
    }

    #endregion Private Types
}
