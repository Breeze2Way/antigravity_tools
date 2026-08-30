using CodexUsageWidget.Data;

namespace CodexUsageWidget.Tests;

public sealed class MainWindowCompositionTests
{
    [Fact]
    public void CreatesRefreshServiceWithOfficialUsageReader()
    {
        var service = MainWindow.CreateRefreshService(
            new CodexDataPaths("state.db", "sessions"),
            () => new OfficialUsageSnapshot(42, TimeSpan.FromHours(2)));

        var state = service.Refresh(
            new DateTimeOffset(2026, 8, 11, 12, 0, 0, TimeSpan.Zero),
            new WidgetSettings());

        Assert.Equal(42, state.OfficialRemainingPercent);
    }
}
