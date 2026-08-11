namespace CodexUsageWidget.Tests;

public sealed class UsageDisplayFormatterTests
{
    [Fact]
    public void FormatsTokenTotalInMillions()
    {
        Assert.Equal("2915.8M", UsageDisplayFormatter.FormatMillions(2_915_815_647));
    }

    [Fact]
    public void FormatsRemainingPercentageOnlyWhenAvailable()
    {
        Assert.Equal("37.5%", UsageDisplayFormatter.FormatRemainingPercent(37.5, hasBudget: true));
        Assert.Equal("--", UsageDisplayFormatter.FormatRemainingPercent(37.5, hasBudget: false));
    }
}
