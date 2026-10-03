using ForecastCopilot.Api.Models;

namespace ForecastCopilot.Api.Tests;

// Unit tests for TriageFacts.From – the numbers the UI shows and (from Stage 16) the LLM phrases.
public class TriageFactsTests
{
    private static BatchDefect Defect(int row, string predicted, double confidence, string? trueFault = null) =>
        new(row, trueFault ?? predicted, predicted, confidence, new Dictionary<string, double>());

    private static readonly ThresholdStat[] Stats =
    [
        new(0.70, 263, 0.676, 0.324, 0.1091),
        new(0.95, 0, 0.0, 1.0, null),
    ];

    [Fact]
    public void FaultMix_CountsPredictedFaults_MostFrequentFirst()
    {
        var batch = new[] { Defect(1, "Bumps", 0.9), Defect(2, "Pastry", 0.5), Defect(3, "Bumps", 0.8, "Pastry") };

        var facts = TriageFacts.From(batch, Stats, 0.70);

        Assert.Equal(3, facts.BatchSize);
        Assert.Equal(new FaultCount("Bumps", 2, 0.6667), facts.FaultMix[0]);
        Assert.Equal(new FaultCount("Pastry", 1, 0.3333), facts.FaultMix[1]);
    }

    [Fact]
    public void ConfidenceEqualToThreshold_IsAutoClassified()
    {
        var batch = new[] { Defect(1, "Bumps", 0.70), Defect(2, "Bumps", 0.69), Defect(3, "Bumps", 0.99) };

        var facts = TriageFacts.From(batch, Stats, 0.70);

        Assert.Equal(2, facts.AutoClassified);
        Assert.Equal(1, facts.ManualReview);
    }

    [Theory]
    [InlineData(0.70, 0.1091)]
    [InlineData(0.95, null)]  // nothing auto-classified on the test set
    [InlineData(0.42, null)]  // threshold not in the stats table
    public void ExpectedErrorRate_IsLookedUpFromTestSetStats(double threshold, double? expected)
    {
        var facts = TriageFacts.From([Defect(1, "Bumps", 0.9)], Stats, threshold);

        Assert.Equal(expected, facts.ExpectedErrorRate);
    }

    [Fact]
    public void LowestConfidence_IsSortedAscending_AndLimitedToFive()
    {
        var batch = Enumerable.Range(1, 8).Select(i => Defect(i, "Stains", 1.0 - i * 0.1)).ToList();

        var facts = TriageFacts.From(batch, Stats, 0.70);

        Assert.Equal([8, 7, 6, 5, 4], facts.LowestConfidence.Select(d => d.Row));
        Assert.Equal(0.2, facts.LowestConfidence[0].Confidence);
    }
}
