namespace CodexUsageWidget.Tests;

public sealed class SettingsDisplayFormatterTests
{
    [Theory]
    [InlineData(45, "45%")]
    [InlineData(92.4, "92%")]
    [InlineData(100, "100%")]
    public void FormatsOpacityAsOneClearPercentage(double value, string expected)
    {
        Assert.Equal(expected, SettingsDisplayFormatter.FormatOpacityPercent(value));
    }
}
