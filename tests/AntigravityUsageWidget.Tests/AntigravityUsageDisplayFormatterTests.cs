namespace AntigravityUsageWidget.Tests;

public sealed class AntigravityUsageDisplayFormatterTests
{
    [Fact]
    public void ShowsOnlyChineseQuotaSummariesWithoutRawApiLabels()
    {
        var quota = new AntigravityDisplayQuota(
            "Pro",
            66.95,
            null,
            94.49,
            null,
            [
                new("Five Hour Limit Remaining", "Gemini Models", 66.95, null, AntigravityQuotaPeriod.Short),
                new("Weekly Limit Remaining", "Gemini Models", 94.49, null, AntigravityQuotaPeriod.Weekly)
            ]);

        var details = AntigravityUsageDisplayFormatter.FormatTooltipDetails(
            quota,
            new DateTimeOffset(2026, 9, 4, 12, 30, 0, TimeSpan.FromHours(8)),
            english: false);

        Assert.Contains("周额度: 94.5%", details);
        Assert.Contains("五小时额度: 67%", details);
        Assert.DoesNotContain("Weekly", details);
        Assert.DoesNotContain("Five Hour", details);
        Assert.DoesNotContain("Gemini Models", details);
    }

    [Fact]
    public void ShowsOnlyEnglishQuotaSummariesWithoutChineseLabels()
    {
        var quota = new AntigravityDisplayQuota(
            "Pro",
            66.95,
            null,
            94.49,
            null,
            [new("Five Hour Limit Remaining", "Gemini Models", 66.95, null, AntigravityQuotaPeriod.Short)]);

        var details = AntigravityUsageDisplayFormatter.FormatTooltipDetails(
            quota,
            new DateTimeOffset(2026, 9, 4, 12, 30, 0, TimeSpan.FromHours(8)),
            english: true);

        Assert.Contains("Weekly quota: 94.5%", details);
        Assert.Contains("5-hour quota: 67%", details);
        Assert.DoesNotContain("周额度", details);
        Assert.DoesNotContain("五小时额度", details);
        Assert.DoesNotContain("Gemini Models", details);
    }

    [Fact]
    public void IncludesQuotaSummariesAndRefreshTimeInChinese()
    {
        var refreshedAt = new DateTimeOffset(2026, 9, 4, 12, 30, 0, TimeSpan.FromHours(8));
        var quota = new AntigravityDisplayQuota(
            "Pro",
            66.95,
            new DateTimeOffset(2026, 9, 4, 15, 11, 54, TimeSpan.Zero),
            94.49,
            new DateTimeOffset(2026, 9, 11, 10, 11, 54, TimeSpan.Zero),
            [
                new("Five Hour Limit Remaining", "Gemini Models", 66.95, null, AntigravityQuotaPeriod.Short),
                new("Weekly Limit Remaining", "Gemini Models", 94.49, null, AntigravityQuotaPeriod.Weekly)
            ]);

        var details = AntigravityUsageDisplayFormatter.FormatTooltipDetails(quota, refreshedAt, english: false);

        Assert.Contains("周额度: 94.5%", details);
        Assert.Contains("五小时额度: 67%", details);
        Assert.Contains("67%", details);
        Assert.Contains("94.5%", details);
        Assert.Contains("更新时间", details);
    }

    [Fact]
    public void UsesEnglishLabelsAndMarksMissingPeriod()
    {
        var quota = new AntigravityDisplayQuota(
            null,
            50,
            null,
            null,
            null,
            [new("Gemini", null, 50, null, AntigravityQuotaPeriod.Short)]);

        var details = AntigravityUsageDisplayFormatter.FormatTooltipDetails(
            quota,
            new DateTimeOffset(2026, 9, 4, 4, 30, 0, TimeSpan.Zero),
            english: true);

        Assert.Contains("5-hour quota: 50%", details);
        Assert.Contains("Weekly quota: unavailable", details);
        Assert.Contains("Updated", details);
        Assert.DoesNotContain("周额度", details);
        Assert.DoesNotContain("五小时额度", details);
    }

    [Fact]
    public void UsesConfiguredLanguageForResetDetails()
    {
        var shortReset = new DateTimeOffset(2026, 9, 4, 15, 11, 54, TimeSpan.Zero);
        var weeklyReset = new DateTimeOffset(2026, 9, 11, 10, 11, 54, TimeSpan.Zero);
        var now = new DateTimeOffset(2026, 9, 4, 12, 30, 0, TimeSpan.Zero);

        var chinese = AntigravityUsageDisplayFormatter.FormatResetDetails(
            shortReset,
            weeklyReset,
            now,
            english: false);
        var english = AntigravityUsageDisplayFormatter.FormatResetDetails(
            shortReset,
            weeklyReset,
            now,
            english: true);

        Assert.Contains("五小时重置时间", chinese);
        Assert.Contains("周重置时间", chinese);
        Assert.DoesNotContain("reset", chinese);
        Assert.Contains("5-hour reset", english);
        Assert.Contains("Weekly reset", english);
        Assert.DoesNotContain("重置", english);
    }
}
