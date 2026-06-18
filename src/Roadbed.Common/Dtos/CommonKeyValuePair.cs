/*
 * The namespace Roadbed.Common.Dtos was removed on purpose and replaced with Roadbed.Common so that no additional using statements are required.
 */

namespace Roadbed.Common;

using System.Collections.Generic;
using System.Text.Json.Serialization;

/// <summary>
/// Non-unique Key/Value Pair.
/// </summary>
/// <typeparam name="TKey">Key data type in pair.</typeparam>
/// <typeparam name="TValue">Value data type in pair.</typeparam>
public sealed class CommonKeyValuePair<TKey, TValue>
    : IEquatable<CommonKeyValuePair<TKey, TValue>>
{
    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="CommonKeyValuePair{key, value}"/> class.
    /// </summary>
    public CommonKeyValuePair()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CommonKeyValuePair{key, value}"/> class.
    /// </summary>
    /// <param name="key">Name of the pair.</param>
    /// <param name="value">Value of the pair.</param>
    public CommonKeyValuePair(TKey key, TValue value)
    {
        this.Key = key;
        this.Value = value;
    }

    #endregion Public Constructors

    #region Public Properties

    /// <summary>
    /// Gets or sets the attribute key.
    /// </summary>
    [JsonPropertyName("key")]
    public TKey? Key { get; set; }

    /// <summary>
    /// Gets or sets the attribute value.
    /// </summary>
    [JsonPropertyName("value")]
    public TValue? Value { get; set; }

    #endregion Public Properties

    #region Public Methods

    /// <summary>
    /// Determines whether two <see cref="CommonKeyValuePair{TKey, TValue}"/> instances are not equal.
    /// </summary>
    /// <param name="left">Instance of <see cref="CommonKeyValuePair{TKey, TValue}"/>.</param>
    /// <param name="right">Instance next to this instance.</param>
    /// <returns>true if the specified objects are equal; otherwise, false.</returns>
    public static bool operator !=(CommonKeyValuePair<TKey, TValue>? left, CommonKeyValuePair<TKey, TValue>? right)
    {
        return !(left == right);
    }

    /// <summary>
    /// Determines whether two <see cref="CommonKeyValuePair{TKey, TValue}"/> instances are equal.
    /// </summary>
    /// <param name="left">Instance of <see cref="CommonKeyValuePair{TKey, TValue}"/>.</param>
    /// <param name="right">Instance next to this instance.</param>
    /// <returns>true if the specified objects are equal; otherwise, false.</returns>
    public static bool operator ==(CommonKeyValuePair<TKey, TValue>? left, CommonKeyValuePair<TKey, TValue>? right)
    {
        if (left is null)
        {
            return right is null;
        }

        return left.Equals(right);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is CommonKeyValuePair<TKey, TValue> other && this.Equals(other);
    }

    /// <summary>
    /// Determines whether the specified objects are equal.
    /// </summary>
    /// <param name="other">Other instance of <see cref="CommonKeyValuePair{TKey, TValue}"/>.</param>
    /// <returns>true if the specified objects are equal; otherwise, false.</returns>
    public bool Equals(CommonKeyValuePair<TKey, TValue>? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return EqualityComparer<TKey>.Default.Equals(this.Key, other.Key) &&
               EqualityComparer<TValue>.Default.Equals(this.Value, other.Value);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(this.Key, this.Value);
    }

    #endregion Public Methods
}