namespace Roadbed.Data;

using System;
using System.Data;
using System.Globalization;
using Dapper;

/// <summary>
/// Dapper type handler for converting SQLite TEXT or MySQL/MariaDB DATE
/// columns to nullable <see cref="DateOnly"/>.
/// </summary>
/// <remarks>
/// <para>
/// SQLite has no native DATE type; values are stored as TEXT in ISO 8601
/// date-only format (<c>yyyy-MM-dd</c>).
/// </para>
/// <para>
/// MySQL / MariaDB DATE columns carry no time component. The driver returns
/// either a string (some configurations) or a <see cref="DateTime"/> with
/// the time at midnight; both shapes round-trip cleanly through
/// <see cref="DateOnly.FromDateTime(DateTime)"/>.
/// </para>
/// </remarks>
public class DapperNullableDateOnlyHandler : SqlMapper.TypeHandler<DateOnly?>
{
    /// <summary>
    /// Parses a database value into a nullable <see cref="DateOnly"/>.
    /// </summary>
    /// <param name="value">The value from the database (TEXT, DateTime, DateOnly, or NULL).</param>
    /// <returns>A <see cref="DateOnly"/> if the value is valid, otherwise <see langword="null"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the value cannot be converted to <see cref="DateOnly"/>.</exception>
    public override DateOnly? Parse(object value)
    {
        if (value == null || value is DBNull)
        {
            return null;
        }

        if (value is string textValue)
        {
            return DateOnly.Parse(textValue, CultureInfo.InvariantCulture);
        }

        if (value is DateTime dateTime)
        {
            return DateOnly.FromDateTime(dateTime);
        }

        if (value is DateOnly dateOnly)
        {
            return dateOnly;
        }

        throw new InvalidOperationException($"Cannot convert {value?.GetType()} to DateOnly?");
    }

    /// <summary>
    /// Sets a nullable <see cref="DateOnly"/> value as a database parameter.
    /// </summary>
    /// <param name="parameter">The database parameter to set.</param>
    /// <param name="value">The nullable <see cref="DateOnly"/> value to store.</param>
    /// <remarks>
    /// Writes the value as an ISO 8601 <c>yyyy-MM-dd</c> string with
    /// <see cref="DbType.Date"/>. This matches the existing DateTime
    /// handler's "write a string, let the driver round-trip it" pattern and
    /// works uniformly across SQLite (no native DATE type) and
    /// MySQL / MariaDB (DATE column). Null values are stored as
    /// <see cref="DBNull.Value"/>.
    /// </remarks>
    public override void SetValue(IDbDataParameter parameter, DateOnly? value)
    {
        if (value.HasValue)
        {
            parameter.Value = value.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            parameter.DbType = DbType.Date;
        }
        else
        {
            parameter.Value = DBNull.Value;
        }
    }
}
