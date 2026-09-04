namespace AntigravityUsageWidget.Tests;

public sealed class AppIconTests
{
    [Fact]
    public void UsesFloatingBallIconFile()
    {
        Assert.Equal("悬浮球.ico", AppIcon.FileName);
        Assert.Equal(
            Path.Combine("publish", "悬浮球.ico"),
            AppIcon.GetPath("publish"));
    }
}
