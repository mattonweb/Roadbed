namespace Roadbed.Net.Installers;

using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Installer for Net HTTP Client services.
/// </summary>
public class InstallNetHttpClient : IServiceCollectionInstaller
{
    #region Public Methods

    /// <inheritdoc/>
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Configure default HTTP client without compression
        services.AddHttpClient("DefaultClient")
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.None,
            });

        // Configure HTTP client with compression enabled
        services.AddHttpClient("CompressedClient")
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip,
            });

        // Register INetHttpClient for dependency injection
        services.AddScoped<INetHttpClient, NetHttpClient>();

        // Capture point-in-time snapshot in ServiceLocator. This allows the class library
        // to be self-contained (as a NuGet package) without depending on the consuming application
        // to do anything extra besides registering the middleware using one of the methods in
        // the Roadbed.Common.ServiceCollectionExtensions class.
        ServiceLocator.SetLocatorProvider(services.BuildServiceProvider());
    }

    #endregion Public Methods
}