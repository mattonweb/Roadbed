namespace Roadbed.IO;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;

/// <summary>
/// Implementation of <see cref="IoFile"/> for CSV files.
/// </summary>
/// <typeparam name="T">Type of Data Transfer Object (DTO) that represents each row in the CSV file.</typeparam>
public class IoCsvFile<T>
    : IoFile
{
    #region Protected Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="IoCsvFile{T}"/> class.
    /// </summary>
    /// <param name="dataMapper">Data mapper used to turn lines in the CSV into a <see cref="IList{T}"/>.</param>
    protected IoCsvFile(ICsvEntityMapper<T> dataMapper)
    {
        ArgumentNullException.ThrowIfNull(dataMapper);

        // Initialize Properties
        this.DataRows = new List<T>();
        this.DataMapper = dataMapper;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IoCsvFile{T}"/> class.
    /// </summary>
    /// <param name="fileInfo">System information about the file.</param>
    /// <param name="dataMapper">Data mapper used to turn lines in the CSV into a <see cref="IList{T}"/>.</param>
    protected IoCsvFile(IoFileInfo fileInfo, ICsvEntityMapper<T> dataMapper)
        : base(fileInfo)
    {
        ArgumentNullException.ThrowIfNull(dataMapper);

        // Validate "In" Values
        ValidateFileInfo(fileInfo);

        if (!string.Equals(fileInfo?.Extension, ".csv", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("File extension isn't 'CSV'.", nameof(fileInfo));
        }

        // Initialize Properties
        this.DataRows = new List<T>();
        this.DataMapper = dataMapper;
    }

    #endregion Protected Constructors

    #region Public Properties

    /// <summary>
    /// Gets or sets the data mapper used to turn lines in the CSV into a <see cref="IList{T}"/>.
    /// </summary>
    public ICsvEntityMapper<T>? DataMapper { get; set; }

    /// <summary>
    /// Gets or sets the rows of data in the CSV.
    /// </summary>
    public IList<T> DataRows { get; set; }

    #endregion Public Properties

    #region Public Methods

    /// <summary>
    /// Creates an instance of <see cref="IoCsvFile{T}"/> from a file.
    /// </summary>
    /// <param name="path">File path to the CSV file.</param>
    /// <param name="dataMapper">Data mapper used to turn lines in the CSV into a <see cref="IList{T}"/>.</param>
    /// <returns>Instance of <see cref="IoCsvFile{T}"/>.</returns>
    public static IoCsvFile<T> FromFile(string path, ICsvEntityMapper<T> dataMapper)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(dataMapper);

        // Create Instance
        IoCsvFile<T> file = new IoCsvFile<T>(
            new IoFileInfo(path),
            dataMapper);

        // Read the File
        file.LoadDataRowsFromFile();

        // Return result
        return file;
    }

    /// <summary>
    /// Asynchronously creates an instance of <see cref="IoCsvFile{T}"/> from a file.
    /// </summary>
    /// <param name="path">File path to the CSV file.</param>
    /// <param name="dataMapper">Data mapper used to turn lines in the CSV into a <see cref="IList{T}"/>.</param>
    /// <returns>Task that represents the asynchronous operation. The task result contains an instance of <see cref="IoCsvFile{T}"/>.</returns>
    public static async Task<IoCsvFile<T>> FromFileAsync(string path, ICsvEntityMapper<T> dataMapper)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(dataMapper);

        // Create Instance
        IoCsvFile<T> file = new IoCsvFile<T>(
            new IoFileInfo(path),
            dataMapper);

        // Read the File
        await file.LoadDataRowsFromFileAsync();

        // Return result
        return file;
    }

    /// <summary>
    /// Creates an instance of <see cref="IoCsvFile{T}"/> from a string.
    /// </summary>
    /// <param name="content">CSV content as a string.</param>
    /// <param name="dataMapper">Data mapper used to turn lines in the CSV into a <see cref="IList{T}"/>.</param>
    /// <returns>Instance of <see cref="IoCsvFile{T}"/>.</returns>
    public static IoCsvFile<T> FromString(string content, ICsvEntityMapper<T> dataMapper)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        ArgumentNullException.ThrowIfNull(dataMapper);

        // Create Instance
        IoCsvFile<T> file = new IoCsvFile<T>(dataMapper);

        // Read the File
        file.LoadDataRowsFromString(content);

        // Return result
        return file;
    }

    /// <summary>
    /// Asynchronously creates an instance of <see cref="IoCsvFile{T}"/> from a string.
    /// </summary>
    /// <param name="content">CSV content as a string.</param>
    /// <param name="dataMapper">Data mapper used to turn lines in the CSV into a <see cref="IList{T}"/>.</param>
    /// <returns>Task that represents the asynchronous operation. The task result contains an instance of <see cref="IoCsvFile{T}"/>.</returns>
    public static async Task<IoCsvFile<T>> FromStringAsync(string content, ICsvEntityMapper<T> dataMapper)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        ArgumentNullException.ThrowIfNull(dataMapper);

        // Create Instance
        IoCsvFile<T> file = new IoCsvFile<T>(dataMapper);

        // Read the File
        await file.LoadDataRowsFromStringAsync(content);

        // Return result
        return file;
    }

    /// <summary>
    /// Exports the <see cref="DataRows"/> property as a content string.
    /// </summary>
    /// <returns>Content string formatted as a CSV.</returns>
    public string ExportDataRowsAsContentString()
    {
        return this.ExportDataRowsAsContentString(GetDefaultConfiguration());
    }

    /// <summary>
    /// Exports the <see cref="DataRows"/> property as a content string.
    /// </summary>
    /// <param name="configuration">CsvHelper configuration used in the export process.</param>
    /// <returns>Content string formatted as a CSV.</returns>
    public string ExportDataRowsAsContentString(CsvConfiguration configuration)
    {
        if (this.DataRows is null || configuration is null)
        {
            return string.Empty;
        }

        string result = string.Empty;

        using MemoryStream memoryStream = new MemoryStream();
        using StreamWriter streamWriter = new StreamWriter(memoryStream);
        using CsvWriter csvWriter = new CsvWriter(streamWriter, configuration);

        csvWriter.WriteRecords(this.DataRows);
        streamWriter.Flush();
        result = Encoding.UTF8.GetString(memoryStream.ToArray());

        return result;
    }

    /// <summary>
    /// Fills the <see cref="DataRows"/> property by reading the CSV content from the <see cref="IoFileInfo"/>.
    /// </summary>
    public void LoadDataRowsFromFile()
    {
        // Validate "In" Properties
        ValidateFileInfo(this.FileInfo!);
        ValidateDataMapper(this.DataMapper);

        // Reset "Out" Properties
        this.DataRows = new List<T>();

        // Fill Data Rows from File
        using FileStream stream = new FileStream(this.FileInfo!.FullPath!, FileMode.Open, FileAccess.Read, FileShare.Read);
        using TextReader textReader = new StreamReader(stream);
        using CsvReader csvReader = new CsvReader(textReader, CultureInfo.InvariantCulture);

        csvReader.Read();
        csvReader.ReadHeader();

        while (csvReader.Read())
        {
            var obj = this.DataMapper!.MapEntity(csvReader);

            if (obj is not null)
            {
                this.DataRows.Add(obj);
            }
        }
    }

    /// <summary>
    /// Asynchronously fills the <see cref="DataRows"/> property by reading the CSV content from the <see cref="IoFileInfo"/>.
    /// </summary>
    /// <returns>Task that represents the asynchronous operation.</returns>
    public async Task LoadDataRowsFromFileAsync()
    {
        // Validate "In" Properties
        ValidateFileInfo(this.FileInfo!);
        ValidateDataMapper(this.DataMapper);

        // Reset "Out" Properties
        this.DataRows = new List<T>();

        // Fill Data Rows from File
        await using FileStream stream = new FileStream(this.FileInfo!.FullPath!, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
        using TextReader textReader = new StreamReader(stream);
        using CsvReader csvReader = new CsvReader(textReader, CultureInfo.InvariantCulture);

        await csvReader.ReadAsync();
        csvReader.ReadHeader();

        while (await csvReader.ReadAsync())
        {
            var obj = this.DataMapper!.MapEntity(csvReader);

            if (obj is not null)
            {
                this.DataRows.Add(obj);
            }
        }
    }

    /// <summary>
    /// Fills the <see cref="DataRows"/> property by reading the CSV content from a string.
    /// </summary>
    /// <param name="content">In-memory content to use to fill the <see cref="DataRows"/> property.</param>
    public void LoadDataRowsFromString(string content)
    {
        // Validate "In" Properties
        ValidateDataMapper(this.DataMapper);

        // Reset "Out" Properties
        this.DataRows = new List<T>();

        // Fill Data Rows from Content
        using TextReader textReader = new StringReader(content);
        using CsvReader csvReader = new CsvReader(textReader, CultureInfo.InvariantCulture);

        csvReader.Read();
        csvReader.ReadHeader();

        while (csvReader.Read())
        {
            var obj = this.DataMapper!.MapEntity(csvReader);

            if (obj is not null)
            {
                this.DataRows.Add(obj);
            }
        }
    }

    /// <summary>
    /// Asynchronously fills the <see cref="DataRows"/> property by reading the CSV content from a string.
    /// </summary>
    /// <param name="content">In-memory content to use to fill the <see cref="DataRows"/> property.</param>
    /// <returns>Task that represents the asynchronous operation.</returns>
    public async Task LoadDataRowsFromStringAsync(string content)
    {
        // Validate "In" Properties
        ValidateDataMapper(this.DataMapper);

        // Reset "Out" Properties
        this.DataRows = new List<T>();

        // Fill Data Rows from Content
        using TextReader textReader = new StringReader(content);
        using CsvReader csvReader = new CsvReader(textReader, CultureInfo.InvariantCulture);

        await csvReader.ReadAsync();
        csvReader.ReadHeader();

        while (await csvReader.ReadAsync())
        {
            var obj = this.DataMapper!.MapEntity(csvReader);

            if (obj is not null)
            {
                this.DataRows.Add(obj);
            }
        }
    }

    /// <summary>
    /// Saves the file content to the file path specified in <see cref="IoFile(IoFileInfo)"/>.
    /// </summary>
    /// <returns>Path to the file that was saved.</returns>
    public string Save()
    {
        return this.Save(GetDefaultConfiguration());
    }

    /// <summary>
    /// Saves the file content to the file path specified in <see cref="IoFile(IoFileInfo)"/>.
    /// </summary>
    /// <param name="configuration">CsvHelper configuration used in the export process.</param>
    /// <returns>Path to the file that was saved.</returns>
    public string Save(CsvConfiguration configuration)
    {
        return this.Save(
            this.ExportDataRowsAsContentString(configuration));
    }

    /// <summary>
    /// Asynchronously saves the file content to the file path specified in <see cref="IoFile(IoFileInfo)"/>.
    /// </summary>
    /// <returns>Task that represents the asynchronous operation. The task result contains the path to the file that was saved.</returns>
    public async Task<string> SaveAsync()
    {
        return await this.SaveAsync(GetDefaultConfiguration());
    }

    /// <summary>
    /// Asynchronously saves the file content to the file path specified in <see cref="IoFile(IoFileInfo)"/>.
    /// </summary>
    /// <param name="configuration">CsvHelper configuration used in the export process.</param>
    /// <returns>Task that represents the asynchronous operation. The task result contains the path to the file that was saved.</returns>
    public async Task<string> SaveAsync(CsvConfiguration configuration)
    {
        return await this.SaveAsync(
            this.ExportDataRowsAsContentString(configuration));
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Default CsvHelper configuration used in the export process.
    /// </summary>
    /// <returns>Default CSV configuration.</returns>
    private static CsvConfiguration GetDefaultConfiguration()
    {
        return new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            Delimiter = ",",
            Encoding = Encoding.UTF8,
        };
    }

    /// <summary>
    /// Validates the Data Mapper.
    /// </summary>
    /// <param name="dataMapper">Data mapper used to turn lines in the CSV into a <see cref="IList{T}"/>.</param>
    /// <exception cref="ArgumentNullException">Data Mapper is null.</exception>
    private static void ValidateDataMapper(ICsvEntityMapper<T>? dataMapper)
    {
        ArgumentNullException.ThrowIfNull(dataMapper);
    }

    #endregion Private Methods
}