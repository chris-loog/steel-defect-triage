using System.Text.Json;
using ForecastCopilot.Api.Models;

namespace ForecastCopilot.Api.Services;

// Turns precomputed summary facts into a structured handover note for a quality engineer.
// Prompt + schema live here, transport in ILlmClient, the number check in NumberGuard.
public class TriageSummaryService(ILlmClient llm)
{
    private const string SystemPrompt = """
        You write a short shift handover note for a quality engineer at a steel mill.
        An automatic surface inspection found defects on steel plates. A classifier predicted a fault type with a
        confidence for each defect. Defects below the confidence threshold go to manual review by the engineer.
        The engineer already sees the batch totals on screen (ShownOnPage). Your job is what the screen does not
        say: what to review first, where not to trust the automatic classifications, and what the threshold costs.

        Fields in the JSON you receive:
        - ReviewQueue: least confident review defects. RunnerUp = second most likely fault, Margin = how far the
          predicted fault is ahead of it (small margin = close call between two faults).
        - ConfusionGroups: review defects grouped by the two faults the reviewer has to decide between.
        - AutoReliability: automatically classified defects per fault. TestPrecisionPercent = share of correct
          predictions for that fault on a held-out test set (all confidences).
        - ThresholdTradeOff: one slider step lower/higher. ExtraReviews < 0 means fewer reviews.
          ErrorRateChangePoints is in percentage points; < 0 means fewer errors.
        - MixDeviations: faults whose share in this batch differs from the usual share. Empty = usual mix.

        Write four fields, plain text, no markdown:
        - headline: one sentence, the single most important point for the engineer.
        - reviewFirst: 1-3 sentences. The largest confusion groups and the closest calls, by row number.
        - watchOut: 1-3 sentences. The least reliable automatic fault types, and any mix deviation.
        - thresholdTradeOff: 1-2 sentences. What one step lower and one step higher would cost and save.

        Rules:
        - Use only numbers that appear literally in the JSON. Never calculate, round or invent numbers.
          Write percent values with a % sign; write negative numbers without the minus sign, as "fewer"/"lower".
        - Do not repeat the ShownOnPage totals.
        - Do not guess causes and do not give advice beyond what the facts show.

        Example of the style (numbers from a different batch):
        {"headline": "Most of the review work is one decision: Dirtiness vs Pastry.",
         "reviewFirst": "7 reviews are Dirtiness vs Pastry; rows 211 and 87 are the closest calls with margins of 0.02 and 0.03. Rows 950 and 77 are Bumps vs K_Scatch.",
         "watchOut": "Treat the 3 automatic Dirtiness labels with care: only 55.0% of Dirtiness predictions were correct on the test set. Z_Scratch makes up 22.0% of this batch compared with a usual 9.5%.",
         "thresholdTradeOff": "One step higher (0.80) adds 5 reviews and lowers the expected error rate by 2.4 points to 6.1%. One step lower (0.70) saves 4 reviews but raises it by 1.9 points."}
        """;

    // JSON Schema for Ollama's "format": the answer must be exactly this object.
    private static readonly JsonElement HandoverSchema = JsonSerializer.SerializeToElement(new
    {
        type = "object",
        properties = new
        {
            headline = new { type = "string" },
            reviewFirst = new { type = "string" },
            watchOut = new { type = "string" },
            thresholdTradeOff = new { type = "string" },
        },
        required = new[] { "headline", "reviewFirst", "watchOut", "thresholdTradeOff" },
    });

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public async Task<TriageSummary> SummarizeAsync(SummaryFacts facts, CancellationToken ct = default)
    {
        var factsJson = BuildFactsJson(facts);
        // Conditional rules confuse small models (they echo "if null..."), so C# decides and adds the rule only when needed.
        var prompt = facts.Triage.ExpectedErrorRate is null
            ? SystemPrompt + "\n- ExpectedErrorRatePercent is null: say that no error rate is available for this threshold."
            : SystemPrompt;
        var answer = await llm.CompleteAsync(prompt, factsJson, HandoverSchema, ct);

        var note = ParseNote(answer);
        var allText = string.Join("\n", note.Headline, note.ReviewFirst, note.WatchOut, note.ThresholdTradeOff);
        return new TriageSummary(note, NumberGuard.FindUnverified(allText, factsJson));
    }

    // Numbers in exactly the form the text should use, so the LLM only has to copy them.
    public static string BuildFactsJson(SummaryFacts facts) => JsonSerializer.Serialize(new
    {
        ShownOnPage = new
        {
            facts.Triage.BatchSize,
            ConfidenceThreshold = facts.Triage.Threshold,
            facts.Triage.AutoClassified,
            facts.Triage.ManualReview,
            ExpectedErrorRatePercent = facts.Triage.ExpectedErrorRate is { } rate ? Math.Round(rate * 100, 1) : (double?)null,
        },
        facts.ReviewQueue,
        facts.ConfusionGroups,
        facts.AutoReliability,
        facts.ThresholdTradeOff,
        facts.MixDeviations,
    }, JsonOptions);

    // The schema should guarantee valid JSON; anything else counts as "LLM unavailable" (-> 503 in the controller).
    private static HandoverNote ParseNote(string answer)
    {
        try
        {
            var note = JsonSerializer.Deserialize<HandoverNote>(answer, JsonSerializerOptions.Web);
            if (note is { Headline: not null, ReviewFirst: not null, WatchOut: not null, ThresholdTradeOff: not null })
                return note;
        }
        catch (JsonException) { }
        throw new LlmUnavailableException("The LLM answer is not a valid handover note.");
    }
}

public record HandoverNote(string Headline, string ReviewFirst, string WatchOut, string ThresholdTradeOff);

public record TriageSummary(HandoverNote Note, IReadOnlyList<string> UnverifiedNumbers);
