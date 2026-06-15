namespace Roadbed.IO;

using System.Data.Common;

/// <summary>
/// Maps a single positioned worksheet row to a typed entity, mirroring the
/// <c>ICsvEntityMapper&lt;T&gt;</c> idiom.
/// </summary>
/// <typeparam name="T">The entity type produced per row.</typeparam>
/// <remarks>
/// The reader is positioned on a data row. Read cells defensively — as strings
/// with <c>TryParse</c>, guarding nulls with <see cref="DbDataReader.IsDBNull"/>
/// — rather than with typed getters, because government spreadsheets are dirty
/// (mixed numeric/text columns, empty-string-as-null, and numeric cells that
/// drop leading zeros such as ZIP/FIPS codes). Return <c>null</c> to filter the
/// row out of the stream.
/// </remarks>
public interface IXlsxEntityMapper<out T>
{
    /// <summary>
    /// Maps the current row of <paramref name="reader"/> to a <typeparamref name="T"/>.
    /// </summary>
    /// <param name="reader">Reader positioned on the row to map. When the read used a header, look columns up with <see cref="DbDataReader.GetOrdinal(string)"/>; otherwise read by ordinal.</param>
    /// <returns>The mapped entity, or <c>null</c> to skip the row.</returns>
    T? MapEntity(DbDataReader reader);
}
