using System.Globalization;

namespace ForecastCopilot.Api.Models;

// All numbers the UI and the LLM talk about, computed once in C#.
// The LLM receives this record as JSON and only phrases it – it never calculates anything itself.
public record ForecastFacts(
    string DataSource,
    string Model,
    string ForecastPeriod,
    int TotalUnitsPredicted,
    int TotalUnitsSameHoursYesterday,
    double? ChangePercentVsYesterday,
    HourFact PeakHour,
    HourFact? LowestProducingHour,
    IReadOnlyList<string> NoProductionPeriods,
    IReadOnlyList<HourFact> BiggestDropsVsYesterday,
    double MaeModel,
    double MaeBaselineSameHourYesterday)
{
    private const string HourFormat = "ddd HH:mm";

    public static ForecastFacts From(ForecastResponse f)
    {
        var points = f.Forecast;
        var total = points.Sum(p => p.UnitsPredicted);
        var totalYesterday = points.Sum(p => p.UnitsSameHourYesterday);

        return new ForecastFacts(
            DataSource: f.DataSource,
            Model: f.Model,
            ForecastPeriod: $"{points[0].Timestamp.ToString(HourFormat, CultureInfo.InvariantCulture)} – {points[^1].Timestamp.AddHours(1).ToString(HourFormat, CultureInfo.InvariantCulture)}",
            TotalUnitsPredicted: total,
            TotalUnitsSameHoursYesterday: totalYesterday,
            // null when yesterday produced nothing (avoids division by zero, e.g. after a Sunday).
            ChangePercentVsYesterday: totalYesterday == 0
                ? null
                : Math.Round(100.0 * (total - totalYesterday) / totalYesterday, 1),
            PeakHour: HourFact.From(points.MaxBy(p => p.UnitsPredicted)!),
            // Lowest hour *while producing* – zero hours are no-shift periods, reported separately.
            LowestProducingHour: points.Where(p => p.UnitsPredicted > 0).MinBy(p => p.UnitsPredicted) is { } low
                ? HourFact.From(low)
                : null,
            NoProductionPeriods: ZeroPeriods(points),
            // Top 3 hours where the forecast is clearly below the same hour yesterday ("unusual dips").
            BiggestDropsVsYesterday: points
                .Where(p => p.UnitsPredicted < p.UnitsSameHourYesterday)
                .OrderBy(p => p.UnitsPredicted - p.UnitsSameHourYesterday)
                .Take(3)
                .Select(HourFact.From)
                .ToList(),
            MaeModel: f.Mae.Model,
            MaeBaselineSameHourYesterday: f.Mae.BaselineSameHourYesterday);
    }

    // Groups consecutive zero-output hours into ranges like "Sat 14:00 – Mon 00:00" (end exclusive).
    private static List<string> ZeroPeriods(IReadOnlyList<ForecastPoint> points)
    {
        var periods = new List<string>();
        DateTime? start = null;
        foreach (var p in points)
        {
            if (p.UnitsPredicted == 0) start ??= p.Timestamp;
            else if (start is not null)
            {
                periods.Add($"{start.Value.ToString(HourFormat, CultureInfo.InvariantCulture)} – {p.Timestamp.ToString(HourFormat, CultureInfo.InvariantCulture)}");
                start = null;
            }
        }
        if (start is not null)
            periods.Add($"{start.Value.ToString(HourFormat, CultureInfo.InvariantCulture)} – {points[^1].Timestamp.AddHours(1).ToString(HourFormat, CultureInfo.InvariantCulture)}");
        return periods;
    }
}

public record HourFact(string Hour, int UnitsPredicted, int UnitsSameHourYesterday, int DifferenceVsYesterday)
{
    public static HourFact From(ForecastPoint p) => new(
        p.Timestamp.ToString("ddd HH:mm", CultureInfo.InvariantCulture),
        p.UnitsPredicted,
        p.UnitsSameHourYesterday,
        p.UnitsPredicted - p.UnitsSameHourYesterday);
}
