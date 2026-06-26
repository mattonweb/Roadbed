namespace Roadbed.DbQueue;

using System;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

/// <summary>
/// Validates queue names that are interpolated into raw SQL as identifier
/// suffixes (e.g. <c>queue_message_{name}</c> / <c>queue_processed_{name}</c>).
/// </summary>
/// <remarks>
/// <para>
/// MySQL cannot parameterize table identifiers, so a queue name composes
/// directly into the SQL text. This validator is the safety guard: queue
/// names are library-/host-controlled (never user-supplied), but they are
/// still checked against a strict whitelist at
/// <see cref="QueueDefinition{T}"/> construction so a typo or misuse is
/// rejected before any SQL string is built.
/// </para>
/// <para>
/// Allowed: lowercase ASCII letters, digits, and underscore
/// (<c>^[a-z0-9_]+$</c>), with a maximum length of <see cref="MaxLength"/>
/// characters. The cap keeps the derived
/// <c>queue_processed_{name}</c> identifier within MySQL's 64-character
/// limit (<c>queue_processed_</c> is 16 characters).
/// </para>
/// </remarks>
internal static class QueueNameValidator
{
    #region Public Fields

    /// <summary>
    /// Maximum permitted length of a queue name. Sized so the derived
    /// <c>queue_processed_{name}</c> table identifier fits inside MySQL's
    /// 64-character identifier limit.
    /// </summary>
    public const int MaxLength = 48;

    #endregion Public Fields

    #region Private Fields

    /// <summary>
    /// Compiled whitelist regex: one or more lowercase ASCII letters,
    /// digits, or underscores.
    /// </summary>
    private static readonly Regex AllowedPattern = new (
        "^[a-z0-9_]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(50));

    #endregion Private Fields

    #region Public Methods

    /// <summary>
    /// Throws if <paramref name="queueName"/> is null, empty, whitespace,
    /// too long, or contains any character outside the whitelist.
    /// </summary>
    /// <param name="queueName">The queue name to validate.</param>
    /// <param name="paramName">Auto-supplied caller argument name; used as the <c>paramName</c> on any thrown <see cref="ArgumentException"/>.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="queueName"/> fails the whitelist:
    /// null/empty/whitespace, exceeds <see cref="MaxLength"/>, or contains
    /// characters other than lowercase ASCII letters, digits, or underscore.
    /// </exception>
    public static void Validate(
        string queueName,
        [CallerArgumentExpression(nameof(queueName))] string? paramName = null)
    {
        if (string.IsNullOrWhiteSpace(queueName))
        {
            throw new ArgumentException(
                "Queue name cannot be null, empty, or whitespace.",
                paramName);
        }

        if (queueName.Length > MaxLength)
        {
            throw new ArgumentException(
                $"Queue name '{queueName}' exceeds the maximum length of {MaxLength} characters.",
                paramName);
        }

        if (!AllowedPattern.IsMatch(queueName))
        {
            throw new ArgumentException(
                $"Queue name '{queueName}' is invalid. Only lowercase ASCII letters, digits, and underscores are allowed (pattern: ^[a-z0-9_]+$).",
                paramName);
        }
    }

    #endregion Public Methods
}
