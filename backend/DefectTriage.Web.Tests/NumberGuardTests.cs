using DefectTriage.Web.Services;

namespace DefectTriage.Web.Tests;

// The guard flags numbers in the LLM answer that don't appear in the facts JSON.
public class NumberGuardTests
{
    private const string Facts = """{ "BatchSize": 50, "ConfidenceThreshold": 0.7, "ExpectedErrorRatePercent": 10.9 }""";

    [Fact]
    public void AllNumbersFromFacts_NothingFlagged() =>
        Assert.Empty(NumberGuard.FindUnverified("Of 50 defects, 10.9% are expected to be wrong.", Facts));

    [Fact]
    public void SameValueDifferentFormat_NotFlagged() =>
        Assert.Empty(NumberGuard.FindUnverified("At threshold 0.70 the model is used.", Facts));

    [Fact]
    public void InventedNumbers_FlaggedOnce()
    {
        var unverified = NumberGuard.FindUnverified("Of 50 defects, 11% are wrong; 11% is high, 50.5 too.", Facts);

        Assert.Equal(["11", "50.5"], unverified);
    }

    [Fact]
    public void NoNumbersInAnswer_NothingFlagged() =>
        Assert.Empty(NumberGuard.FindUnverified("Most defects are Bumps.", Facts));
}
