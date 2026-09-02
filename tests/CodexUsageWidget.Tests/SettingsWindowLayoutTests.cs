namespace CodexUsageWidget.Tests;

public sealed class SettingsWindowLayoutTests
{
    [Fact]
    public void SettingsWindowLeavesRoomForColorRowsAndActionButtons()
    {
        Assert.True(MainWindow.SettingsWindowWidth >= 480);
        Assert.True(MainWindow.SettingsWindowHeight >= 540);
    }
}
