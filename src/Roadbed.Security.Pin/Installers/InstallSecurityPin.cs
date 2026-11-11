namespace Roadbed.Security.Pin.Installers;

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>
/// Installer for <see cref="PinService"/>.
/// </summary>
/// <remarks>
/// <para>
/// Registers <see cref="PinService"/> as a singleton built from the
/// <see cref="SecurityPinOptions"/> the host registered, or from the public
/// defaults when the host registered none. The options are read when the
/// service is first resolved, so the host's installer may run before or after
/// this one.
/// </para>
/// <para>
/// A host that needs a second <see cref="PinService"/> for another purpose
/// registers it itself, for example as a keyed singleton with its own
/// <see cref="SecurityPinOptions"/>.
/// </para>
/// </remarks>
public class InstallSecurityPin : IServiceCollectionInstaller
{
    #region Public Methods

    /// <inheritdoc/>
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);

        // TimeProvider — the clock PinService reads the current window from.
        // Defaults to the system clock; a test can register a FakeTimeProvider
        // BEFORE this call to override it.
        services.TryAddSingleton<TimeProvider>(TimeProvider.System);

        services.TryAddSingleton<PinService>(provider => new PinService(
            provider.GetService<SecurityPinOptions>() ?? new SecurityPinOptions(),
            provider.GetRequiredService<TimeProvider>()));
    }

    #endregion Public Methods
}
