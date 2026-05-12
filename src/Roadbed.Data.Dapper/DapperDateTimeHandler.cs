namespace Roadbed.Data;

using System;
using System.Data;
using System.Globalization;
using Dapper;

/// <summary>
/// Dapper type handler for converting SQLite TEXT or MySQL/MariaDB DATETIME
/// columns to DateTime.
/// </summary>
/// <remarks>
/// <para>
/// SQLite stores DateTime as TEXT in ISO 8601 format (e.g. "2024-01-15 14:30:00").
/// All values are treated as UTC to maintain consistency.
/// </para>
/// <para>
/// MySQL / MariaDB DATETIME columns are naive — there is no timezone
/// information in the column. The application-side convention these
/// handlers serve is to write UTC values. On read-back the driver returns
/// <see cref="DateTime"/> with <see cref="DateTimeKind.Unspecified"/>;
/// this handler re-attaches <see cref="DateTimeKind.Utc"/> rather than
/// calling <see cref="DateTime.ToUniversalTime"/>, which would treat the
/// value as local time and shift it by the local timezone offset.
/// </para>
/// </remarks>
public class DapperDateTimeHandler : SqlMapper.TypeHandler<DateTime>
{
    /// <summary>
    /// Parses a database value into a DateTime.
    /// </summary>
    /// <param name="value">The value from the database (TEXT or DateTime).</param>
    /// <returns>A UTC DateTime.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the value cannot be converted to DateTime.</exception>
    public override DateTime Parse(object value)
    {
        if (value is string textValue)
        {
            // Parse as UTC and ensure Kind is set to UTC
            DateTime parsed = DateTime.Parse(textValue, CultureInfo.InvariantCulture);
            return DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
        }

        if (value is DateTime dateTime)
        {
            // The application-side convention is "always write UTC". The
            // MariaDB / MySQL driver returns DateTime with Kind=Unspecified
            // because DATETIME columns have no timezone info; re-attach UTC
            // rather than calling ToUniversalTime() (which would treat the
            // value as local and shift it). Kind=Local from an explicit
            // caller is still converted via ToUniversalTime() so a non-UTC
            // input is normalized on read.
            return dateTime.Kind switch
            {
                DateTimeKind.Utc => dateTime,
                DateTimeKind.Unspecified => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc),
                DateTimeKind.Local => dateTime.ToUniversalTime(),
                _ => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc),
            };
        }

        throw new InvalidOperationException($"Cannot convert {value?.GetType()} to DateTime");
    }

    /// <summary>
    /// Sets a DateTime value as a database parameter.
    /// </summary>
    /// <param name="parameter">The database parameter to set.</param>
    /// <param name="value">The DateTime value to store (will be converted to UTC).</param>
    /// <remarks>
    /// Non-UTC DateTime values are automatically converted to UTC before storage.
    /// </remarks>
    public override void SetValue(IDbDataParameter parameter, DateTime value)
    {
        // Convert to UTC before storing
        DateTime utcValue = value.Kind == DateTimeKind.Utc
            ? value
            : value.ToUniversalTime();

        parameter.Value = utcValue.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        parameter.DbType = DbType.String;
    }
}