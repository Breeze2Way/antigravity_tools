namespace AntigravityUsageWidget.Tests;

public sealed class AntigravityQuotaAggregatorTests
{
    [Fact]
    public void UsesTheLowestRemainingQuotaForEachDisplayPeriod()
    {
        var snapshot = new AntigravityQuotaSnapshot(
            "Pro",
            [
                new("Gemini", "Gemini Models", 66.95, new DateTimeOffset(2026, 9, 4, 7, 11, 54, TimeSpan.Zero), AntigravityQuotaPeriod.Short),
                new("Third-party", "Claude and GPT models", 100, new DateTimeOffset(2026, 9, 4, 9, 25, 30, TimeSpan.Zero), AntigravityQuotaPeriod.Short),
                new("Gemini", "Gemini Models", 94.49, new DateTimeOffset(2026, 9, 11, 2, 11, 54, TimeSpan.Zero), AntigravityQuotaPeriod.Weekly),
                new("Third-party", "Claude and GPT models", 100, new DateTimeOffset(2026, 9, 11, 4, 4, 58, TimeSpan.Zero), AntigravityQuotaPeriod.Weekly)
            ],
            new DateTimeOffset(2026, 9, 4, 4, 0, 0, TimeSpan.Zero));

        var result = AntigravityQuotaAggregator.Aggregate(snapshot);

        Assert.Equal(66.95, result.ShortRemainingPercent!.Value, precision: 6);
        Assert.Equal(snapshot.Rows[0].ResetAt, result.ShortResetAt);
        Assert.Equal(94.49, result.WeeklyRemainingPercent!.Value, precision: 6);
        Assert.Equal(snapshot.Rows[2].ResetAt, result.WeeklyResetAt);
    }

    [Fact]
    public void LeavesMissingPeriodsUnavailable()
    {
        var snapshot = new AntigravityQuotaSnapshot(
            null,
            [new("Gemini", null, 50, null, AntigravityQuotaPeriod.Short)],
            DateTimeOffset.UtcNow);

        var result = AntigravityQuotaAggregator.Aggregate(snapshot);

        Assert.Equal(50, result.ShortRemainingPercent!.Value);
        Assert.Null(result.WeeklyRemainingPercent);
    }

    [Fact]
    public void UsesTheCurrentModelsGroupInsteadOfTheLowestOtherGroup()
    {
        var snapshot = new AntigravityQuotaSnapshot(
            "Pro",
            [
                new("Gemini", "Gemini Models", 66.95, null, AntigravityQuotaPeriod.Short),
                new("Claude", "Claude and GPT models", 10, null, AntigravityQuotaPeriod.Short),
                new("Gemini", "Gemini Models", 94.49, null, AntigravityQuotaPeriod.Weekly),
                new("Claude", "Claude and GPT models", 20, null, AntigravityQuotaPeriod.Weekly)
            ],
            DateTimeOffset.UtcNow)
        {
            SelectedModelId = "MODEL_PLACEHOLDER_M318",
            SelectedModelLabel = "Gemini 3.8 Flash (High)"
        };

        var result = AntigravityQuotaAggregator.Aggregate(snapshot);

        Assert.Equal(66.95, result.ShortRemainingPercent!.Value, precision: 6);
        Assert.Equal(94.49, result.WeeklyRemainingPercent!.Value, precision: 6);
    }
}
