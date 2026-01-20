namespace Roadbed.Scheduling.Installers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

/// <summary>
/// Installer for Quartz.NET Scheduling services.
/// </summary>
public class InstallScheduling : IServiceCollectionInstaller
{
    #region Public Methods

    /// <inheritdoc/>
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Discover and register all ISchedulingJob implementations from all loaded assemblies
        RegisterSchedulingJobs(services);

        // Configure Quartz.NET
        services.AddQuartz(q =>
        {
            // Use in-memory job store
            q.UseInMemoryStore();

            // Configure all discovered jobs
            ConfigureQuartzJobs(q, services);
        });

        // Add Quartz hosted service to run in the background
        services.AddQuartzHostedService(options =>
        {
            // Wait for jobs to complete on shutdown
            options.WaitForJobsToComplete = true;
        });

        // Capture point-in-time snapshot in ServiceLocator. This allows the class library
        // to be self-contained (as a NuGet package) without depending on the consuming application
        // to do anything extra besides registering the middleware using one of the methods in
        // the Roadbed.Common.ServiceCollectionExtensions class.
        ServiceLocator.SetLocatorProvider(services.BuildServiceProvider());
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Applies misfire strategy to cron schedule builder.
    /// </summary>
    /// <param name="builder">Cron schedule builder.</param>
    /// <param name="strategy">Misfire strategy to apply.</param>
    private static void ApplyMisfireStrategy(CronScheduleBuilder builder, SchedulingMisfireStrategy strategy)
    {
        switch (strategy)
        {
            case SchedulingMisfireStrategy.Default:
                // Quartz uses smart defaults
                break;

            case SchedulingMisfireStrategy.DoNothing:
                builder.WithMisfireHandlingInstructionDoNothing();
                break;

            case SchedulingMisfireStrategy.FireAndProceed:
                builder.WithMisfireHandlingInstructionFireAndProceed();
                break;

            case SchedulingMisfireStrategy.IgnoreMisfires:
                builder.WithMisfireHandlingInstructionIgnoreMisfires();
                break;
        }
    }

    /// <summary>
    /// Applies misfire strategy to simple schedule builder.
    /// </summary>
    /// <param name="builder">Simple schedule builder.</param>
    /// <param name="strategy">Misfire strategy to apply.</param>
    private static void ApplySimpleMisfireStrategy(SimpleScheduleBuilder builder, SchedulingMisfireStrategy strategy)
    {
        switch (strategy)
        {
            case SchedulingMisfireStrategy.Default:
                // Quartz uses smart defaults
                break;

            case SchedulingMisfireStrategy.FireNow:
                builder.WithMisfireHandlingInstructionFireNow();
                break;

            case SchedulingMisfireStrategy.IgnoreMisfires:
                builder.WithMisfireHandlingInstructionIgnoreMisfires();
                break;

            case SchedulingMisfireStrategy.NextWithExistingCount:
                builder.WithMisfireHandlingInstructionNextWithExistingCount();
                break;

            case SchedulingMisfireStrategy.NextWithRemainingCount:
                builder.WithMisfireHandlingInstructionNextWithRemainingCount();
                break;

            case SchedulingMisfireStrategy.NowWithExistingCount:
                builder.WithMisfireHandlingInstructionNowWithExistingCount();
                break;

            case SchedulingMisfireStrategy.NowWithRemainingCount:
                builder.WithMisfireHandlingInstructionNowWithRemainingCount();
                break;
        }
    }

    /// <summary>
    /// Configures all discovered jobs in Quartz.
    /// </summary>
    /// <param name="configurator">Quartz configurator.</param>
    /// <param name="services">Service collection to build service provider from.</param>
    private static void ConfigureQuartzJobs(IServiceCollectionQuartzConfigurator configurator, IServiceCollection services)
    {
        // Build a temporary service provider to get job instances for configuration
        var serviceProvider = services.BuildServiceProvider();
        var jobs = serviceProvider.GetServices<ISchedulingJob>();

        foreach (var job in jobs)
        {
            var jobType = job.GetType();
            var jobKey = new JobKey(job.Name, job.Schedule.GroupName);

            // Add the job to Quartz
            configurator.AddJob(jobType, jobKey, j => j
                .WithDescription(job.Description));

            // Create trigger based on schedule type
            configurator.AddTrigger(t => ConfigureTrigger(t, jobKey, job.Schedule));
        }
    }

    /// <summary>
    /// Configures a trigger based on the schedule configuration.
    /// </summary>
    /// <param name="trigger">Trigger configurator.</param>
    /// <param name="jobKey">Job key for the trigger.</param>
    /// <param name="schedule">Schedule configuration.</param>
    private static void ConfigureTrigger(
        ITriggerConfigurator trigger,
        JobKey jobKey,
        SchedulingSchedule schedule)
    {
        trigger.ForJob(jobKey)
            .WithIdentity($"{jobKey.Name}-trigger", jobKey.Group)
            .WithPriority((int)schedule.Priority);

        switch (schedule.ScheduleType)
        {
            case SchedulingScheduleType.Cron:
                trigger.WithCronSchedule(schedule.CronExpression!, b =>
                {
                    b.InTimeZone(schedule.TimeZone);

                    if (schedule.MisfireHandlingEnabled)
                    {
                        ApplyMisfireStrategy(b, schedule.MisfireStrategy);
                    }
                    else
                    {
                        b.WithMisfireHandlingInstructionDoNothing();
                    }
                });
                break;

            case SchedulingScheduleType.SimpleInterval:
                trigger.StartAt(DateTimeOffset.UtcNow.Add(schedule.StartDelay!.Value))
                    .WithSimpleSchedule(b =>
                    {
                        b.WithInterval(schedule.Interval!.Value)
                            .RepeatForever();

                        if (schedule.MaxExecutionCount.HasValue)
                        {
                            b.WithRepeatCount(schedule.MaxExecutionCount.Value - 1); // -1 because first execution doesn't count as repeat
                        }

                        if (schedule.MisfireHandlingEnabled)
                        {
                            ApplySimpleMisfireStrategy(b, schedule.MisfireStrategy);
                        }
                        else
                        {
                            b.WithMisfireHandlingInstructionFireNow();
                        }
                    });
                break;

            case SchedulingScheduleType.SpecificTimeOnce:
                trigger.StartAt(new DateTimeOffset(schedule.StartAt!.Value, schedule.TimeZone.GetUtcOffset(schedule.StartAt.Value)));
                break;

            case SchedulingScheduleType.SpecificTimeWithInterval:
                trigger.StartAt(new DateTimeOffset(schedule.StartAt!.Value, schedule.TimeZone.GetUtcOffset(schedule.StartAt.Value)))
                    .WithSimpleSchedule(b =>
                    {
                        b.WithInterval(schedule.Interval!.Value)
                            .RepeatForever();

                        if (schedule.MaxExecutionCount.HasValue)
                        {
                            b.WithRepeatCount(schedule.MaxExecutionCount.Value - 1);
                        }

                        if (schedule.MisfireHandlingEnabled)
                        {
                            ApplySimpleMisfireStrategy(b, schedule.MisfireStrategy);
                        }
                        else
                        {
                            b.WithMisfireHandlingInstructionFireNow();
                        }
                    });
                break;
        }
    }

    /// <summary>
    /// Discovers and registers all ISchedulingJob implementations across all loaded assemblies.
    /// </summary>
    /// <param name="services">Service collection to register jobs with.</param>
    private static void RegisterSchedulingJobs(IServiceCollection services)
    {
        var visited = new HashSet<string>();
        var jobTypes = new List<Type>();

        // Get all loaded assemblies, excluding system/Microsoft assemblies
        var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !string.IsNullOrEmpty(a.FullName) &&
                        !a.FullName.StartsWith("System.") &&
                        !a.FullName.StartsWith("Microsoft."));

        foreach (var assembly in loadedAssemblies)
        {
            if (string.IsNullOrEmpty(assembly.FullName) || visited.Contains(assembly.FullName))
            {
                continue;
            }

            visited.Add(assembly.FullName);

            try
            {
                var assemblyJobTypes = assembly.GetTypes()
                    .Where(t => typeof(ISchedulingJob).IsAssignableFrom(t))
                    .Where(t => !t.IsInterface && !t.IsAbstract)
                    .Where(t => t.GetConstructors().Any(c => c.GetParameters().Length > 0)); // Has DI constructor

                jobTypes.AddRange(assemblyJobTypes);
            }
            catch (ReflectionTypeLoadException)
            {
                // Skip assemblies that can't be loaded
            }
        }

        // Register all discovered job types
        foreach (var jobType in jobTypes)
        {
            // Register the job as itself (for DI injection)
            services.AddTransient(jobType);

            // Also register as ISchedulingJob for discovery
            services.AddTransient(typeof(ISchedulingJob), jobType);
        }
    }

    #endregion Private Methods
}