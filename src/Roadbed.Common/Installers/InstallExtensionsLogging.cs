namespace Roadbed.Common.Installers;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Installer for Extensions Logging services.
/// </summary>
/// <remarks>
/// <para>
/// Registers the standard <c>Microsoft.Extensions.Logging</c> infrastructure
/// (<c>ILoggerFactory</c> + <c>ILogger&lt;T&gt;</c>) into the service
/// collection and snapshots the resulting provider into
/// <see cref="ServiceLocator"/>.
/// </para>
/// <para>
/// This installer deliberately does <strong>not</strong> resolve
/// <c>ILoggerFactory</c> eagerly. Earlier versions built a throwaway
/// provider, captured its <c>ILoggerFactory</c> instance, and
/// re-registered it as a singleton — which (a) realized the
/// OpenTelemetry logging pipeline against the throwaway container,
/// orphaning any
/// <c>builder.Logging.AddRoadbedDbLogging()</c>-registered exporter from
/// the host's runtime singletons, and (b) crashed at startup when the
/// exporter's factory tried to resolve services that had not yet been
/// registered by later installers in the discovery chain. Letting the
/// host build its own <c>ILoggerFactory</c> on demand attaches every MEL
/// provider to the host's real DI graph.
/// </para>
/// </remarks>
public class InstallExtensionsLogging : IServiceCollectionInstaller
{
    /// <inheritdoc/>
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Register MEL infrastructure. AddLogging is idempotent — calling
        // it more than once is harmless.
        services.AddLogging();

        // Capture point-in-time snapshot in ServiceLocator so this library
        // works as a self-contained NuGet package. Use a fresh provider here
        // (not one repurposed for ILoggerFactory resolution) to avoid
        // forcing eager realization of any logger provider configured
        // earlier in the pipeline (notably the OpenTelemetry MEL provider
        // wired by Roadbed.Logging.AddRoadbedDbLogging).
        ServiceLocator.SetLocatorProvider(services.BuildServiceProvider());
    }
}