/*
 * Keep the namespace as the root path to reduce the using; statements consuming projects (.csproj) need to include.
 */
namespace Roadbed.Secrets.KeePass;

/// <summary>
/// Plain-CLR-type representation of one KeePass entry's standard string fields.
/// Each property maps to the matching KeePass <c>PwEntry.Strings.ReadSafe(...)</c>
/// key (Title, UserName, Password, URL, Notes). Custom string fields are not
/// surfaced in sprint 1.
/// </summary>
public sealed class KeePassSecret
{
    #region Public Properties

    /// <summary>
    /// Gets the KeePass entry's <c>Title</c> field.
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Gets the KeePass entry's <c>UserName</c> field.
    /// </summary>
    public string UserName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the KeePass entry's <c>Password</c> field.
    /// </summary>
    public string Password { get; init; } = string.Empty;

    /// <summary>
    /// Gets the KeePass entry's <c>URL</c> field.
    /// </summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>
    /// Gets the KeePass entry's <c>Notes</c> field.
    /// </summary>
    public string Notes { get; init; } = string.Empty;

    #endregion
}
