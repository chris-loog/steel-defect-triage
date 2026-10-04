using System.Globalization;

namespace DefectTriage.Web.Models;

// The confidence thresholds the Python API has test-set stats for (GET /threshold-stats) = the slider's grid.
// The only place that knows which thresholds exist: lookup, neighbours and slider range all come from here.
public class ThresholdGrid
{
    private readonly List<ThresholdStat> stats;

    public ThresholdGrid(IEnumerable<ThresholdStat> stats)
    {
        this.stats = stats.OrderBy(s => s.Threshold).ToList();
        if (this.stats.Count < 2)
            throw new ArgumentException("A threshold grid needs at least two thresholds.", nameof(stats));
    }

    public double Min => stats[0].Threshold;
    public double Max => stats[^1].Threshold;
    public double Step => Math.Round(stats[1].Threshold - stats[0].Threshold, 4);

    // Null = not on the grid. Thresholds are doubles (0.7 may arrive as 0.7000000001) -> tolerance, not ==.
    public ThresholdStat? At(double threshold) => IndexOf(threshold) is var i and >= 0 ? stats[i] : null;

    // One slider step down / up; null at the ends of the grid (or if threshold is not on it).
    public ThresholdStat? Below(double threshold) => Neighbour(threshold, -1);
    public ThresholdStat? Above(double threshold) => Neighbour(threshold, +1);

    // For triage.js: "0.70" -> expected error rate (null if nothing is auto-classified at that threshold).
    public IReadOnlyDictionary<string, double?> ErrorRates =>
        stats.ToDictionary(s => s.Threshold.ToString("0.00", CultureInfo.InvariantCulture), s => s.ErrorRate);

    private int IndexOf(double threshold) => stats.FindIndex(s => Math.Abs(s.Threshold - threshold) < 1e-6);

    private ThresholdStat? Neighbour(double threshold, int offset)
    {
        var i = IndexOf(threshold);
        var j = i + offset;
        return i >= 0 && j >= 0 && j < stats.Count ? stats[j] : null;
    }
}
