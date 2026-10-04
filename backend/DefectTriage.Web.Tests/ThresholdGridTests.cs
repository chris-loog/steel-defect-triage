using DefectTriage.Web.Models;

namespace DefectTriage.Web.Tests;

// Unit tests for ThresholdGrid – the slider's thresholds, as delivered by GET /threshold-stats.
public class ThresholdGridTests
{
    // Deliberately unsorted and with float noise, as doubles from JSON can be.
    private static readonly ThresholdGrid Grid = new([new(0.35, 0.2), new(0.30000000001, 0.21), new(0.40, null)]);

    [Fact]
    public void Range_ComesFromTheStats()
    {
        Assert.Equal(0.30, Grid.Min, 6);
        Assert.Equal(0.40, Grid.Max, 6);
        Assert.Equal(0.05, Grid.Step, 6);
    }

    [Theory]
    [InlineData(0.30, true)]
    [InlineData(0.35, true)]
    [InlineData(0.32, false)] // between two steps
    [InlineData(0.45, false)] // above the grid
    public void At_FindsOnlyThresholdsOnTheGrid(double threshold, bool found) =>
        Assert.Equal(found, Grid.At(threshold) is not null);

    [Fact]
    public void BelowAndAbove_AreOneStep_NullAtTheEnds()
    {
        Assert.Null(Grid.Below(0.30));
        Assert.Equal(0.30, Grid.Below(0.35)!.Threshold, 6);
        Assert.Equal(0.40, Grid.Above(0.35)!.Threshold, 6);
        Assert.Null(Grid.Above(0.40));
        Assert.Null(Grid.Above(0.32)); // not on the grid
    }

    [Fact]
    public void ErrorRates_AreKeyedLikeTheSliderValue()
    {
        Assert.Equal(new Dictionary<string, double?> { ["0.30"] = 0.21, ["0.35"] = 0.2, ["0.40"] = null }, Grid.ErrorRates);
    }

    [Fact]
    public void LessThanTwoThresholds_Throws() =>
        Assert.Throws<ArgumentException>(() => new ThresholdGrid([new(0.7, 0.1)]));
}
