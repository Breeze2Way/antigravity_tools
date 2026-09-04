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
                DateTimeOffset.UtcNow));

        var state = service.Refresh(
            new DateTimeOffset(2026, 8, 11, 12, 0, 0, TimeSpan.Zero));

        Assert.Equal(42, state.OfficialRemainingPercent);
    }
}
