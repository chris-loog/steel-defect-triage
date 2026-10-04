using System.Text.Json.Serialization;

namespace DefectTriage.Web.Models;

// Mirror the Pydantic models in classifier/app.py (BatchDefect, ThresholdStat).
// [JsonPropertyName] maps Python snake_case to C# PascalCase in both directions.

// One held-out test defect, classified by the model. TrueFault is known only because it comes from the test set.
public record BatchDefect(
    [property: JsonPropertyName("row")] int Row,
    [property: JsonPropertyName("true_fault")] string TrueFault,
    [property: JsonPropertyName("predicted_fault")] string PredictedFault,
    [property: JsonPropertyName("confidence")] double Confidence,
    [property: JsonPropertyName("probabilities")] IReadOnlyDictionary<string, double> Probabilities)
{
    [JsonIgnore] public bool IsCorrect => PredictedFault == TrueFault;

    // The triage rule (same as Python's threshold_stats): confidence >= threshold -> classified automatically.
    public bool IsAutoAt(double threshold) => Confidence >= threshold;
}

// What-if for one confidence threshold, computed on the whole held-out test set (389 defects).
// Python also sends n_auto/auto_share/manual_share; the page counts those from the batch, so they are not mapped.
public record ThresholdStat(
    [property: JsonPropertyName("threshold")] double Threshold,
    [property: JsonPropertyName("error_rate")] double? ErrorRate); // null if nothing is auto-classified

// Subset of GET /model-info (metrics.json written by train.py): per-class results on the held-out test set.
public record ModelInfo(
    [property: JsonPropertyName("per_class_test")] IReadOnlyDictionary<string, ClassMetric> PerClassTest);

// Support = number of test defects whose true fault is this class.
public record ClassMetric(
    [property: JsonPropertyName("precision")] double Precision,
    [property: JsonPropertyName("recall")] double Recall,
    [property: JsonPropertyName("support")] double Support);
