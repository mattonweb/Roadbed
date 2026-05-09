namespace Roadbed.Secrets.KeePass;

/// <summary>
/// Supplies the master key and database file path that <see cref="KeePassReader"/>
/// needs to open a KeePass2 (<c>.kdbx</c>) database. The host application owns
/// how these values are sourced (configuration, secret store, CLI arg, etc.) —
/// this library does not assume any particular configuration layout.
/// </summary>
public interface IKeePassOptions
{
    /// <summary>
    /// Gets the master key for the KeePass2 database. Required.
    /// </summary>
    string MasterKey { get; }

    /// <summary>
    /// Gets the absolute path to the KeePass2 database file. Required.
    /// </summary>
    string DatabasePath { get; }
}
