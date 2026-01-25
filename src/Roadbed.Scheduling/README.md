# Roadbed.Scheduling

This package provides job scheduling capabilities using Quartz.NET with automatic job discovery, registration, and built-in metrics support.

## Quartz.NET

The `Roadbed.Scheduling` project provides integration with [Quartz.NET](https://www.quartz-scheduler.net/), a full-featured, open source job scheduling system that can be used from smallest apps to large scale enterprise systems.

### NuGet Packages

You need the Quartz.NET packages that provide dependency injection and hosting support. Include the following packages, provided by Quartz.NET, in your project:
```xml
<ItemGroup>
  <PackageReference Include="Quartz" Version="*.*.*" />
  <PackageReference Include="Quartz.Extensions.DependencyInjection" Version="*.*.*" />
  <PackageReference Include="Quartz.Extensions.Hosting" Version="*.*.*" />
</ItemGroup>
```

## Application Start-up

The `Roadbed.Common` project provides an extension method that will discover and invoke all `IServiceCollectionInstaller` implementations in the application domain. This will find the `InstallScheduling` implementation provided by `Roadbed.Scheduling`. The installer will then discover all `ISchedulingJob` implementations and register them with dependency injection and Quartz.NET.
```csharp
var builder = WebApplication.CreateBuilder(args);

// This discovers and invokes ALL IServiceCollectionInstaller implementations,
// including InstallScheduling from Roadbed.Scheduling
builder.Services.InstallModulesInAppDomain(builder.Configuration);

var app = builder.Build();
app.Run();
```

### What Happens Automatically

1. ✅ `InstallModulesInAppDomain()` scans assemblies for `IServiceCollectionInstaller` implementations
2. ✅ Finds `InstallScheduling` in `Roadbed.Scheduling`
3. ✅ Creates an instance and calls `ConfigureServices()`
4. ✅ `InstallScheduling` discovers all `ISchedulingJob` implementations across all loaded assemblies
5. ✅ Registers jobs with dependency injection
6. ✅ Configures Quartz.NET with all discovered jobs
7. ✅ Sets up hosted service to run jobs in background
8. ✅ Configures metrics listener for job execution tracking

**Zero manual configuration required!** Your jobs are automatically discovered and scheduled.

---

## Metrics and Monitoring

`Roadbed.Scheduling` includes built-in metrics support to track job execution. By default, metrics are **optional** with zero overhead when not configured.

### Default Behavior (No Metrics)

Without any configuration, jobs run normally with zero metrics overhead:
```csharp
// No additional configuration needed - metrics are optional
builder.Services.InstallModulesInAppDomain(builder.Configuration);
```

### Built-in Logging Metrics

To log job execution metrics to your application logs, register the `LoggingMetricsAdapter`:
```csharp
// Program.cs
builder.Services.AddSingleton<ISchedulingMetrics, LoggingMetricsAdapter>();
builder.Services.InstallModulesInAppDomain(builder.Configuration);
```

**Log Output Example:**
```
info: Job MonthlyBilling (Default) started - FireInstanceId: abc123
info: Job MonthlyBilling (Default) completed in 1234.5ms - Processed 45 customers
```

### Job Result Messages

Jobs can provide execution summaries by setting `Context.Result`:
```csharp
public override async Task ExecuteAsync(CancellationToken cancellationToken)
{
    this.LogInformation("Starting monthly billing");
    
    var customers = await this._billingService.GetActiveCustomersAsync(cancellationToken);
    int processedCount = 0;
    decimal totalRevenue = 0m;
    
    foreach (var customer in customers)
    {
        var invoice = await this._billingService.ProcessBillingAsync(customer, cancellationToken);
        processedCount++;
        totalRevenue += invoice.Amount;
    }
    
    // Set result message - captured by metrics
    this.Context.Result = $"Processed {processedCount}/{customers.Count} customers, total revenue: ${totalRevenue:N2}";
    
    this.LogInformation("Monthly billing completed");
}
```

The result message appears in metrics logs:
```
info: Job MonthlyBilling (Default) completed in 2341.2ms - Processed 45/45 customers, total revenue: $12,345.67
```

### Custom Metrics Adapters

Implement `ISchedulingMetrics` to send metrics to any monitoring system:
```csharp
public class MyCustomMetrics : ISchedulingMetrics
{
    public void JobStarted(JobExecutionInfo info)
    {
        // Send to your monitoring system
    }

    public void JobCompleted(JobExecutionInfo info, TimeSpan duration)
    {
        // Record success metrics
        // info.ResultMessage contains job's result (if set)
    }

    public void JobFailed(JobExecutionInfo info, Exception exception, TimeSpan duration)
    {
        // Record failure metrics
    }

    public void JobMisfired(JobExecutionInfo info)
    {
        // Note: Currently not captured (requires TriggerListener)
        // Reserved for future implementation
    }
}

// Register your custom metrics
builder.Services.AddSingleton<ISchedulingMetrics, MyCustomMetrics>();
```

### ISchedulingMetrics Interface

The metrics interface provides four lifecycle events:
```csharp
public interface ISchedulingMetrics
{
    void JobStarted(JobExecutionInfo info);
    void JobCompleted(JobExecutionInfo info, TimeSpan duration);
    void JobFailed(JobExecutionInfo info, Exception exception, TimeSpan duration);
    void JobMisfired(JobExecutionInfo info);
}
```

**JobExecutionInfo Properties:**
- `JobName`, `JobGroup` - Job identification
- `TriggerName`, `TriggerGroup` - Trigger identification
- `FireInstanceId` - Unique execution ID
- `FireTimeUtc` - Actual execution time
- `ScheduledFireTimeUtc` - Scheduled execution time
- `PreviousFireTimeUtc`, `NextFireTimeUtc` - Previous/next runs
- `ResultMessage` - Optional result set via `Context.Result`

### Metrics Best Practices

**1. Keep metrics operations fast** - Methods are called synchronously
```csharp
public void JobCompleted(JobExecutionInfo info, TimeSpan duration)
{
    // ✅ Good - Queue for async processing
    this._metricsQueue.Enqueue(new MetricEvent(info, duration));
    
    // ❌ Avoid - Slow synchronous operations
    // await this._database.SaveMetricsAsync(info);
}
```

**2. Never throw exceptions** - Metrics failures are logged as warnings and don't break jobs
```csharp
public void JobCompleted(JobExecutionInfo info, TimeSpan duration)
{
    try
    {
        this._monitoring.RecordSuccess(info.JobName, duration);
    }
    catch (Exception ex)
    {
        this._logger.LogWarning(ex, "Failed to record metrics");
        // Don't throw - metrics failures should not break jobs
    }
}
```

**3. Use structured logging** - Better for log aggregation
```csharp
this._logger.LogInformation(
    "Job {JobName} completed in {DurationMs}ms - {Result}",
    info.JobName,
    duration.TotalMilliseconds,
    info.ResultMessage);
```

---

## Creating Scheduled Jobs

There are two ways to create scheduled jobs: **hardcoded schedules** (via constructor) and **configuration-driven schedules** (via property overrides).

### Method 1: Hardcoded Schedule (Constructor Pattern)

Use this approach when your schedule is fixed and part of the job's definition.
```csharp
namespace MyApp.Jobs;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Roadbed.Scheduling;

/// <summary>
/// Example scheduled job with hardcoded schedule.
/// </summary>
public class FooJob : BaseSchedulingJob<FooJob>
{
    #region Private Fields

    private readonly IFooService _fooService;

    #endregion Private Fields

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="FooJob"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <param name="fooService">Service for foo operations.</param>
    /// <exception cref="ArgumentNullException">Thrown when logger or fooService is null.</exception>
    public FooJob(
        ILogger<FooJob> logger,
        IFooService fooService)
        : base(
            name: "FooJob",
            description: "Processes foo items every 30 minutes",
            schedule: new SchedulingSchedule(TimeSpan.FromMinutes(30))
            {
                GroupName = "MyApp",
                Priority = SchedulingJobPriority.Normal,
            },
            logger: logger)
    {
        ArgumentNullException.ThrowIfNull(fooService);
        this._fooService = fooService;
    }

    #endregion Public Constructors

    #region Public Methods

    /// <inheritdoc/>
    public override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        this.LogInformation("Starting foo processing");

        try
        {
            int processedCount = await this._fooService.ProcessFooItemsAsync(cancellationToken);
            
            // Set result message for metrics
            this.Context.Result = $"Processed {processedCount} items";
            
            this.LogInformation("Foo processing completed successfully");
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Error processing foo items");
            throw;
        }
    }

    #endregion Public Methods
}
```

### Method 2: Configuration-Driven Schedule (Property Override Pattern)

Use this approach when you want to configure schedules through `appsettings.json` without recompiling.
```csharp
namespace MyApp.Jobs;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roadbed.Scheduling;

/// <summary>
/// Example scheduled job with configuration-driven schedule.
/// </summary>
public class FooJob : BaseSchedulingJob<FooJob>
{
    #region Private Fields

    private readonly IConfiguration _configuration;
    private readonly IFooService _fooService;
    private SchedulingSchedule? _schedule;

    #endregion Private Fields

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="FooJob"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="fooService">Service for foo operations.</param>
    /// <exception cref="ArgumentNullException">Thrown when logger, configuration, or fooService is null.</exception>
    public FooJob(
        ILogger<FooJob> logger,
        IConfiguration configuration,
        IFooService fooService)
        : base(logger)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(fooService);

        this._configuration = configuration;
        this._fooService = fooService;
    }

    #endregion Public Constructors

    #region Public Properties

    /// <inheritdoc/>
    public override string Description => "Processes foo items at configurable intervals";

    /// <inheritdoc/>
    public override string Name => "FooJob";

    /// <inheritdoc/>
    public override SchedulingSchedule Schedule
    {
        get
        {
            if (this._schedule != null)
            {
                return this._schedule;
            }

            // Read from configuration with defaults
            var intervalMinutes = this._configuration.GetValue<int>("Jobs:FooJob:IntervalMinutes", 30);
            var startDelaySeconds = this._configuration.GetValue<int>("Jobs:FooJob:StartDelaySeconds", 0);
            var priority = this._configuration.GetValue<SchedulingJobPriority>("Jobs:FooJob:Priority", SchedulingJobPriority.Normal);

            this._schedule = new SchedulingSchedule(
                TimeSpan.FromMinutes(intervalMinutes),
                TimeSpan.FromSeconds(startDelaySeconds))
            {
                GroupName = "MyApp",
                Priority = priority
            };

            return this._schedule;
        }
    }

    #endregion Public Properties

    #region Public Methods

    /// <inheritdoc/>
    public override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        this.LogInformation("Starting foo processing");

        try
        {
            int processedCount = await this._fooService.ProcessFooItemsAsync(cancellationToken);
            
            // Set result message for metrics
            this.Context.Result = $"Processed {processedCount} items";
            
            this.LogInformation("Foo processing completed successfully");
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Error processing foo items");
            throw;
        }
    }

    #endregion Public Methods
}
```

### Configuration File (appsettings.json)

When using the configuration-driven approach, add settings to your `appsettings.json`:
```json
{
  "Jobs": {
    "FooJob": {
      "IntervalMinutes": 30,
      "StartDelaySeconds": 0,
      "Priority": "Normal"
    }
  }
}
```

---

## Schedule Types

`SchedulingSchedule` supports multiple schedule types:

### 1. Cron Expression
```csharp
// Run every day at 2:30 AM
new SchedulingSchedule("0 30 2 * * ?")
{
    TimeZone = TimeZoneInfo.Local
}
```

### 2. Simple Interval
```csharp
// Run every 15 minutes, starting immediately
new SchedulingSchedule(TimeSpan.FromMinutes(15))

// Run every hour, starting after 10 minutes
new SchedulingSchedule(TimeSpan.FromHours(1), TimeSpan.FromMinutes(10))
```

### 3. Specific Time (One-Time)
```csharp
// Run once at a specific date/time
new SchedulingSchedule(new DateTime(2026, 1, 20, 14, 30, 0))
```

### 4. Specific Time with Interval
```csharp
// Run every 30 minutes starting at a specific time
new SchedulingSchedule(
    new DateTime(2026, 1, 20, 14, 30, 0),
    TimeSpan.FromMinutes(30))
```

---

## Schedule Configuration Properties

All schedule types support these configuration properties:
```csharp
new SchedulingSchedule("0 0 8 * * ?")
{
    TimeZone = TimeZoneInfo.Local,                    // Default: UTC
    GroupName = "MyApp",                               // Default: "Default"
    Priority = SchedulingJobPriority.High,             // Default: Normal
    MaxExecutionCount = 100,                           // Default: null (infinite)
    MisfireHandlingEnabled = true,                     // Default: true
    MisfireStrategy = SchedulingMisfireStrategy.Default // Default: Default
}
```

### Job Priorities

Available priority levels:

- `SchedulingJobPriority.Lowest` (0)
- `SchedulingJobPriority.VeryLow` (2)
- `SchedulingJobPriority.Low` (4)
- `SchedulingJobPriority.Normal` (5) - **Default**
- `SchedulingJobPriority.High` (7)
- `SchedulingJobPriority.VeryHigh` (9)
- `SchedulingJobPriority.Highest` (10)

---

## Built-in Jobs

`Roadbed.Scheduling` includes two built-in monitoring jobs:

### 1. SchedulingStartupJobsSummaryJob

Logs a summary of all scheduled jobs **30 seconds after application startup**.

- **Group**: System
- **Priority**: Lowest
- **Runs**: Once at startup

### 2. SchedulingScheduledJobsSummaryJob

Logs a daily summary of all scheduled jobs.

- **Group**: System
- **Priority**: Lowest
- **Runs**: Daily at 8:00 AM (local time)

Both jobs help you verify that all jobs are registered and scheduled correctly.

---

## Complete Example

Here's a complete example showing metrics, result messages, and both scheduling patterns:
```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Optional: Enable logging metrics
builder.Services.AddSingleton<ISchedulingMetrics, LoggingMetricsAdapter>();

// Auto-discover and register all jobs
builder.Services.InstallModulesInAppDomain(builder.Configuration);

var app = builder.Build();
app.Run();
```
```csharp
// DailyReportJob.cs - Simple, fixed schedule
public class DailyReportJob : BaseSchedulingJob<DailyReportJob>
{
    private readonly IReportService _service;

    public DailyReportJob(ILogger<DailyReportJob> logger, IReportService service)
        : base(
            name: "DailyReport",
            description: "Generates daily reports at 6 AM",
            schedule: new SchedulingSchedule("0 0 6 * * ?"),
            logger: logger)
    {
        this._service = service;
    }

    public override async Task ExecuteAsync(CancellationToken ct)
    {
        var report = await this._service.GenerateReportAsync(ct);
        this.Context.Result = $"Generated report with {report.RecordCount} records";
    }
}
```
```csharp
// DataSyncJob.cs - Flexible, configuration-driven
public class DataSyncJob : BaseSchedulingJob<DataSyncJob>
{
    private readonly IConfiguration _config;
    private readonly ISyncService _service;

    public DataSyncJob(ILogger<DataSyncJob> logger, IConfiguration config, ISyncService service)
        : base(logger)
    {
        this._config = config;
        this._service = service;
    }

    public override string Name => "DataSync";
    public override string Description => "Syncs data at configurable intervals";
    
    public override SchedulingSchedule Schedule
    {
        get
        {
            var minutes = this._config.GetValue<int>("Jobs:DataSync:IntervalMinutes", 60);
            return new SchedulingSchedule(TimeSpan.FromMinutes(minutes));
        }
    }

    public override async Task ExecuteAsync(CancellationToken ct)
    {
        var result = await this._service.SyncDataAsync(ct);
        this.Context.Result = $"Synced {result.RecordCount} records, {result.ErrorCount} errors";
    }
}
```
```json
// appsettings.json
{
  "Jobs": {
    "DataSync": {
      "IntervalMinutes": 60,
      "Priority": "High"
    }
  }
}
```

**Console Output with Logging Metrics:**
```
info: Job DailyReport (Default) started - FireInstanceId: f8c3a...
info: Job DailyReport (Default) completed in 1523.4ms - Generated report with 1,234 records
info: Job DataSync (Default) started - FireInstanceId: 9d2e1...
info: Job DataSync (Default) completed in 2341.7ms - Synced 5,678 records, 0 errors
```

---

## Advanced Topics

### Error Handling in Jobs

Exceptions thrown from `ExecuteAsync` are captured by Quartz and reported to metrics:
```csharp
public override async Task ExecuteAsync(CancellationToken cancellationToken)
{
    try
    {
        await this.ProcessDataAsync(cancellationToken);
        this.Context.Result = "Success";
    }
    catch (Exception ex)
    {
        // Set partial result before throwing
        this.Context.Result = "Failed after processing 500 records";
        this.LogError(ex, "Job execution failed");
        throw; // Quartz marks job as failed
    }
}
```

Metrics will receive the partial result message in `JobFailed`:
```
error: Job DataProcessing (Default) failed after 2341.2ms - Failed after processing 500 records
```

### Thread Safety

- Jobs are created as **Transient** - new instance per execution
- Multiple executions of the same job can run concurrently
- Use `[DisallowConcurrentExecution]` attribute if needed (Quartz feature)

### Metrics Thread Safety

`ISchedulingMetrics` implementations must be thread-safe as they are registered as **Singleton** and may be called concurrently by multiple jobs.

---

## License

See main Roadbed repository for license information.