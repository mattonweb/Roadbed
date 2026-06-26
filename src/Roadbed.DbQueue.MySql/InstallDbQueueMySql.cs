namespace Roadbed.DbQueue.MySql;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>
/// Auto-discovered installer that selects MySQL/MariaDB as the
/// <see cref="Roadbed.DbQueue"/> backing provider.
/// </summary>
/// <remarks>
/// <para>
/// Registers the MySQL implementation of <c>IDbQueueDataExecutor</c> and
/// captures the service provider into <see cref="ServiceLocator"/> so the
/// public <see cref="QueueProcessor{T}"/> constructor — which resolves the
/// executor via <see cref="ServiceLocator.GetService{T}"/> — sees it.
/// </para>
/// <para>
/// This installer does <strong>not</strong> register any
/// <see cref="QueueDefinition{T}"/> or marker
/// <see cref="Roadbed.Data.IDataConnectionFactory"/>: those are host-owned,
/// per queue, because they carry schema-specific connections. The host
/// constructs each <see cref="QueueProcessor{T}"/> (or a typed wrapper) with
/// the queue's definition.
/// </para>
/// </remarks>
public sealed class InstallDbQueueMySql : IServiceCollectionInstaller
{
    #region Public Methods

    /// <inheritdoc/>
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton<IDbQueueDataExecutor, MySqlDbQueueDataExecutor>();

        ServiceLocator.SetLocatorProvider(services.BuildServiceProvider());
    }

    #endregion Public Methods
}
