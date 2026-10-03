using System.Globalization;
using System.Text.RegularExpressions;

namespace ForecastCopilot.Api.Services;

// Deterministic check on the non-deterministic LLM: every number in the answer must appear in the facts.
// Flags only (the UI shows a warning); it doesn't block the summary.
public static partial class NumberGuard
{
    // 42, 10.9, 0.70 – no sign or thousands separators; the facts contain none.
    [GeneratedRegex(@"\d+(?:\.\d+)?")]
    private static partial Regex NumberPattern();

    public static IReadOnlyList<string> FindUnverified(string answer, string factsJson)
    {
        var known = Numbers(factsJson).Select(n => n.Value).ToHashSet();
        return Numbers(answer)
            .Where(n => !known.Contains(n.Value))
            .Select(n => n.Text)
            .Distinct()
            .ToList();
    }

    // decimal, not double: 0.7m == 0.70m, and no binary rounding surprises.
    private static IEnumerable<(string Text, decimal Value)> Numbers(string text) =>
        NumberPattern().Matches(text).Select(m => (m.Value, decimal.Parse(m.Value, CultureInfo.InvariantCulture)));
}
