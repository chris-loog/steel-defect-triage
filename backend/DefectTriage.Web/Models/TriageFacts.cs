namespace DefectTriage.Web.Models;

// All numbers the UI and the LLM talk about, computed once in C#.
// Batch numbers describe this batch; ExpectedErrorRate comes from the held-out test set (389 defects).
public record TriageFacts(
    int BatchSize,
    IReadOnlyList<FaultCount> FaultMix,
    double Threshold,
    int AutoClassified,
    int ManualReview,
    double? ExpectedErrorRate)
{
    public const double DefaultThreshold = 0.70;

    public static TriageFacts From(IReadOnlyList<BatchDefect> batch, ThresholdGrid grid, double threshold)
    {
        var auto = batch.Count(d => d.IsAutoAt(threshold));

        var faultMix = batch
            .GroupBy(d => d.PredictedFault)
            .Select(g => new FaultCount(g.Key, g.Count(), Math.Round((double)g.Count() / batch.Count, 4)))
            .OrderByDescending(f => f.Count).ThenBy(f => f.Fault)
            .ToList();

        return new TriageFacts(batch.Count, faultMix, threshold, auto, batch.Count - auto, grid.At(threshold)?.ErrorRate);
    }
}

public record FaultCount(string Fault, int Count, double Share);
