namespace Roadbed.Data;

using System;
using System.Data;
using System.Globalization;
using Dapper;

/// <summary>
/// Dapper type handler for converting SQLite TEXT or MySQL/MariaDB DATETIME
/// columns to nullable DateTimeOffset.
/// </summary>
/// <remarks>
/// <para>
/// SQLite stores DateTimeOffset as TEXT in ISO 8601 format with timezone
/// offset, e.g. "2024-01-15 14:30:00-06:00". Round-trip preserves the
/// original timezone offset.
/// </para>
/// <para>
/// MySQL / MariaDB DATETIME columns are naive — there is no timezone
/// information in the column. The application-side convention these
/// handlers serve is to write UTC values (callers use
/// <see cref="DateTimeOffset.UtcNow"/>). On read-back the driver returns
/// <see cref="DateTime"/> with <see cref="DateTimeKind.Unspecified"/>;
/// this handler re-attaches <see cref="DateTimeKind.Utc"/> and returns a
/// UTC-offset <see cref="DateTimeOffset"/>. If a caller writes a non-UTC
/// <see cref="DateTimeOffset"/> through these handlers against MariaDB,
/// the offset is silently dropped by the database on insert — the
/// "always UTC" convention is mandatory for the MariaDB backend.
/// </para>
/// </remarks>
public class DapperNullableDateTimeOffsetHandler : SqlMapper.TypeHandler<DateTimeOffset?>
{
    /// <summary>
    /// Parses a database value into a nullable DateTimeOffset.
    /// </summary>
    /// <param name="value">The value from the database (TEXT, DateTime, DateTimeOffset, or NULL).</param>
    /// <returns>A DateTimeOffset with timezone information preserved if the value is valid, otherwise null.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the value cannot be converted to DateTimeOffset.</exception>
    public override DateTimeOffset? Parse(object value)
    {
        if (value == null || value is DBNull)
        {
            return null;
        }

        if (value is string textValue)
        {
            return DateTimeOffset.Parse(textValue, CultureInfo.InvariantCulture);
        }

        if (value is DateTimeOffset dateTimeOffset)
        {
            return dateTimeOffset;
        }

        if (value is DateTime dateTime)
        {
            // MySQL / MariaDB DATETIME columns are naive (no timezone). The
            // application-side convention these handlers serve is "always write
            // UTC values" (callers use DateTimeOffset.UtcNow). On read-back, the
            // driver returns DateTime with Kind=Unspecified; we re-attach UTC.
            return new DateTimeOffset(
                DateTime.SpecifyKind(dateTime, DateTimeKind.Utc),
                TimeSpan.Zero);
        }

        throw new InvalidOperationException($"Cannot convert {value?.GetType()} to DateTimeOffset?");
    }

    /// <summary>
    /// Sets a nullable DateTimeOffset value as a database parameter.
    /// </summary>
    /// <param name="parameter">The database parameter to set.</param>
    /// <param name="value">The nullable DateTimeOffset value to store (timezone offset is preserved).</param>
    /// <remarks>
    /// The timezone offset is preserved in the stored format (e.g., "-06:00" for Central Time).
    /// Null values are stored as DBNull.
    /// </remarks>
    public override void SetValue(IDbDataParameter parameter, DateTimeOffset? value)
    {
        if (value.HasValue)
        {
            // Store in ISO 8601 format with timezone offset
            parameter.Value = value.Value.ToString("yyyy-MM-dd HH:mm:sszzz", CultureInfo.InvariantCulture);
            parameter.DbType = DbType.String;
        }
        else
        {
            parameter.Value = DBNull.Value;
        }
    }
}