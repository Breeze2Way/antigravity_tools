using AntigravityUsageWidget.Models;
using AntigravityUsageWidget.Services;

namespace AntigravityUsageWidget.Tests;

public sealed class UsageRateCalculatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 12, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CalculatesRecentTokensPerMinuteFromRecordsInWindow()
    {
        var records = new[]
        {
            CreateRecord(Now.AddMinutes(-1), 1_000, "one"),
            CreateRecord(Now.AddMinutes(-3), 2_000, "three"),
            CreateRecord(Now.AddMinutes(-8), 9_000, "old")
        };

        var rate = UsageRateCalculator.CalculateTokensPerMinute(
            records,
            Now,
            TimeSpan.FromMinutes(5));

        Assert.Equal(600, rate, precision: 6);
    }

    [Fact]
    public void ConvertsCumulativeRecordsToRecentDeltaRate()
    {
        var records = new[]
        {
            CreateRecord(Now.AddMinutes(-4), 10_000, "first", "session.jsonl", true),
            CreateRecord(Now.AddMinutes(-2), 16_000, "second", "session.jsonl", true),
            CreateRecord(Now.AddMinutes(-1), 21_000, "third", "session.jsonl", true)
        };

        var rate = UsageRateCalculator.CalculateTokensPerMinute(
            records,
            Now,
            TimeSpan.FromMinutes(5));

        Assert.Equal(2_200, rate, precision: 6);
    }

    private static UsageRecord CreateRecord(
        DateTimeOffset timestamp,
        long totalTokens,
        string identity,
        string sourcePath = "session.jsonl",
        bool isCumulative = false)
    {
        return new UsageRecord(
            timestamp,
            new TokenUsage(totalTokens, 0, 0, 0, 0, totalTokens),
            sourcePath,
            identity,
            isCumulative);
    }
}
