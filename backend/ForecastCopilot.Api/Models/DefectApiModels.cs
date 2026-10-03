using System.Text.Json.Serialization;

namespace ForecastCopilot.Api.Models;

// Mirror the Pydantic models in forecasting/app.py (BatchDefect, ThresholdStat).
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
}

// What-if for one confidence threshold, computed on the whole held-out test set (389 defects).
public record ThresholdStat(
    [property: JsonPropertyName("threshold")] double Threshold,
    [property: JsonPropertyName("n_auto")] int NAuto,
    [property: JsonPropertyName("auto_share")] double AutoShare,
    [property: JsonPropertyName("manual_share")] double ManualShare,
    [property: JsonPropertyName("error_rate")] double? ErrorRate); // null if nothing is auto-classified
