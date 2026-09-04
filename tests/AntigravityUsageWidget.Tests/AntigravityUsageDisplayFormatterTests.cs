namespace AntigravityUsageWidget.Tests;

public sealed class AntigravityUsageDisplayFormatterTests
{
    [Fact]
    public void IncludesPlanAllRowsAndRefreshTimeInChinese()
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

        Assert.Contains("Antigravity Pro", details);
        Assert.Contains("Gemini Models", details);
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

        Assert.Contains("Antigravity", details);
        Assert.Contains("Short", details);
        Assert.Contains("Weekly: unavailable", details);
        Assert.Contains("Updated", details);
    }
}
