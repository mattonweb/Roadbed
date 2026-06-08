namespace Roadbed.Logging;

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;

/// <summary>
/// Extension methods that wire the OpenTelemetry logging pipeline into a
/// host's <see cref="ILoggingBuilder"/>.
/// </summary>
/// <remarks>
/// <para>
/// Call this after the host has registered <see cref="LoggingOptions"/> and
/// <see cref="ILoggingDatabaseFactory"/>, and ideally after
/// <c>InstallModulesInAppDomain</c> has wired the
/// <see cref="LoggingChannel"/> and repositories. The DI graph captured at
/// host build time resolves the channel from the same singleton everyone
/// else uses.
/// </para>
/// </remarks>
public static class LoggingBuilderExtensions
{
    #region Public Methods

    /// <summary>
    /// Registers the OpenTelemetry MEL provider with batching and the
    /// Roadbed.Logging database exporter.
    /// </summary>
    /// <param name="builder">The host's logging builder.</param>
    /// <param name="configureOptions">Optional callback that lets the host tweak the underlying <see cref="OpenTelemetryLoggerOptions"/> (e.g. to switch scope inclusion off).</param>
    /// <returns>The same <paramref name="builder"/> for fluent chaining.</returns>
    public static ILoggingBuilder AddRoadbedDbLogging(
        this ILoggingBuilder builder,
        Action<OpenTelemetryLoggerOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddOpenTelemetry(options =>
        {
            options.IncludeScopes = true;
            options.IncludeFormattedMessage = true;
            options.ParseStateValues = true;
            configureOptions?.Invoke(options);
        });

        // Use the synchronous simple processor rather than the batch
        // processor. The exporter is intentionally cheap: it maps the record
        // and hands it to the in-process logging channel, and the background
        // log-writer hosted service performs the real async batched insert off
        // the hot path. An OTel-level batch processor would only add a
        // redundant second buffer.
        //
        // More importantly, the batch processor runs the export on a
        // background drain thread where the ambient diagnostic activity is no
        // longer current. The exporter resolves the activity id column from
        // that ambient activity's roadbed activity-id tag — the same activity
        // that feeds the trace and span id columns — so the export must run in
        // the emitting execution context. The simple processor exports
        // synchronously on the thread that logged, giving the activity id
        // column the same coverage as the trace and span id columns,
        // including the caller's own log lines inside a logging activity
        // scope.
        builder.Services
            .AddOpenTelemetry()
            .WithLogging(logging => logging.AddProcessor(sp =>
                new SimpleLogRecordExportProcessor(
                    new RoadbedDbLogRecordExporter(
                        channelAccessor: () => sp.GetRequiredService<LoggingChannel>(),
                        options: sp.GetRequiredService<LoggingOptions>()))));

        return builder;
    }

    #endregion Public Methods
}
