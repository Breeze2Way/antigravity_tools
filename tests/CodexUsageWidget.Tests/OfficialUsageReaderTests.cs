namespace CodexUsageWidget.Tests;

public sealed class OfficialUsageReaderTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(1234, true)]
    public void InspectsOnlyProcessesWithMainWindows(long handle, bool expected)
    {
        Assert.Equal(expected, OfficialUsageReader.ShouldInspectProcess(new IntPtr(handle)));
    }

    [Fact]
    public void BoundsUiAutomationPollingToLimitCpuBursts()
    {
        Assert.Equal(4, OfficialUsageReader.UiReadAttempts);
        Assert.Equal(TimeSpan.FromMilliseconds(250), OfficialUsageReader.UiPollInterval);
    }

    [Fact]
    public void RetriesUiReadUntilAValueIsAvailable()
    {
        var attempts = 0;
        var waits = 0;

        var result = OfficialUsageReader.WaitUntil(
            () => ++attempts >= 3,
            maxAttempts: 4,
            wait: () => waits++);

        Assert.True(result);
        Assert.Equal(3, attempts);
        Assert.Equal(2, waits);
    }

    [Theory]
    [InlineData("54%", 54)]
    [InlineData("  37.5% ", 37.5)]
    public void ParsesOfficialPercentage(string text, double expected)
    {
        Assert.True(OfficialUsageReader.TryParsePercentage(text, out var percentage));
        Assert.Equal(expected, percentage, precision: 6);
    }

    [Fact]
    public void ParsesPercentageFromCurrentUsageMenuLabel()
    {
        Assert.True(OfficialUsageReader.TryParsePercentage("使用情况 剩余 78%", out var percentage));
        Assert.Equal(78, percentage, precision: 6);
    }

    [Fact]
    public void SelectsPercentageFromWeeklyUsageRow()
    {
        var percentage = OfficialUsageReader.SelectWeeklyPercentage(
            [
                new OfficialUsageReader.PercentageCandidate(96, 646, 18),
                new OfficialUsageReader.PercentageCandidate(86, 670, 18)
            ],
            [
                new OfficialUsageReader.UsageLabelCandidate("5 小时", 646, 18),
                new OfficialUsageReader.UsageLabelCandidate("1 周", 670, 18)
            ]);

        Assert.Equal(86, percentage);

        var fiveHourPercentage = OfficialUsageReader.SelectFiveHourPercentage(
            [
                new OfficialUsageReader.PercentageCandidate(96, 646, 18),
                new OfficialUsageReader.PercentageCandidate(86, 670, 18)
            ],
            [
                new OfficialUsageReader.UsageLabelCandidate("5 小时", 646, 18),
                new OfficialUsageReader.UsageLabelCandidate("1 周", 670, 18)
            ]);

        Assert.Equal(96, fiveHourPercentage);
    }

    [Theory]
    [InlineData("剩余 54%")]
    [InlineData("54")]
    [InlineData("101%")]
    [InlineData("")]
    public void RejectsNonStandaloneOrInvalidPercentage(string text)
    {
        Assert.False(OfficialUsageReader.TryParsePercentage(text, out _));
    }

    [Theory]
    [InlineData("重置时间：2小时30分钟", 150)]
    [InlineData("Resets in 1h 15m", 75)]
    [InlineData("45分钟后重置", 45)]
    [InlineData("1天2小时", 1_560)]
    public void ParsesResetCountdown(string text, double expectedMinutes)
    {
        Assert.True(OfficialUsageReader.TryParseResetAfter(text, out var resetAfter));
        Assert.Equal(expectedMinutes, resetAfter.TotalMinutes, precision: 6);
    }

    [Fact]
    public void ParsesChineseResetDateAsCountdown()
    {
        var now = new DateTimeOffset(2026, 8, 12, 12, 0, 0, TimeSpan.FromHours(8));

        Assert.True(OfficialUsageReader.TryParseResetAfter("8月18日", now, out var resetAfter));
        Assert.Equal(132, resetAfter.TotalHours, precision: 6);
    }

    [Theory]
    [InlineData("重置时间")]
    [InlineData("54%")]
    [InlineData("")]
    public void RejectsTextWithoutResetCountdown(string text)
    {
        Assert.False(OfficialUsageReader.TryParseResetAfter(text, out _));
    }
}
