namespace ForecastCopilot.Api.Models;

// Either Batch/ThresholdStats are set, or Error explains why the Python service could not deliver them.
public record TriagePageViewModel(
    int Seed,
    IReadOnlyList<BatchDefect>? Batch,
    IReadOnlyList<ThresholdStat>? ThresholdStats,
    string? Error)
{
    // KPIs come from TriageFacts – the same numbers the LLM summary is based on.
    public TriageFacts? Facts { get; } = Batch is null || ThresholdStats is null
        ? null
        : TriageFacts.From(Batch, ThresholdStats, TriageFacts.DefaultThreshold);
}
