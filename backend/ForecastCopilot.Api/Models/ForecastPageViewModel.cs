namespace ForecastCopilot.Api.Models;

// Either Forecast is set, or Error explains why the Python service could not deliver one.
public record ForecastPageViewModel(ForecastResponse? Forecast, string? Error);
