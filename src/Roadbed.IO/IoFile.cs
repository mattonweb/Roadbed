/*
 * The namespace Roadbed.IO.Entities was removed on purpose and replaced with Roadbed.IO so that no additional using statements are required.
 */

namespace Roadbed.IO;

using System;
using System.IO;
using System.Threading.Tasks;

/// <summary>
/// Base Entity for IO File related operations.
/// </summary>
public abstract class IoFile
{
    #region Protected Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="IoFile"/> class.
    /// </summary>
    protected IoFile()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IoFile"/> class.
    /// </summary>
    /// <param name="fileInfo">System information about the file.</param>
    protected IoFile(IoFileInfo fileInfo)
    {
        this.FileInfo = fileInfo;
    }

    #endregion Protected Constructors

    #region Public Properties

    /// <summary>
    /// Gets or sets the File Info.
    /// </summary>
    public IoFileInfo? FileInfo { get; set; }

    #endregion Public Properties

    #region Public Methods

    /// <summary>
    /// Validates the file info for files.
    /// </summary>
    /// <param name="fileInfo">File information to validate.</param>
    /// <exception cref="ArgumentNullException">File info is null or file extension is null or empty.</exception>
    public static void ValidateFileInfo(IoFileInfo? fileInfo)
    {
        ArgumentNullException.ThrowIfNull(fileInfo);

        if (string.IsNullOrWhiteSpace(fileInfo.Extension))
        {
            throw new ArgumentNullException(nameof(fileInfo), "File extension is null or empty.");
        }
    }

    /// <summary>
    /// Saves the file content to the file path specified in <see cref="IoFile(IoFileInfo)"/>.
    /// </summary>
    /// <param name="fileContent">Content of the file to be saved.</param>
    /// <returns>Path to the file that was saved.</returns>
    public string Save(string fileContent)
    {
        // Validate "In" Properties
        ValidateFileInfo(this.FileInfo!);

        if (string.IsNullOrWhiteSpace(fileContent))
        {
            return string.Empty;
        }

        using StreamWriter streamWriter = new StreamWriter(this.FileInfo!.FullPath!);
        streamWriter.Write(fileContent);

        return this.FileInfo!.FullPath!;
    }

    /// <summary>
    /// Asynchronously saves the file content to the file path specified in <see cref="IoFile(IoFileInfo)"/>.
    /// </summary>
    /// <param name="fileContent">Content of the file to be saved.</param>
    /// <returns>Task that represents the asynchronous operation. The task result contains the path to the file that was saved.</returns>
    public async Task<string> SaveAsync(string fileContent)
    {
        // Validate "In" Properties
        ValidateFileInfo(this.FileInfo!);

        if (string.IsNullOrWhiteSpace(fileContent))
        {
            return string.Empty;
        }

        await using StreamWriter streamWriter = new StreamWriter(this.FileInfo!.FullPath!);
        await streamWriter.WriteAsync(fileContent);

        return this.FileInfo!.FullPath!;
    }

    #endregion Public Methods
}