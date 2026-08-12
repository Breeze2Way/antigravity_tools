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

    [Fact]
    public void FormatsResetTimeWithRemainingHours()
    {
        var now = new DateTimeOffset(2026, 8, 12, 12, 0, 0, TimeSpan.Zero);
        var resetAt = now.AddHours(2.5);

        Assert.Equal(
            "重置时间：2026-08-12 14:30 [剩余 2h]",
            UsageDisplayFormatter.FormatResetDetails(resetAt, now));
    }

    [Fact]
    public void OmitsResetDetailsWhenResetTimeIsUnavailable()
    {
        Assert.Null(UsageDisplayFormatter.FormatResetDetails(null, DateTimeOffset.UtcNow));
    }
}
