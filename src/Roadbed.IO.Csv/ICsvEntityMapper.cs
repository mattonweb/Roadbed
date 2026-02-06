namespace Roadbed.IO;

using CsvHelper;

/// <summary>
/// Interface for mapping CSV entities.
/// </summary>
/// <typeparam name="T">Data type for the result entity.</typeparam>
public interface ICsvEntityMapper<out T>
{
    /// <summary>
    /// Maps a row from the CSV file to an entity of type <typeparamref name="T" />.
    /// </summary>
    /// <param name="reader">Reads data that was parsed from <see cref="IParser" />.</param>
    /// <returns>Entity populated with data from the row in the CSV File..</returns>
    T? MapEntity(CsvReader reader);
}