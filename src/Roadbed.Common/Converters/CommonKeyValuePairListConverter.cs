namespace Roadbed.Common.Converters;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Custom System.Text.Json converter for an <see cref="IList{T}"/> of
/// <see cref="CommonKeyValuePair{TKey, TValue}"/> that is serialized as a flat
/// JSON object — each pair's <see cref="CommonKeyValuePair{TKey, TValue}.Key"/>
/// becomes a property name and its
/// <see cref="CommonKeyValuePair{TKey, TValue}.Value"/> becomes the property
/// value.
/// </summary>
/// <typeparam name="TKey">Key data type in pair.</typeparam>
/// <typeparam name="TValue">Value data type in pair.</typeparam>
public class CommonKeyValuePairListConverter<TKey, TValue>
    : JsonConverter<IList<CommonKeyValuePair<TKey, TValue>>>
{
    #region Public Methods

    /// <inheritdoc/>
    public override IList<CommonKeyValuePair<TKey, TValue>>? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException(
                $"Expected StartObject token but found {reader.TokenType}.");
        }

        var result = new List<CommonKeyValuePair<TKey, TValue>>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return result;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException(
                    $"Expected PropertyName token but found {reader.TokenType}.");
            }

            string propertyName = reader.GetString() ?? string.Empty;

            TKey? key = (TKey?)Convert.ChangeType(
                propertyName,
                typeof(TKey),
                CultureInfo.InvariantCulture);

            // Advance to the value token before deserializing.
            reader.Read();
            TValue? value = JsonSerializer.Deserialize<TValue>(ref reader, options);

            if (key is not null)
            {
                result.Add(new CommonKeyValuePair<TKey, TValue>(key, value!));
            }
        }

        throw new JsonException("Unexpected end of JSON while reading object.");
    }

    /// <inheritdoc/>
    public override void Write(
        Utf8JsonWriter writer,
        IList<CommonKeyValuePair<TKey, TValue>>? value,
        JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        if (value is not null)
        {
            foreach (var pair in value)
            {
                if ((pair is not null) && (pair.Key is not null))
                {
                    string keyString = pair.Key.ToString() ?? string.Empty;

                    writer.WritePropertyName(keyString);
                    JsonSerializer.Serialize(writer, pair.Value, options);
                }
            }
        }

        writer.WriteEndObject();
    }

    #endregion Public Methods
}
