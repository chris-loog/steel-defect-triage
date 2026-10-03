namespace ForecastCopilot.Api.Models;

// Either Forecast is set, or Error explains why the Python service could not deliver one.
public record ForecastPageViewModel(ForecastResponse? Forecast, string? Error)
{
    // KPIs for the header cards. Computed in C# (not JS, not the LLM) so the numbers are deterministic.
    public int TotalPredicted => Forecast?.Forecast.Sum(p => p.UnitsPredicted) ?? 0;

    public int TotalSameHourYesterday => Forecast?.Forecast.Sum(p => p.UnitsSameHourYesterday) ?? 0;

    // null when yesterday produced nothing (avoids division by zero, e.g. after a Sunday).
    public double? ChangePercent => TotalSameHourYesterday == 0
        ? null
        : 100.0 * (TotalPredicted - TotalSameHourYesterday) / TotalSameHourYesterday;

    public ForecastPoint? PeakPoint => Forecast?.Forecast.MaxBy(p => p.UnitsPredicted);
}
