namespace AntigravityUsageWidget.Tests;

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
    public void FormatsTooltipDetailsWithoutRedundantStatusLine()
    {
        var details = UsageDisplayFormatter.FormatTooltipDetails(
            "54%",
            sevenDayTokens: 1_000_000,
            thirtyDayTokens: 2_000_000,
            refreshedAt: new DateTimeOffset(2026, 8, 12, 12, 0, 0, TimeSpan.Zero));

        Assert.Contains("周剩余：54%", details);
        Assert.Contains("近 7 天总量：1.0M", details);
        Assert.Contains("近 30 天总量：2.0M", details);
        Assert.DoesNotContain("状态：", details);
    }

    [Fact]
    public void FormatsBothFiveHourAndWeeklyRemainingValues()
    {
        var details = UsageDisplayFormatter.FormatTooltipDetails(
            fiveHourText: "88%",
            weeklyText: "66%",
            sevenDayTokens: 1_000_000,
            thirtyDayTokens: 2_000_000,
            refreshedAt: new DateTimeOffset(2026, 8, 12, 12, 0, 0, TimeSpan.Zero));

        Assert.Contains("五小时剩余：88%", details);
        Assert.Contains("周剩余：66%", details);
    }

    [Fact]
    public void IncludesTodayAndYesterdayUsageInTooltip()
    {
        var details = UsageDisplayFormatter.FormatTooltipDetails(
            "80%",
            "60%",
            todayTokens: 10_000_000,
            yesterdayTokens: 200_000_000,
            sevenDayTokens: 300_000_000,
            thirtyDayTokens: 400_000_000,
            refreshedAt: new DateTimeOffset(2026, 8, 11, 12, 0, 0, TimeSpan.Zero));

        Assert.Contains("用量：10M (昨日200M)", details);
    }

    [Fact]
    public void FormatsTooltipDetailsInEnglish()
    {
        var details = UsageDisplayFormatter.FormatTooltipDetails(
            "88%",
            "66%",
            todayTokens: 10_000_000,
            yesterdayTokens: 200_000_000,
            sevenDayTokens: 300_000_000,
            thirtyDayTokens: 400_000_000,
            refreshedAt: new DateTimeOffset(2026, 8, 11, 12, 0, 0, TimeSpan.Zero),
            english: true);

        Assert.Contains("Five-hour remaining: 88%", details);
        Assert.Contains("Weekly remaining: 66%", details);
        Assert.Contains("Usage: 10M (yesterday 200M)", details);
        Assert.Contains("Last 7 days total: 300.0M", details);
        Assert.Contains("Updated:", details);
        Assert.DoesNotContain("剩余", details);
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
    public void FormatsResetTimeInEnglish()
    {
        var now = new DateTimeOffset(2026, 8, 12, 12, 0, 0, TimeSpan.Zero);

        Assert.Equal(
            "Reset time: 2026-08-12 14:30 [2h remaining]",
            UsageDisplayFormatter.FormatResetDetails(now.AddHours(2.5), now, english: true));
    }

    [Fact]
    public void OmitsResetDetailsWhenResetTimeIsUnavailable()
    {
        Assert.Null(UsageDisplayFormatter.FormatResetDetails(null, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void FormatsBothFiveHourAndWeeklyResetTimes()
    {
        var now = new DateTimeOffset(2026, 8, 12, 12, 0, 0, TimeSpan.Zero);

        var details = UsageDisplayFormatter.FormatResetDetails(
            now.AddHours(2.5),
            now.AddDays(2),
            now);

        Assert.Contains("五小时重置时间：2026-08-12 14:30 [剩余 2h]", details);
        Assert.Contains("周重置时间：2026-08-14 12:00 [剩余 48h]", details);
    }
}
