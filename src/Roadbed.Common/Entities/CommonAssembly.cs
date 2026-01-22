/*
 * The namespace Roadbed.Common.Entities was removed on purpose and replaced with Roadbed.Common so that no additional using statements are required.
 */

namespace Roadbed.Common;

using System.Reflection;

/// <summary>
/// Service for CommonAssembly related operations.
/// </summary>
public static class CommonAssembly
{
    #region Public Methods

    /// <summary>
    /// Opens an embedded resource and returns the content as text.
    /// </summary>
    /// <param name="assembly">Assembly to use to locate the embedded resource.</param>
    /// <param name="fileAndExtensionWithFullNamespace">Full namespace of the file to read.</param>
    /// <returns>Content of the embedded resource.</returns>
    public static CommonEmbeddedResourceResponse ReadTextResource(Assembly assembly, string fileAndExtensionWithFullNamespace)
    {
        // Null Check
        if (assembly == null)
        {
            return CommonEmbeddedResourceResponse.Failure("Assembly is null.");
        }
        else if (string.IsNullOrEmpty(fileAndExtensionWithFullNamespace))
        {
            return CommonEmbeddedResourceResponse.Failure("File name is null or empty.");
        }

        // Get All Embedded Resource Names
        string[] resourceNames = assembly.GetManifestResourceNames();
        string result = string.Empty;

        // Loop through each resource
        foreach (string name in resourceNames)
        {
            // Ensure the name being passed in matches of resource being passed in (case-insensitive)
            if (name.Equals(fileAndExtensionWithFullNamespace, StringComparison.OrdinalIgnoreCase))
            {
                using (Stream? stream = assembly.GetManifestResourceStream(name)) // Use 'name' instead
                {
                    if (stream != null)
                    {
                        using (StreamReader sr = new StreamReader(stream))
                        {
                            result = sr.ReadToEnd();
                        }
                    }
                }

                // Stop Loop
                break;
            }
        }

        // Return Result
        if (string.IsNullOrEmpty(result))
        {
            return CommonEmbeddedResourceResponse.Failure("File not found or it is empty.");
        }
        else
        {
            return CommonEmbeddedResourceResponse.Success(result);
        }
    }

    #endregion Public Methods
}