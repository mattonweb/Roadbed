namespace Roadbed.Test.Unit.Security.Pin;

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roadbed.Security.Pin;
using Roadbed.Security.Pin.Installers;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="InstallSecurityPin"/> class.
/// </summary>
[TestClass]
public sealed class InstallSecurityPinTests
{
    #region Private Fields

    /// <summary>
    /// RFC 6238 Appendix B: at T = 59 seconds, the 32-byte ASCII key gives this
    /// 8-digit HMAC-SHA256 TOTP with a 30-second window.
    /// </summary>
    private const int Rfc6238PinAtFiftyNineSeconds = 46119246;

    #endregion Private Fields

    #region Public Methods

    /// <summary>
    /// Unit test to verify that the installed service uses the options and the
    /// clock the host registered before the installer ran.
    /// </summary>
    [TestMethod]
    public void ConfigureServices_HostOptionsRegisteredFirst_UsesHostOptionsAndClock()
    {
        // Arrange (Given)
        ServiceCollection services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(new FakeTimeProvider(DateTimeOffset.FromUnixTimeSeconds(59)));
        services.AddSingleton(CreateRfcOptions());

        // Act (When)
        new InstallSecurityPin().ConfigureServices(services, new ConfigurationBuilder().Build());
        int pin = GeneratePin(services);

        // Assert (Then)
        Assert.AreEqual(
            Rfc6238PinAtFiftyNineSeconds,
            pin,
            "The installed PinService should use the host's options and fake clock.");
    }

    /// <summary>
    /// Unit test to verify that options the host registers after the installer
    /// ran are still the ones the service uses.
    /// </summary>
    [TestMethod]
    public void ConfigureServices_HostOptionsRegisteredAfter_UsesHostOptions()
    {
        // Arrange (Given)
        ServiceCollection services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(new FakeTimeProvider(DateTimeOffset.FromUnixTimeSeconds(59)));

        // Act (When)
        new InstallSecurityPin().ConfigureServices(services, new ConfigurationBuilder().Build());
        services.AddSingleton(CreateRfcOptions());
        int pin = GeneratePin(services);

        // Assert (Then)
        Assert.AreEqual(
            Rfc6238PinAtFiftyNineSeconds,
            pin,
            "Options registered after the installer should still reach PinService.");
    }

    /// <summary>
    /// Unit test to verify that the service falls back to the default options
    /// when the host registered none.
    /// </summary>
    [TestMethod]
    public void ConfigureServices_NoHostOptions_UsesDefaultOptions()
    {
        // Arrange (Given)
        FakeTimeProvider timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 11, 10, 14, 23, 17, TimeSpan.Zero));
        ServiceCollection services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(timeProvider);
        int expectedPin = new PinService(new SecurityPinOptions(), timeProvider).Generate();

        // Act (When)
        new InstallSecurityPin().ConfigureServices(services, new ConfigurationBuilder().Build());
        int pin = GeneratePin(services);

        // Assert (Then)
        Assert.AreEqual(
            expectedPin,
            pin,
            "Without host options, PinService should use the default options.");
    }

    /// <summary>
    /// Unit test to verify that the installer registers the system clock when
    /// the host registered none.
    /// </summary>
    [TestMethod]
    public void ConfigureServices_NoClockRegistered_RegistersSystemClock()
    {
        // Arrange (Given)
        ServiceCollection services = new ServiceCollection();

        // Act (When)
        new InstallSecurityPin().ConfigureServices(services, new ConfigurationBuilder().Build());
        TimeProvider registered;
        using (ServiceProvider provider = services.BuildServiceProvider())
        {
            registered = provider.GetRequiredService<TimeProvider>();
        }

        // Assert (Then)
        Assert.AreSame(
            TimeProvider.System,
            registered,
            "The installer should register TimeProvider.System when no clock is registered.");
    }

    /// <summary>
    /// Unit test to verify that a null service collection is rejected.
    /// </summary>
    [TestMethod]
    public void ConfigureServices_NullServices_ThrowsArgumentNullException()
    {
        // Arrange (Given)
        IServiceCollection? services = null;
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            new InstallSecurityPin().ConfigureServices(services!, new ConfigurationBuilder().Build());
        }
        catch (ArgumentNullException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "ConfigureServices should throw ArgumentNullException when services is null.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates options that reproduce the RFC 6238 Appendix B HMAC-SHA256 vectors.
    /// </summary>
    /// <returns>The options.</returns>
    private static SecurityPinOptions CreateRfcOptions()
    {
        return new SecurityPinOptions
        {
            SecretKeyWithPatternSupport = "12345678901234567890123456789012",
            WindowInSeconds = 30,
            PinLength = 8,
        };
    }

    /// <summary>
    /// Builds the service provider, resolves <see cref="PinService"/> and generates a PIN.
    /// </summary>
    /// <param name="services">The configured service collection.</param>
    /// <returns>The generated PIN.</returns>
    private static int GeneratePin(ServiceCollection services)
    {
        using (ServiceProvider provider = services.BuildServiceProvider())
        {
            return provider.GetRequiredService<PinService>().Generate();
        }
    }

    #endregion Private Methods
}
