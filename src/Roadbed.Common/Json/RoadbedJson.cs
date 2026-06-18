/*
 * The namespace Roadbed.Common was removed on purpose and replaced with Roadbed
 * so that no additional using statements are required.
 */

namespace Roadbed;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Single shared <see cref="JsonSerializerOptions"/> used by every Roadbed
/// JSON read/write. Constructing options per call is the single biggest
/// System.Text.Json performance footgun — its reflection-derived metadata
/// caches are keyed by options instance — so every Roadbed call site MUST
/// pass <see cref="Options"/> rather than allocating its own.
/// </summary>
/// <remarks>
/// <para>
/// The configuration mirrors the lenient defaults Roadbed inherited from
/// Newtonsoft.Json so that existing wire formats and persisted JSON keep
/// round-tripping after the migration:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="JsonSerializerOptions.PropertyNameCaseInsensitive"/> — STJ is case-sensitive by default; Newtonsoft was not. This is the most common silent break, so it is enabled globally.</description></item>
///   <item><description><see cref="JsonNumberHandling.AllowReadingFromString"/> — some upstream APIs send numbers as JSON strings.</description></item>
///   <item><description><see cref="JsonCommentHandling.Skip"/> + <see cref="JsonSerializerOptions.AllowTrailingCommas"/> — lenient-read parity with Newtonsoft.</description></item>
///   <item><description><see cref="JsonIgnoreCondition.WhenWritingNull"/> — preserves the null-omission output shape Roadbed DTOs declared per-property with <c>NullValueHandling.Ignore</c>.</description></item>
/// </list>
/// </remarks>
public static class RoadbedJson
{
    #region Public Properties

    /// <summary>
    /// Gets the shared options instance. Returned read-only so a caller
    /// cannot mutate the framework-wide configuration in flight.
    /// </summary>
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    #endregion Public Properties

    #region Private Methods

    /// <summary>
    /// Builds and freezes the shared options instance.
    /// </summary>
    /// <returns>A frozen <see cref="JsonSerializerOptions"/>.</returns>
    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        // MakeReadOnly(true) wires up the default reflection-based
        // TypeInfoResolver if none was set, then freezes the options so the
        // metadata cache is keyed by this one instance for the lifetime of
        // the process.
        options.MakeReadOnly(populateMissingResolver: true);
        return options;
    }

    #endregion Private Methods
}
