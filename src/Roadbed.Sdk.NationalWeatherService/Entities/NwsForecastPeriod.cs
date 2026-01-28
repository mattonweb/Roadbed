/*
 * The namespace Roadbed.Sdk.NationalWeatherService.Entities was removed on purpose and replaced with Roadbed.Sdk.NationalWeatherService so that no additional using statements are required.
 */

namespace Roadbed.Sdk.NationalWeatherService;

using System;
using Roadbed.Sdk.NationalWeatherService.Dtos;

/// <summary>
/// Forecast period from the National Weather Service.
/// </summary>
/// <remarks>
/// Represents a single forecast period (e.g., "Tonight", "Monday", etc.) with
/// temperature, conditions, and timing information.
/// </remarks>
public sealed record NwsForecastPeriod
{
    #region Private Constants

    /// <summary>
    /// WMO unit for percentage values.
    /// </summary>
    private const string WmoUnitPercent = "wmoUnit:percent";

    #endregion Private Constants

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="NwsForecastPeriod"/> class.
    /// </summary>
    /// <param name="displayOrder">Order to display the period.</param>
    /// <param name="name">Name of the period (e.g., "Tonight", "Monday").</param>
    /// <param name="startTime">Start time of the forecast period.</param>
    /// <param name="endTime">End time of the forecast period.</param>
    /// <param name="temperatureInFahrenheit">Temperature forecast for the period in Fahrenheit.</param>
    /// <param name="windSpeed">Wind speed for the period (e.g., "5 to 10 mph").</param>
    /// <param name="windDirection">Wind direction for the period (e.g., "N", "SW").</param>
    /// <param name="descriptionShort">Short forecast description.</param>
    /// <param name="descriptionDetailed">Detailed forecast description.</param>
    /// <param name="chanceOfPrecipitation">Percentage chance of precipitation during the period (0-100).</param>
    public NwsForecastPeriod(
        int displayOrder,
        string? name,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        decimal temperatureInFahrenheit,
        string windSpeed,
        string windDirection,
        string descriptionShort,
        string descriptionDetailed,
        decimal chanceOfPrecipitation)
    {
        this.DisplayOrder = displayOrder;
        this.Name = name;
        this.StartTime = startTime;
        this.EndTime = endTime;
        this.TemperatureInFahrenheit = temperatureInFahrenheit;
        this.WindSpeed = windSpeed;
        this.WindDirection = windDirection;
        this.DescriptionShort = descriptionShort;
        this.DescriptionDetailed = descriptionDetailed;
        this.ChanceOfPrecipitation = chanceOfPrecipitation;
    }

    #endregion Public Constructors

    #region Public Properties

    /// <summary>
    /// Gets the percentage chance of precipitation during the period.
    /// </summary>
    /// <remarks>
    /// Range: 0 to 100 percent.
    /// </remarks>
    public decimal ChanceOfPrecipitation { get; init; }

    /// <summary>
    /// Gets the detailed forecast description.
    /// </summary>
    public string DescriptionDetailed { get; init; }

    /// <summary>
    /// Gets the short forecast description.
    /// </summary>
    public string DescriptionShort { get; init; }

    /// <summary>
    /// Gets the display order of the period.
    /// </summary>
    /// <remarks>
    /// Used to sort forecast periods chronologically (1, 2, 3, etc.).
    /// </remarks>
    public int DisplayOrder { get; init; }

    /// <summary>
    /// Gets the end time of the forecast period.
    /// </summary>
    public DateTimeOffset EndTime { get; init; }

    /// <summary>
    /// Gets the name of the period.
    /// </summary>
    /// <remarks>
    /// Examples: "Tonight", "Monday", "Monday Night", "Tuesday".
    /// </remarks>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the start time of the forecast period.
    /// </summary>
    public DateTimeOffset StartTime { get; init; }

    /// <summary>
    /// Gets the temperature forecast for the period in Fahrenheit.
    /// </summary>
    public decimal TemperatureInFahrenheit { get; init; }

    /// <summary>
    /// Gets the wind direction for the period.
    /// </summary>
    /// <remarks>
    /// Examples: "N", "NE", "E", "SE", "S", "SW", "W", "NW".
    /// </remarks>
    public string WindDirection { get; init; }

    /// <summary>
    /// Gets the wind speed for the period.
    /// </summary>
    /// <remarks>
    /// Examples: "5 to 10 mph", "15 mph".
    /// </remarks>
    public string WindSpeed { get; init; }

    #endregion Public Properties

    #region Public Methods

    /// <summary>
    /// Creates a forecast period from the NWS API response.
    /// </summary>
    /// <param name="period">Forecast period response from the NWS API.</param>
    /// <returns>Forecast period with data converted to standard units (Fahrenheit).</returns>
    /// <exception cref="ArgumentNullException">Thrown when period is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when required properties are null.</exception>
    internal static NwsForecastPeriod FromPeriod(NwsForecastPeriodResponse period)
    {
        ArgumentNullException.ThrowIfNull(period);

        // Validate required properties
        if (!period.Number.HasValue)
        {
            throw new InvalidOperationException("Forecast period number is required");
        }

        if (!period.StartTime.HasValue)
        {
            throw new InvalidOperationException("Forecast period start time is required");
        }

        if (!period.EndTime.HasValue)
        {
            throw new InvalidOperationException("Forecast period end time is required");
        }

        if (!period.Temperature.HasValue)
        {
            throw new InvalidOperationException("Forecast period temperature is required");
        }

        // Convert temperature to Fahrenheit if needed
        decimal temperatureInFahrenheit = period.Temperature.Value;
        if (string.Equals(period.TemperatureUnit, "C", StringComparison.OrdinalIgnoreCase))
        {
            temperatureInFahrenheit = temperatureInFahrenheit.ConvertCelsiusToFahrenheit();
        }

        // Extract precipitation chance if available
        decimal chanceOfPrecipitation = 0;
        if (period.Precipitation != null &&
            !string.IsNullOrWhiteSpace(period.Precipitation.Unit) &&
            period.Precipitation.Unit.Equals(WmoUnitPercent, StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(period.Precipitation.Value) &&
            decimal.TryParse(period.Precipitation.Value, out decimal parsedChance))
        {
            chanceOfPrecipitation = parsedChance;
        }

        return new NwsForecastPeriod(
            period.Number.Value,
            period.Name,
            period.StartTime.Value,
            period.EndTime.Value,
            temperatureInFahrenheit,
            period.WindSpeed ?? "Unknown",
            period.WindDirection ?? "Unknown",
            period.ForecastShort ?? string.Empty,
            period.ForecastDetailed ?? string.Empty,
            chanceOfPrecipitation);
    }

    #endregion Public Methods
}