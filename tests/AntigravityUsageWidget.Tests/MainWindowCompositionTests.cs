using AntigravityUsageWidget.Data;

namespace AntigravityUsageWidget.Tests;

public sealed class MainWindowCompositionTests
{
    [Fact]
    public void CreatesRefreshServiceWithOfficialUsageReader()
    {
        var service = MainWindow.CreateRefreshService(
            () => new AntigravityQuotaSnapshot(
                "Pro",
                [new("Gemini", null, 42, null, AntigravityQuotaPeriod.Weekly)],
                DateTimeOffset.UtcNow),
            _ => new AntigravityTokenUsageSummary(1_200_000, 800_000));

        var state = service.Refresh(
            new DateTimeOffset(2026, 8, 11, 12, 0, 0, TimeSpan.Zero));

        Assert.Equal(42, state.OfficialRemainingPercent);
        Assert.Equal(1_200_000, state.TodayTokens);
        Assert.Equal(800_000, state.YesterdayTokens);
    }
}
