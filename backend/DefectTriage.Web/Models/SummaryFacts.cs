namespace DefectTriage.Web.Models;

// Everything the LLM handover note may talk about, computed once in C#.
// Triage = the basics the page shows; the other lists are insights the page doesn't show.
public record SummaryFacts(
    TriageFacts Triage,
    IReadOnlyList<ReviewItem> ReviewQueue,
    IReadOnlyList<ConfusionGroup> ConfusionGroups,
    IReadOnlyList<AutoReliability> AutoReliability,
    IReadOnlyList<ThresholdOption> ThresholdTradeOff,
    IReadOnlyList<MixDeviation> MixDeviations)
{
    private const int ReviewQueueCount = 10;
    private const double ThresholdStep = 0.05;      // same step as the slider
    private const double MixDeviationPoints = 10;   // smaller differences are noise in a batch of 50

    public static SummaryFacts From(
        IReadOnlyList<BatchDefect> batch, IReadOnlyList<ThresholdStat> thresholdStats,
        IReadOnlyDictionary<string, ClassMetric> perClassTest, double threshold)
    {
        var triage = TriageFacts.From(batch, thresholdStats, threshold);

        var manual = batch
            .Where(d => d.Confidence < threshold)
            .OrderBy(d => d.Confidence)
            .Select(ToReviewItem)
            .ToList();

        return new SummaryFacts(
            triage,
            manual.Take(ReviewQueueCount).ToList(),
            ConfusionGroupsOf(manual),
            AutoReliabilityOf(batch, perClassTest, threshold),
            TradeOffOf(batch, thresholdStats, triage),
            MixDeviationsOf(batch, perClassTest));
    }

    // Runner-up = second most likely class -> tells the reviewer which two faults to decide between.
    private static ReviewItem ToReviewItem(BatchDefect d)
    {
        var runnerUp = d.Probabilities.Where(p => p.Key != d.PredictedFault).MaxBy(p => p.Value);
        return new ReviewItem(d.Row, d.PredictedFault, Math.Round(d.Confidence, 2),
            runnerUp.Key, Math.Round(runnerUp.Value, 2), Math.Round(d.Confidence - runnerUp.Value, 2));
    }

    // "Bumps vs Other_Faults" and "Other_Faults vs Bumps" are the same decision -> sort the pair's names.
    private static List<ConfusionGroup> ConfusionGroupsOf(List<ReviewItem> manual) => manual
        .GroupBy(r => string.Join(" vs ", new[] { r.PredictedFault, r.RunnerUp }.Order()))
        .Select(g => new ConfusionGroup(g.Key, g.Count(), g.Select(r => r.Row).ToList()))
        .OrderByDescending(g => g.Count).ThenBy(g => g.Pair)
        .ToList();

    // Precision over all test defects of that class (not only those above this threshold) -> a conservative hint.
    private static List<AutoReliability> AutoReliabilityOf(
        IReadOnlyList<BatchDefect> batch, IReadOnlyDictionary<string, ClassMetric> perClassTest, double threshold) =>
        batch
            .Where(d => d.Confidence >= threshold && perClassTest.ContainsKey(d.PredictedFault))
            .GroupBy(d => d.PredictedFault)
            .Select(g => new AutoReliability(g.Key, g.Count(), Percent(perClassTest[g.Key].Precision)))
            .OrderBy(a => a.TestPrecisionPercent).ThenBy(a => a.Fault)
            .ToList();

    // One slider step down and up: review load in *this* batch, error rate from the test set, deltas precomputed.
    private static List<ThresholdOption> TradeOffOf(
        IReadOnlyList<BatchDefect> batch, IReadOnlyList<ThresholdStat> thresholdStats, TriageFacts now)
    {
        var options = new List<ThresholdOption>();
        foreach (var (direction, t) in new[] { ("lower", now.Threshold - ThresholdStep), ("higher", now.Threshold + ThresholdStep) })
        {
            var stat = thresholdStats.FirstOrDefault(s => Math.Abs(s.Threshold - t) < 1e-6);
            if (stat is null) continue; // no step below 0.30 / above 0.95

            var manualReview = batch.Count(d => d.Confidence < stat.Threshold);
            options.Add(new ThresholdOption(
                direction,
                Math.Round(stat.Threshold, 2),
                manualReview,
                manualReview - now.ManualReview,
                stat.ErrorRate is { } e ? Percent(e) : null,
                stat.ErrorRate is { } e2 && now.ExpectedErrorRate is { } e1 ? Math.Round((e2 - e1) * 100, 1) : null));
        }
        return options;
    }

    // Compare predicted shares with predicted shares: usual predicted count per class on the test set
    // = true positives / precision, with true positives = recall * support.
    private static List<MixDeviation> MixDeviationsOf(
        IReadOnlyList<BatchDefect> batch, IReadOnlyDictionary<string, ClassMetric> perClassTest)
    {
        var usualPredicted = perClassTest
            .Where(c => c.Value.Precision > 0)
            .ToDictionary(c => c.Key, c => c.Value.Recall * c.Value.Support / c.Value.Precision);
        var usualTotal = usualPredicted.Values.Sum();

        return usualPredicted
            .Select(c =>
            {
                var batchShare = (double)batch.Count(d => d.PredictedFault == c.Key) / batch.Count;
                var usualShare = c.Value / usualTotal;
                return new MixDeviation(c.Key, Percent(batchShare), Percent(usualShare),
                    Math.Round((batchShare - usualShare) * 100, 1));
            })
            .Where(m => Math.Abs(m.DifferencePoints) >= MixDeviationPoints)
            .OrderByDescending(m => Math.Abs(m.DifferencePoints)).ThenBy(m => m.Fault)
            .ToList();
    }

    private static double Percent(double fraction) => Math.Round(fraction * 100, 1);
}

public record ReviewItem(
    int Row, string PredictedFault, double Confidence, string RunnerUp, double RunnerUpProbability, double Margin);

public record ConfusionGroup(string Pair, int Count, IReadOnlyList<int> Rows);

public record AutoReliability(string Fault, int AutoCount, double TestPrecisionPercent);

// ExtraReviews < 0 = fewer reviews; ErrorRateChangePoints in percentage points (null if no error rate).
public record ThresholdOption(
    string Direction, double Threshold, int ManualReview, int ExtraReviews,
    double? ExpectedErrorRatePercent, double? ErrorRateChangePoints);

public record MixDeviation(string Fault, double BatchSharePercent, double UsualSharePercent, double DifferencePoints);
