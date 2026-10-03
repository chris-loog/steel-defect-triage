using System.Text.Json.Serialization;

namespace ForecastCopilot.Api.Models;

// Mirrors the Pydantic models in forecasting/app.py (JSON contract: docs/PROJECT.md §6).
// [JsonPropertyName] maps Python snake_case to C# PascalCase in both directions.

public record ForecastResponse(
    [property: JsonPropertyName("data_source")] string DataSource,
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("mae")] MaeInfo Mae,
    [property: JsonPropertyName("last_actual_timestamp")] DateTime LastActualTimestamp,
    [property: JsonPropertyName("forecast")] IReadOnlyList<ForecastPoint> Forecast);

public record MaeInfo(
    [property: JsonPropertyName("model")] double Model,
    [property: JsonPropertyName("baseline_same_hour_yesterday")] double BaselineSameHourYesterday);

public record ForecastPoint(
    [property: JsonPropertyName("timestamp")] DateTime Timestamp,
    [property: JsonPropertyName("units_predicted")] int UnitsPredicted,
    [property: JsonPropertyName("units_same_hour_yesterday")] int UnitsSameHourYesterday);
