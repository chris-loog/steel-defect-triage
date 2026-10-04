namespace DefectTriage.Web.Models;

// All numbers the UI and the LLM talk about, computed once in C#.
// Batch numbers describe this batch; ExpectedErrorRate comes from the held-out test set (389 defects).
public record TriageFacts(
    int BatchSize,
    IReadOnlyList<FaultCount> FaultMix,
    double Threshold,
    int AutoClassified,
    int ManualReview,
    double? ExpectedErrorRate,
    IReadOnlyList<LowConfidenceDefect> LowestConfidence)
{
    public const double DefaultThreshold = 0.70;
    private const int LowestConfidenceCount = 5;

    public static TriageFacts From(
        IReadOnlyList<BatchDefect> batch, IReadOnlyList<ThresholdStat> thresholdStats, double threshold)
    {
        // Same rule as Python's threshold_stats: confidence >= threshold -> auto-classified.
        var auto = batch.Count(d => d.Confidence >= threshold);

        var faultMix = batch
            .GroupBy(d => d.PredictedFault)
            .Select(g => new FaultCount(g.Key, g.Count(), Math.Round((double)g.Count() / batch.Count, 4)))
            .OrderByDescending(f => f.Count).ThenBy(f => f.Fault)
            .ToList();

        // Thresholds are doubles (0.7 may be 0.7000000001) -> compare with a tolerance, not ==.
        var stat = thresholdStats.FirstOrDefault(s => Math.Abs(s.Threshold - threshold) < 1e-6);

        var lowest = batch
            .OrderBy(d => d.Confidence)
            .Take(LowestConfidenceCount)
            .Select(d => new LowConfidenceDefect(d.Row, d.PredictedFault, Math.Round(d.Confidence, 2)))
            .ToList();

        return new TriageFacts(batch.Count, faultMix, threshold, auto, batch.Count - auto, stat?.ErrorRate, lowest);
    }
}

public record FaultCount(string Fault, int Count, double Share);

public record LowConfidenceDefect(int Row, string PredictedFault, double Confidence);
