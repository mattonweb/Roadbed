namespace Roadbed.Common.Installers;

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// Installer for Extensions Logging services.
/// </summary>
public class InstallExtensionsLogging : IServiceCollectionInstaller
{
    /// <inheritdoc/>
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Add logging services to the IServiceCollection
        services.AddLogging();

        // Build the service provider
        IServiceProvider serviceProvider = services.BuildServiceProvider();

        // Manually get the ILoggerFactory instance
        ILoggerFactory loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

        // Register the ILoggerFactory instance in Dependency Injection
        services.AddSingleton<ILoggerFactory>(loggerFactory);

        // Capture point-in-time snapshot in ServiceLocator. This allows the class library
        // to be self-contained (as a NuGet package) without depending on the consuming application
        // to do anything extra besides registering the middleware using one of the methods in
        // the Roadbed.Common.ServiceCollectionExtensions class.
        ServiceLocator.SetLocatorProvider(serviceProvider);
    }
}