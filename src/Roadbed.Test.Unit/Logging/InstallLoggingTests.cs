namespace Roadbed.Test.Unit.Logging;

using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Roadbed.Data;
using Roadbed.Logging;
using Roadbed.Logging.Installers;

/// <summary>
/// Unit tests for <see cref="InstallLogging"/>, focused on the channel-
/// sharing guarantee that makes the host writer and any
/// <c>ServiceLocator</c>-resolved producer meet around one
/// <see cref="LoggingChannel"/>.
/// </summary>
[TestClass]
public class InstallLoggingTests
{
    #region Public Methods

    /// <summary>
    /// Verifies that two distinct <see cref="System.IServiceProvider"/>
    /// instances built from the same <see cref="IServiceCollection"/>
    /// resolve the SAME <see cref="LoggingChannel"/> object — the
    /// guarantee that lets host-resolved and
    /// <c>ServiceLocator</c>-resolved producers enqueue into one queue.
    /// </summary>
    [TestMethod]
    public void ConfigureServices_LoggingChannel_IsSharedAcrossContainers()
    {
        // Arrange (Given)
        var services = new ServiceCollection();
        services.AddSingleton(new LoggingOptions { Application = "test" });
        services.AddSingleton<ILoggingDatabaseFactory>(BuildFactory(DataConnectionStringType.MySQL));

        var installer = new InstallLogging();

        // Act (When)
        installer.ConfigureServices(services, new ConfigurationBuilder().Build());

        using var providerA = services.BuildServiceProvider();
        using var providerB = services.BuildServiceProvider();

        var channelA = providerA.GetRequiredService<LoggingChannel>();
        var channelB = providerB.GetRequiredService<LoggingChannel>();

        // Assert (Then)
        Assert.AreSame(
            channelA,
            channelB,
            "LoggingChannel must be a singleton instance shared across every IServiceProvider built from the same IServiceCollection.");
    }

    /// <summary>
    /// Verifies that calling <see cref="InstallLogging.ConfigureServices"/>
    /// more than once does not register multiple
    /// <see cref="LoggingChannel"/> instances. Auto-discovery in a
    /// multi-host or test scenario can fire the installer twice; the
    /// channel descriptor must remain a single shared instance.
    /// </summary>
    [TestMethod]
    public void ConfigureServices_RunTwice_LoggingChannelStaysShared()
    {
        // Arrange (Given)
        var services = new ServiceCollection();
        services.AddSingleton(new LoggingOptions { Application = "test" });
        services.AddSingleton<ILoggingDatabaseFactory>(BuildFactory(DataConnectionStringType.SQLite));

        var installer = new InstallLogging();

        // Act (When) - run the installer twice over the same services
        installer.ConfigureServices(services, new ConfigurationBuilder().Build());
        installer.ConfigureServices(services, new ConfigurationBuilder().Build());

        using var provider = services.BuildServiceProvider();
        var channel = provider.GetRequiredService<LoggingChannel>();

        // Assert (Then) - exactly one descriptor; resolving it produces a single instance.
        int channelDescriptorCount = 0;
        foreach (var descriptor in services)
        {
            if (descriptor.ServiceType == typeof(LoggingChannel))
            {
                channelDescriptorCount++;
            }
        }

        Assert.AreEqual(
            1,
            channelDescriptorCount,
            "Double-running the installer must not register a second LoggingChannel descriptor.");
        Assert.IsNotNull(channel);
    }

    /// <summary>
    /// Verifies that <see cref="LoggingChannel"/> resolved before any
    /// <c>ServiceLocator</c> snapshot is taken still matches the instance
    /// resolved after — proving the descriptor pins one object regardless
    /// of when each container is built.
    /// </summary>
    [TestMethod]
    public void ConfigureServices_ChannelResolvedBeforeAndAfter_AreSameInstance()
    {
        // Arrange (Given)
        var services = new ServiceCollection();
        services.AddSingleton(new LoggingOptions { Application = "test" });
        services.AddSingleton<ILoggingDatabaseFactory>(BuildFactory(DataConnectionStringType.MySQL));

        var installer = new InstallLogging();
        installer.ConfigureServices(services, new ConfigurationBuilder().Build());

        // Act (When) - resolve before, then build a new provider, then resolve again.
        using var earlyProvider = services.BuildServiceProvider();
        var earlyChannel = earlyProvider.GetRequiredService<LoggingChannel>();

        using var lateProvider = services.BuildServiceProvider();
        var lateChannel = lateProvider.GetRequiredService<LoggingChannel>();

        // Assert (Then)
        Assert.AreSame(earlyChannel, lateChannel);
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Builds a mock <see cref="ILoggingDatabaseFactory"/> whose connection
    /// reports the supplied type. Tests that exercise installer wiring
    /// only need the factory to advertise a supported provider; no live
    /// connection is ever opened.
    /// </summary>
    /// <param name="type">The connection-string type to advertise.</param>
    /// <returns>A configured <see cref="ILoggingDatabaseFactory"/> mock.</returns>
    private static ILoggingDatabaseFactory BuildFactory(DataConnectionStringType type)
    {
        var connection = new DataConnecionString(type, "Server=stub");
        var mock = new Mock<ILoggingDatabaseFactory>();
        mock.SetupGet(f => f.Connecion).Returns(connection);
        mock.Setup(f => f.CreateOpenConnection()).Returns(Mock.Of<IDbConnection>());
        mock.Setup(f => f.CreateOpenConnectionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Mock.Of<IDbConnection>()));
        return mock.Object;
    }

    #endregion Private Methods
}
