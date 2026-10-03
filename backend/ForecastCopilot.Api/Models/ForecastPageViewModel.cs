namespace ForecastCopilot.Api.Models;

// Either Forecast is set, or Error explains why the Python service could not deliver one.
public record ForecastPageViewModel(ForecastResponse? Forecast, string? Error)
{
    // KPIs come from ForecastFacts – the same numbers the LLM summary is based on.
    public ForecastFacts? Facts { get; } = Forecast is null ? null : ForecastFacts.From(Forecast);

    public int TotalPredicted => Facts?.TotalUnitsPredicted ?? 0;

    public int TotalSameHourYesterday => Facts?.TotalUnitsSameHoursYesterday ?? 0;

    public double? ChangePercent => Facts?.ChangePercentVsYesterday;

    public ForecastPoint? PeakPoint => Forecast?.Forecast.MaxBy(p => p.UnitsPredicted);
}
