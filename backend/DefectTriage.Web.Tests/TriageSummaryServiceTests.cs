using System.Text.Json;
using DefectTriage.Web.Models;
using DefectTriage.Web.Services;

namespace DefectTriage.Web.Tests;

// No real LLM in tests: a fake ILlmClient records the request and returns a fixed answer.
public class TriageSummaryServiceTests
{
    private class FakeLlm(string answer) : ILlmClient
    {
        public string? UserMessage { get; private set; }
        public JsonElement? Schema { get; private set; }

        public Task<string> CompleteAsync(
            string systemPrompt, string userMessage, JsonElement? responseSchema = null, CancellationToken ct = default)
        {
            UserMessage = userMessage;
            Schema = responseSchema;
            return Task.FromResult(answer);
        }
    }

    private static readonly SummaryFacts Facts = new(
        new TriageFacts(50, [new FaultCount("Bumps", 21, 0.42)], 0.70, 34, 16, 0.1091, []),
        ReviewQueue: [new ReviewItem(1203, "Pastry", 0.41, "Other_Faults", 0.38, 0.03)],
        ConfusionGroups: [new ConfusionGroup("Other_Faults vs Pastry", 6, [1203])],
        AutoReliability: [new AutoReliability("Pastry", 4, 64.5)],
        ThresholdTradeOff: [new ThresholdOption("higher", 0.75, 19, 3, 8.1, -2.8)],
        MixDeviations: []);

    private static string Note(string headline, string reviewFirst = "a", string watchOut = "b", string tradeOff = "c") =>
        JsonSerializer.Serialize(new { headline, reviewFirst, watchOut, thresholdTradeOff = tradeOff });

    [Fact]
    public void FactsJson_HasPercentsReadyToCopy()
    {
        var json = SummaryFactsJson();

        Assert.Contains("\"ExpectedErrorRatePercent\": 10.9", json);
        Assert.Contains("\"TestPrecisionPercent\": 64.5", json);
        Assert.DoesNotContain("0.1091", json); // the fraction form would make the LLM calculate
    }

    [Fact]
    public async Task Summarize_SendsFactsAndSchema_ParsesNote()
    {
        var llm = new FakeLlm(Note("Most reviews are Other_Faults vs Pastry."));

        var summary = await new TriageSummaryService(llm).SummarizeAsync(Facts);

        Assert.Equal(SummaryFactsJson(), llm.UserMessage);
        Assert.NotNull(llm.Schema);
        Assert.Equal("Most reviews are Other_Faults vs Pastry.", summary.Note.Headline);
        Assert.Empty(summary.UnverifiedNumbers);
    }

    [Fact]
    public async Task Summarize_GuardCoversAllSections()
    {
        var llm = new FakeLlm(Note("6 reviews.", reviewFirst: "Row 1203 first.", watchOut: "Pastry 70%.", tradeOff: "3 more at 0.75."));

        var summary = await new TriageSummaryService(llm).SummarizeAsync(Facts);

        Assert.Equal(["70"], summary.UnverifiedNumbers);
    }

    [Theory]
    [InlineData("This is plain text, not JSON.")]
    [InlineData("""{"headline": "only one field"}""")]
    public async Task Summarize_InvalidAnswer_ThrowsLlmUnavailable(string answer) =>
        await Assert.ThrowsAsync<LlmUnavailableException>(() => new TriageSummaryService(new FakeLlm(answer)).SummarizeAsync(Facts));

    private static string SummaryFactsJson() => TriageSummaryService.BuildFactsJson(Facts);
}
