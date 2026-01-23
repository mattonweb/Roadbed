# Roadbed.Sdk.NationalWeatherService

C# SDK for the National Weather Service API providing weather forecasts, station data, and office information.

## Overview

This SDK simplifies interaction with the NWS API by handling HTTP requests, parsing GeoJSON responses, and providing strongly-typed C# objects. Get weather forecasts using latitude/longitude coordinates with automatic conversion to NWS grid coordinates.

**API Documentation**: https://api.weather.gov/

## Installation
```bash
dotnet add package Roadbed.Sdk.NationalWeatherService
```

## Quick Start

### 1. Create Messaging Request

The NWS API requires a User Agent string. Use `MessagingPublisher` to provide this:
```csharp
using Roadbed.Messaging;
using Roadbed.Common;

// Create publisher (used as User Agent for NWS API)
var publisher = new MessagingPublisher(
    new CommonBusinessKey("my-weather-app", "MyWeatherApp"),
    "v1.0");

// Create messaging request
var messagingRequest = new MessagingMessageRequest<CommonKeyValuePair<string, string>>(
    publisher,
    "nws.forecast");
```

### 2. Get Forecast by Location
```csharp
using Roadbed.Sdk.NationalWeatherService;

// Convert lat/long to NWS grid coordinates
var forecastRequest = await NwsForecastRequest.FromLocation(
    latitude: 39.7456,
    longitude: -97.0892,
    messagingRequest,
    cancellationToken);

Console.WriteLine($"Office: {forecastRequest.OfficeId}");
Console.WriteLine($"Grid: ({forecastRequest.GridCoordinateX}, {forecastRequest.GridCoordinateY})");

// Get daily forecast (12-hour periods for next 7 days)
var dailyForecast = await NwsForecastDaily.FromForecastRequest(
    forecastRequest,
    messagingRequest,
    cancellationToken);

Console.WriteLine($"Forecast created: {dailyForecast.CreatedOn}");
Console.WriteLine($"Periods: {dailyForecast.Periods?.Count}");

// Access forecast periods
if (dailyForecast.Periods != null)
{
    foreach (var period in dailyForecast.Periods)
    {
        Console.WriteLine($"{period.Name}: {period.DescriptionShort}");
        Console.WriteLine($"  Temperature: {period.TemperatureInFahrenheit}°F");
        Console.WriteLine($"  Wind: {period.WindSpeed} {period.WindDirection}");
        Console.WriteLine($"  Precipitation: {period.ChanceOfPrecipitation}%");
    }
}
```

### 3. Get Hourly Forecast
```csharp
// Get hourly forecast (hourly periods for next 7 days)
var hourlyForecast = await NwsForecastHourly.FromForecastRequest(
    forecastRequest,
    messagingRequest,
    cancellationToken);

Console.WriteLine($"Hourly periods: {hourlyForecast.Periods?.Count}");

// Access hourly periods
if (hourlyForecast.Periods != null)
{
    var nextHour = hourlyForecast.Periods.First();
    Console.WriteLine($"Next hour: {nextHour.StartTime}");
    Console.WriteLine($"Temp: {nextHour.TemperatureInFahrenheit}°F");
    Console.WriteLine($"Conditions: {nextHour.DescriptionShort}");
}
```

## Core Workflow
```
Latitude/Longitude
      ↓
NwsForecastRequest.FromLocation()  ← Converts to grid coordinates
      ↓
Grid Coordinates (Office ID, X, Y)
      ↓
NwsForecastDaily / NwsForecastHourly
      ↓
Collection of NwsForecastPeriod
      ↓
Temperature, Wind, Precipitation, Descriptions
```

## Key Classes

### NwsForecastRequest

Converts geographic coordinates to NWS grid coordinates required for forecasts.
```csharp
var request = await NwsForecastRequest.FromLocation(
    latitude: 39.7456,
    longitude: -97.0892,
    messagingRequest,
    cancellationToken);

// Properties populated from API:
// - OfficeId (e.g., "TOP")
// - GridCoordinateX (e.g., 31)
// - GridCoordinateY (e.g., 80)
// - WeatherForecastOffice (NwsOffice details)
```

### NwsForecastDaily

Retrieves daily forecasts with 12-hour periods (day/night) for the next 7 days.
```csharp
var daily = await NwsForecastDaily.FromForecastRequest(
    forecastRequest,
    messagingRequest,
    cancellationToken);

// Properties:
// - CreatedOn: When NWS generated the forecast
// - Periods: Collection of NwsForecastPeriod
```

### NwsForecastHourly

Retrieves hourly forecasts for the next 7 days (approximately 156 periods).
```csharp
var hourly = await NwsForecastHourly.FromForecastRequest(
    forecastRequest,
    messagingRequest,
    cancellationToken);

// Properties:
// - CreatedOn: When NWS generated the forecast
// - Periods: Collection of NwsForecastPeriod
```

### NwsForecastPeriod

Individual forecast period with weather details.

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string?` | Period name (e.g., "Tonight", "Monday") |
| `StartTime` | `DateTimeOffset` | Period start time |
| `EndTime` | `DateTimeOffset` | Period end time |
| `TemperatureInFahrenheit` | `decimal` | Temperature in Fahrenheit |
| `WindSpeed` | `string` | Wind speed (e.g., "5 to 10 mph") |
| `WindDirection` | `string` | Wind direction (e.g., "N", "SW") |
| `DescriptionShort` | `string` | Brief forecast description |
| `DescriptionDetailed` | `string` | Detailed forecast description |
| `ChanceOfPrecipitation` | `decimal` | Precipitation chance (0-100%) |
| `DisplayOrder` | `int` | Chronological ordering |

**Note**: Temperatures are automatically converted to Fahrenheit if the API returns Celsius.

## Complete Example
```csharp
using Roadbed.Sdk.NationalWeatherService;
using Roadbed.Messaging;
using Roadbed.Common;

public class WeatherService
{
    private readonly MessagingMessageRequest<CommonKeyValuePair<string, string>> _messagingRequest;

    public WeatherService()
    {
        var publisher = new MessagingPublisher(
            new CommonBusinessKey("weather-app", "WeatherApp"),
            "v1.0");
        
        _messagingRequest = new MessagingMessageRequest<CommonKeyValuePair<string, string>>(
            publisher,
            "nws.forecast");
    }

    public async Task<string> GetForecastAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default)
    {
        // Get grid coordinates
        var forecastRequest = await NwsForecastRequest.FromLocation(
            latitude,
            longitude,
            _messagingRequest,
            cancellationToken);

        // Get daily forecast
        var daily = await NwsForecastDaily.FromForecastRequest(
            forecastRequest,
            _messagingRequest,
            cancellationToken);

        if (daily.Periods == null || daily.Periods.Count == 0)
        {
            return "No forecast data available";
        }

        // Build forecast summary
        var today = daily.Periods[0];
        var tonight = daily.Periods.Count > 1 ? daily.Periods[1] : null;

        var summary = $"Weather for {forecastRequest.OfficeId}\n";
        summary += $"\n{today.Name}: {today.DescriptionShort}\n";
        summary += $"Temperature: {today.TemperatureInFahrenheit}°F\n";
        summary += $"Wind: {today.WindSpeed} {today.WindDirection}\n";
        summary += $"Precipitation: {today.ChanceOfPrecipitation}%\n";

        if (tonight != null)
        {
            summary += $"\n{tonight.Name}: {tonight.DescriptionShort}\n";
            summary += $"Temperature: {tonight.TemperatureInFahrenheit}°F\n";
        }

        return summary;
    }
}
```

## Additional Metadata (Optional)

These entities provide supplementary information about NWS infrastructure but are not required for retrieving forecasts.

### NwsAdminDistrict

Retrieves all weather stations for a US state or territory.
```csharp
var district = await NwsAdminDistrict.FromAdminDistrict(
    AdminDistrictTwoCharacterCode.CA,  // California
    messagingRequest,
    cancellationToken);

Console.WriteLine($"Stations in CA: {district.Stations?.Count}");
```

### NwsStation

Retrieves details about a specific observation station.
```csharp
var station = await NwsStation.FromId(
    "KSFO",  // San Francisco International Airport
    messagingRequest,
    cancellationToken);

Console.WriteLine($"Station: {station.Name}");
Console.WriteLine($"Location: {station.Latitude}, {station.Longitude}");
Console.WriteLine($"Time Zone: {station.TimeZone}");
```

### NwsOffice

Retrieves details about a Weather Forecast Office.
```csharp
var office = await NwsOffice.FromId(
    "TOP",  // Topeka, Kansas office
    messagingRequest,
    cancellationToken);

Console.WriteLine($"Office: {office.Name}");
Console.WriteLine($"Address: {office.StreetAddress}, {office.AddressLocality}");
Console.WriteLine($"Phone: {office.Telephone}");
```

## Requirements

- .NET 10.0+
- Roadbed (core utilities and logging)
- Roadbed.Messaging (for MessagingPublisher)
- Roadbed.Net (HTTP client wrapper)
- Newtonsoft.Json

## Related Packages

- **Roadbed** - Core utilities and base classes
- **Roadbed.Messaging** - Message envelope structure
- **Roadbed.Net** - HTTP client with retry logic