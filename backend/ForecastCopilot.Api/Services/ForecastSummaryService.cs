using System.Text.Json;
using ForecastCopilot.Api.Models;

namespace ForecastCopilot.Api.Services;

// Turns structured forecast facts into a short planner summary. Prompt lives here, transport in ILlmClient.
public class ForecastSummaryService(ILlmClient llm)
{
    private const string SystemPrompt = """
        You are an assistant for a production planner of a manufacturing line.
        You receive a JSON object with forecast facts for the next 24 hours of hourly output (units produced).
        Write a short, factual summary in English: 3 to 5 sentences, plain text, no lists, no markdown.

        Rules:
        - Use only numbers and hour labels that appear literally in the JSON. Never calculate, round,
          estimate or invent numbers.
        - Cover: total output and change vs. the same hours yesterday, peak hour, lowest producing hour,
          periods without production (no shift), and the biggest drops vs. yesterday if any.
        - If ChangePercentVsYesterday is null, say there is no comparison because yesterday had no output.
        - Mention once that the data comes from a simulated production line.
        - Do not give advice beyond what the facts support.
        """;

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public Task<string> SummarizeAsync(ForecastFacts facts, CancellationToken ct = default) =>
        llm.CompleteAsync(SystemPrompt, JsonSerializer.Serialize(facts, JsonOptions), ct);
}
