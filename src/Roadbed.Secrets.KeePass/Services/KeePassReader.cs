namespace Roadbed.Secrets.KeePass;

using System;
using System.Collections.Generic;
using System.IO;
using KeePassLib;
using KeePassLib.Keys;
using KeePassLib.Serialization;
using Microsoft.Extensions.Logging;
using Roadbed;

/// <summary>
/// Opens a KeePass2 database once at construction, caches every entry's
/// standard string fields in memory, and serves <see cref="Read"/> calls
/// from the cache. Designed for startup-time use by the host application;
/// the database file is opened exactly once per <see cref="KeePassReader"/>
/// instance.
/// </summary>
/// <remarks>
/// <para>
/// The class is intentionally <b>not sealed</b> so consumers managing
/// multiple KeePass databases in DI can declare a one-line subclass per
/// database, paired with a marker subinterface of
/// <see cref="IKeePassOptions"/>. Subclasses use the
/// <see cref="KeePassReader(IKeePassOptions, ILogger)"/> protected ctor
/// so they can pass their own typed <see cref="ILogger{TCategoryName}"/>.
/// </para>
/// <para>
/// All members are non-virtual; subclasses inherit behavior unchanged.
/// </para>
/// </remarks>
public class KeePassReader
    : BaseClassWithLogging
{
    #region Private Fields

    private readonly IReadOnlyDictionary<string, KeePassSecret> _entries;

    #endregion

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="KeePassReader"/> class.
    /// Opens the KeePass database identified by <paramref name="settings"/>,
    /// loads every entry's standard string fields into an in-memory cache,
    /// and closes the file. All construction-time errors (missing file,
    /// blank options, wrong master key, malformed database) surface here so
    /// the host application fails fast at startup.
    /// </summary>
    /// <param name="settings">
    /// Required. Supplies the master key and the absolute path to the
    /// <c>.kdbx</c> database file. Both fields must be populated.
    /// </param>
    /// <param name="logger">Typed logger.</param>
    /// <exception cref="ArgumentNullException"><paramref name="settings"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">
    /// <see cref="IKeePassOptions.MasterKey"/> or <see cref="IKeePassOptions.DatabasePath"/>
    /// is blank, or the database file does not exist on disk.
    /// </exception>
    public KeePassReader(IKeePassOptions settings, ILogger<KeePassReader> logger)
        : this(settings, (ILogger)logger)
    {
    }

    #endregion

    #region Protected Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="KeePassReader"/> class
    /// from a non-generic <see cref="ILogger"/>. Subclasses use this overload
    /// so they can pass their own typed <see cref="ILogger{TCategoryName}"/>
    /// (which IS-A <see cref="ILogger"/>) and keep their distinct logger
    /// category for log filtering. The body is identical to the public
    /// constructor.
    /// </summary>
    /// <param name="settings">
    /// Required. Supplies the master key and the absolute path to the
    /// <c>.kdbx</c> database file. Both fields must be populated.
    /// </param>
    /// <param name="logger">Non-generic logger.</param>
    /// <exception cref="ArgumentNullException"><paramref name="settings"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">
    /// <see cref="IKeePassOptions.MasterKey"/> or <see cref="IKeePassOptions.DatabasePath"/>
    /// is blank, or the database file does not exist on disk.
    /// </exception>
    protected KeePassReader(IKeePassOptions settings, ILogger logger)
        : base(logger)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var masterKey = ResolveMasterKey(settings);
        var path = ResolveDatabasePath(settings);
        this._entries = LoadAllEntries(path, masterKey);

        this.LogInformation(
            "Loaded {EntryCount} entries from KeePass database {DatabasePath}",
            this._entries.Count,
            path);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Returns the cached <see cref="KeePassSecret"/> whose Title matches
    /// <paramref name="entryTitle"/>. The KeePass database is not re-opened
    /// — every call is a dictionary lookup against the cache populated at
    /// construction.
    /// </summary>
    /// <param name="entryTitle">Exact Title of the entry to read.</param>
    /// <returns>The matching entry's fields, never <see langword="null"/>.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="entryTitle"/> is null, empty, or whitespace.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// No entry with the requested Title exists in the cached database.
    /// </exception>
    public KeePassSecret Read(string entryTitle)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entryTitle);

        if (!this._entries.TryGetValue(entryTitle, out var secret))
        {
            throw new InvalidOperationException(
                $"Entry with title '{entryTitle}' was not found in the KeePass database.");
        }

        return secret;
    }

    #endregion

    #region Private Methods

    private static string ResolveMasterKey(IKeePassOptions settings)
    {
        var masterKey = settings.MasterKey;

        if (string.IsNullOrWhiteSpace(masterKey))
        {
            throw new InvalidOperationException(
                $"KeePass master key was not supplied. "
                + $"Populate {nameof(IKeePassOptions)}.{nameof(IKeePassOptions.MasterKey)} "
                + $"from the host application's secret store.");
        }

        return masterKey;
    }

    private static string ResolveDatabasePath(IKeePassOptions settings)
    {
        var path = settings.DatabasePath;

        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException(
                $"KeePass database path was not supplied. "
                + $"Populate {nameof(IKeePassOptions)}.{nameof(IKeePassOptions.DatabasePath)} "
                + $"with the absolute path to the .kdbx file.");
        }

        if (!File.Exists(path))
        {
            throw new InvalidOperationException(
                $"KeePass database file was not found at path: {path}");
        }

        return path;
    }

    private static IReadOnlyDictionary<string, KeePassSecret> LoadAllEntries(string path, string masterKey)
    {
        var compositeKey = new CompositeKey();
        compositeKey.AddUserKey(new KcpPassword(masterKey));

        var ioConnectionInfo = new IOConnectionInfo { Path = path };

        var database = new PwDatabase();
        try
        {
            database.Open(ioConnectionInfo, compositeKey, null);

            // KeePass allows duplicate titles. On collision the first
            // entry wins — matches the reference example's FirstOrDefault
            // semantics.
            //
            // GetEntries(true) walks the entire group tree recursively.
            // KeePass UI-created databases place entries inside default
            // subgroups (General, Windows, Network, …); a flattened
            // database (entries directly under RootGroup) also works
            // since RootGroup is the starting point. RootGroup.Entries
            // alone would only see directly-attached entries and miss
            // anything in a subgroup.
            var result = new Dictionary<string, KeePassSecret>(StringComparer.Ordinal);
            foreach (var entry in database.RootGroup.GetEntries(true))
            {
                var title = entry.Strings.ReadSafe("Title");
                if (!result.ContainsKey(title))
                {
                    result[title] = new KeePassSecret
                    {
                        Title = title,
                        UserName = entry.Strings.ReadSafe("UserName"),
                        Password = entry.Strings.ReadSafe("Password"),
                        Url = entry.Strings.ReadSafe("URL"),
                        Notes = entry.Strings.ReadSafe("Notes"),
                    };
                }
            }

            return result;
        }
        finally
        {
            if (database.IsOpen)
            {
                database.Close();
            }
        }
    }

    #endregion
}
