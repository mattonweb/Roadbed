/*
 * The namespace Roadbed.IO.Dtos was removed on purpose and replaced with Roadbed.IO so that no additional using statements are required.
 */

namespace Roadbed.IO;

using System;
using System.IO;

/// <summary>
/// System File Data Transfer Object (DTO).
/// </summary>
/// <remarks>
/// This is a simplified version of <see cref="System.IO.FileInfo"/> for data transfer purposes.
/// </remarks>
public class IoFileInfo
{
    #region Private Fields

    /// <summary>
    /// Container for the public property FileInfo.
    /// </summary>
    private FileInfo? _fileInfo;

    #endregion Private Fields

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="IoFileInfo"/> class.
    /// </summary>
    public IoFileInfo()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IoFileInfo"/> class.
    /// </summary>
    /// <param name="path">Full Path of the file.</param>
    public IoFileInfo(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        this.FullPath = path;
    }

    #endregion Public Constructors

    #region Public Properties

    /// <summary>
    /// Gets the Extension of the file.
    /// </summary>
    public string? Extension
    {
        get
        {
            if (string.IsNullOrWhiteSpace(this.FullPath) || this._fileInfo is null)
            {
                return null;
            }

            return this._fileInfo.Extension;
        }
    }

    /// <summary>
    /// Gets the full version of <see cref="System.IO.FileInfo"/>.
    /// </summary>
    public FileInfo? FileInfo
    {
        get => this._fileInfo;
        internal set => this._fileInfo = value;
    }

    /// <summary>
    /// Gets or sets the Full Path of the file.
    /// </summary>
    public string? FullPath
    {
        get => this._fileInfo?.FullName;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                this._fileInfo = null;
            }
            else
            {
                this._fileInfo = new FileInfo(value);
            }
        }
    }

    #endregion Public Properties
}