using ForecastCopilot.Api.Models;

namespace ForecastCopilot.Api.Tests;

// Unit tests for SummaryFacts.From – the extra insights the LLM handover note talks about.
public class SummaryFactsTests
{
    // Probabilities: the predicted fault gets `confidence`, the runner-up gets `runnerUp`, the rest share nothing.
    private static BatchDefect Defect(int row, string predicted, double confidence, string runnerUp = "Other_Faults",
        double runnerUpProbability = 0.1) =>
        new(row, predicted, predicted, confidence,
            new Dictionary<string, double> { [predicted] = confidence, [runnerUp] = runnerUpProbability, ["Stains"] = 0.0 });

    private static readonly ThresholdStat[] Stats =
    [
        new(0.30, 389, 1.0, 0.0, 0.211),
        new(0.35, 380, 0.977, 0.023, 0.2),
        new(0.65, 280, 0.72, 0.28, 0.13),
        new(0.70, 263, 0.676, 0.324, 0.1091),
        new(0.75, 240, 0.617, 0.383, 0.081),
    ];

    // Usual predicted counts = recall * support / precision: Bumps 80, Pastry 20 -> 80 % / 20 %.
    private static readonly Dictionary<string, ClassMetric> PerClass = new()
    {
        ["Bumps"] = new(0.8, 0.8, 80),
        ["Pastry"] = new(0.5, 0.5, 20),
    };

    [Fact]
    public void ReviewQueue_HasRunnerUpAndMargin_LeastConfidentFirst()
    {
        var batch = new[] { Defect(1, "Bumps", 0.60, "Pastry", 0.35), Defect(2, "Pastry", 0.45, "Bumps", 0.40), Defect(3, "Bumps", 0.9) };

        var facts = SummaryFacts.From(batch, Stats, PerClass, 0.70);

        Assert.Equal(
            [new ReviewItem(2, "Pastry", 0.45, "Bumps", 0.40, 0.05), new ReviewItem(1, "Bumps", 0.60, "Pastry", 0.35, 0.25)],
            facts.ReviewQueue);
    }

    [Fact]
    public void ConfusionGroups_TreatPairAsUnordered()
    {
        var batch = new[] { Defect(1, "Bumps", 0.6, "Pastry"), Defect(2, "Pastry", 0.5, "Bumps"), Defect(3, "Bumps", 0.5) };

        var facts = SummaryFacts.From(batch, Stats, PerClass, 0.70);

        Assert.Equal(2, facts.ConfusionGroups.Count);
        Assert.Equal("Bumps vs Pastry", facts.ConfusionGroups[0].Pair);
        Assert.Equal([2, 1], facts.ConfusionGroups[0].Rows); // least confident first
        Assert.Equal("Bumps vs Other_Faults", facts.ConfusionGroups[1].Pair);
    }

    [Fact]
    public void AutoReliability_OnlyAutoClassified_LeastPreciseFirst()
    {
        var batch = new[] { Defect(1, "Bumps", 0.9), Defect(2, "Pastry", 0.8), Defect(3, "Bumps", 0.75), Defect(4, "Pastry", 0.5) };

        var facts = SummaryFacts.From(batch, Stats, PerClass, 0.70);

        Assert.Equal([new AutoReliability("Pastry", 1, 50.0), new AutoReliability("Bumps", 2, 80.0)], facts.AutoReliability);
    }

    [Fact]
    public void TradeOff_OneStepDownAndUp_WithPrecomputedDeltas()
    {
        var batch = new[] { Defect(1, "Bumps", 0.66), Defect(2, "Bumps", 0.72), Defect(3, "Bumps", 0.9) };

        var facts = SummaryFacts.From(batch, Stats, PerClass, 0.70);

        Assert.Equal(
            [new ThresholdOption("lower", 0.65, 0, -1, 13.0, 2.1), new ThresholdOption("higher", 0.75, 2, 1, 8.1, -2.8)],
            facts.ThresholdTradeOff);
    }

    [Fact]
    public void TradeOff_AtLowestThreshold_HasNoStepDown()
    {
        var facts = SummaryFacts.From([Defect(1, "Bumps", 0.9)], Stats, PerClass, 0.30);

        Assert.Equal("higher", Assert.Single(facts.ThresholdTradeOff).Direction);
    }

    [Fact]
    public void MixDeviations_OnlyBigDifferences()
    {
        // Batch: 50 % Bumps / 50 % Pastry vs. usual 80 % / 20 % -> both differ by 30 points.
        var batch = new[] { Defect(1, "Bumps", 0.9), Defect(2, "Pastry", 0.9) };
        Assert.Equal(2, SummaryFacts.From(batch, Stats, PerClass, 0.70).MixDeviations.Count);

        // Batch: 80 % / 20 % = usual mix -> nothing to report.
        var usual = Enumerable.Range(1, 4).Select(i => Defect(i, "Bumps", 0.9)).Append(Defect(5, "Pastry", 0.9)).ToList();
        Assert.Empty(SummaryFacts.From(usual, Stats, PerClass, 0.70).MixDeviations);
    }
}
