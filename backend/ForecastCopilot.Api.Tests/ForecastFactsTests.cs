using ForecastCopilot.Api.Models;

namespace ForecastCopilot.Api.Tests;

// Unit tests for ForecastFacts.From – the numbers the UI shows and the LLM phrases.
public class ForecastFactsTests
{
    private static readonly DateTime Start = new(2026, 10, 5, 0, 0, 0); // a Monday

    // Builds a forecast with one point per hour from Start; predicted[i] / yesterday[i] belong to hour i.
    private static ForecastFacts Facts(int[] predicted, int[] yesterday)
    {
        var points = predicted
            .Select((units, i) => new ForecastPoint(Start.AddHours(i), units, yesterday[i]))
            .ToList();
        var response = new ForecastResponse("simulated production line", "random_forest",
            new MaeInfo(10.3, 40.6), Start.AddHours(-1), points);
        return ForecastFacts.From(response);
    }

    [Fact]
    public void Totals_and_change_percent_vs_yesterday()
    {
        var facts = Facts(predicted: [100, 100, 0], yesterday: [100, 100, 100]);

        Assert.Equal(200, facts.TotalUnitsPredicted);
        Assert.Equal(300, facts.TotalUnitsSameHoursYesterday);
        Assert.Equal(-33.3, facts.ChangePercentVsYesterday); // -33.333… rounded to 1 decimal
        Assert.Equal("Mon 00:00 – Mon 03:00", facts.ForecastPeriod); // end is exclusive
    }

    [Fact]
    public void Change_percent_is_null_when_yesterday_produced_nothing()
    {
        var facts = Facts(predicted: [50, 50], yesterday: [0, 0]);

        Assert.Null(facts.ChangePercentVsYesterday); // no division by zero
    }

    [Fact]
    public void Peak_and_lowest_producing_hour_ignore_zero_hours()
    {
        var facts = Facts(predicted: [0, 80, 120, 60, 0], yesterday: [0, 0, 0, 0, 0]);

        Assert.Equal("Mon 02:00", facts.PeakHour.Hour);
        Assert.Equal(120, facts.PeakHour.UnitsPredicted);
        Assert.Equal("Mon 03:00", facts.LowestProducingHour!.Hour); // 60, not one of the 0 hours
    }

    [Fact]
    public void Lowest_producing_hour_is_null_when_nothing_is_produced()
    {
        var facts = Facts(predicted: [0, 0, 0], yesterday: [100, 100, 100]);

        Assert.Null(facts.LowestProducingHour);
    }

    [Fact]
    public void No_production_periods_group_consecutive_zero_hours()
    {
        var facts = Facts(predicted: [0, 0, 100, 100, 0, 0], yesterday: [0, 0, 0, 0, 0, 0]);

        // Second period runs until the end of the forecast horizon.
        Assert.Equal(["Mon 00:00 – Mon 02:00", "Mon 04:00 – Mon 06:00"], facts.NoProductionPeriods);
    }

    [Fact]
    public void Biggest_drops_are_top_three_largest_drop_first()
    {
        var facts = Facts(predicted: [100, 100, 100, 100, 100], yesterday: [150, 90, 130, 200, 110]);

        // Drops: Mon 03:00 (-100), Mon 00:00 (-50), Mon 02:00 (-30), Mon 04:00 (-10); Mon 01:00 is an increase.
        Assert.Equal(["Mon 03:00", "Mon 00:00", "Mon 02:00"], facts.BiggestDropsVsYesterday.Select(h => h.Hour));
        Assert.Equal(-100, facts.BiggestDropsVsYesterday[0].DifferenceVsYesterday);
    }
}
