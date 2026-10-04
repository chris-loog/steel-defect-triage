namespace DefectTriage.Web.Models;

// The triage page for one batch. If the Python service is down, the controller shows the Unavailable view instead.
public record TriagePageViewModel(int Seed, IReadOnlyList<BatchDefect> Batch, ThresholdGrid Grid)
{
    // KPIs come from TriageFacts – the same numbers the LLM summary is based on.
    public TriageFacts Facts { get; } = TriageFacts.From(Batch, Grid, TriageFacts.DefaultThreshold);
}
