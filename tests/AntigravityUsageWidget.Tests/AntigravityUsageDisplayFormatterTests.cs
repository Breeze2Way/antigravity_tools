namespace AntigravityUsageWidget.Tests;

public sealed class AntigravityUsageDisplayFormatterTests
{
    [Fact]
    public void ShowsBothPeriodsForEachModelGroupInChinese()
    {
        var quota = new AntigravityDisplayQuota(
            "Pro",
            67,
            null,
            94.5,
            null,
            [
                new("Five Hour Limit Remaining", "Gemini Models", 67, null, AntigravityQuotaPeriod.Short),
                new("Weekly Limit Remaining", "Gemini Models", 94.5, null, AntigravityQuotaPeriod.Weekly),
                new("Five Hour Limit Remaining", "Claude and GPT models", 100, null, AntigravityQuotaPeriod.Short),
                new("Weekly Limit Remaining", "Claude and GPT models", 100, null, AntigravityQuotaPeriod.Weekly)
            ]);

        var details = AntigravityUsageDisplayFormatter.FormatTooltipDetails(
            quota,
            DateTimeOffset.UtcNow,
            english: false);

        Assert.Contains("Gemini : [5h:67%] [周:94.5%]", details);
        Assert.Contains("Claude : [5h:100%] [周:100%]", details);
        Assert.DoesNotContain("Gemini Models", details);
        Assert.DoesNotContain("Weekly quota", details);
    }

    [Fact]
    public void ShowsBothPeriodsForEachModelGroupInEnglish()
    {
        var quota = new AntigravityDisplayQuota(
            "Pro",
            67,
            null,
            94.5,
            null,
            [
                new("Five Hour Limit Remaining", "Gemini Models", 67, null, AntigravityQuotaPeriod.Short),
                new("Weekly Limit Remaining", "Gemini Models", 94.5, null, AntigravityQuotaPeriod.Weekly),
                new("Five Hour Limit Remaining", "Claude and GPT models", 100, null, AntigravityQuotaPeriod.Short),
                new("Weekly Limit Remaining", "Claude and GPT models", 100, null, AntigravityQuotaPeriod.Weekly)
            ]);

        var details = AntigravityUsageDisplayFormatter.FormatTooltipDetails(
            quota,
            DateTimeOffset.UtcNow,
            english: true);

        Assert.Contains("Gemini : [5h:67%] [Weekly:94.5%]", details);
        Assert.Contains("Claude : [5h:100%] [Weekly:100%]", details);
        Assert.DoesNotContain("周额度", details);
        Assert.DoesNotContain("五小时额度", details);
    }

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

        Assert.Contains("Gemini : [5h:67%] [周:94.5%]", details);
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
            [
                new("Five Hour Limit Remaining", "Gemini Models", 66.95, null, AntigravityQuotaPeriod.Short),
                new("Weekly Limit Remaining", "Gemini Models", 94.49, null, AntigravityQuotaPeriod.Weekly)
            ]);

        var details = AntigravityUsageDisplayFormatter.FormatTooltipDetails(
            quota,
            new DateTimeOffset(2026, 9, 4, 12, 30, 0, TimeSpan.FromHours(8)),
            english: true);

        Assert.Contains("Gemini : [5h:67%] [Weekly:94.5%]", details);
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

        Assert.Contains("Gemini : [5h:67%] [周:94.5%]", details);
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
            [new("Gemini", "Gemini Models", 50, null, AntigravityQuotaPeriod.Short)]);

        var details = AntigravityUsageDisplayFormatter.FormatTooltipDetails(
            quota,
            new DateTimeOffset(2026, 9, 4, 4, 30, 0, TimeSpan.Zero),
            english: true);

        Assert.Contains("Gemini : [5h:50%] [Weekly:unavailable]", details);
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

    [Fact]
    public void AddsDailyTokenUsageToTheQuotaTooltipInTheConfiguredLanguage()
    {
        var quota = new AntigravityDisplayQuota("Pro", 75, null, 90, null, []);
        var summary = new AntigravityTokenUsageSummary(3_450_039, 500_000);

        var chinese = AntigravityUsageDisplayFormatter.FormatTooltipDetails(
            quota,
            DateTimeOffset.UtcNow,
            english: false,
            tokenUsage: summary);
        var english = AntigravityUsageDisplayFormatter.FormatTooltipDetails(
            quota,
            DateTimeOffset.UtcNow,
            english: true,
            tokenUsage: summary);

        Assert.Contains("今日token:3.5M(昨日：0.5M)", chinese);
        Assert.Contains("Today tokens:3.5M (Yesterday:0.5M)", english);
    }

    [Fact]
    public void FormatsTheChineseTooltipInTheCompactLayout()
    {
        var refreshedAt = new DateTimeOffset(2026, 9, 4, 14, 39, 30, TimeSpan.FromHours(8));
        var quota = new AntigravityDisplayQuota(
            "Pro",
            64.8,
            refreshedAt.AddMinutes(32),
            94.1,
            new DateTimeOffset(2026, 9, 11, 10, 11, 0, TimeSpan.FromHours(8)),
            [
                new("Gemini short", "Gemini Models", 64.8, null, AntigravityQuotaPeriod.Short),
                new("Gemini weekly", "Gemini Models", 94.1, null, AntigravityQuotaPeriod.Weekly),
                new("Claude short", "Claude and GPT models", 100, null, AntigravityQuotaPeriod.Short),
                new("Claude weekly", "Claude and GPT models", 100, null, AntigravityQuotaPeriod.Weekly)
            ]);

        var details = AntigravityUsageDisplayFormatter.FormatTooltipDetails(
            quota,
            refreshedAt,
            english: false,
            tokenUsage: new AntigravityTokenUsageSummary(3_500_000, 0));

        var lines = details.Split(Environment.NewLine);
        Assert.Equal("今日token:3.5M(昨日：0.0M)", lines[0]);
        Assert.Equal("Gemini : [5h:64.8%] [周:94.1%]", lines[1]);
        Assert.Equal("Claude : [5h:100%] [周:100%]", lines[2]);
        Assert.Equal(string.Empty, lines[3]);
        Assert.Equal("更新时间:2026-09-04 14:39:30", lines[4]);
    }
}
