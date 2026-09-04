namespace AntigravityUsageWidget.Tests;

public sealed class UsageCalculatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 11, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void AggregatesOnlyRecordsInsideWindowAndIgnoresDuplicates()
    {
        var inside = CreateRecord(Now.AddHours(-4), 10, "inside");
        var outside = CreateRecord(Now.AddHours(-5).AddMinutes(-1), 100, "outside");

        var snapshot = UsageCalculator.Aggregate(
            [inside, inside with { Identity = inside.Identity }, outside],
            Now,
            TimeSpan.FromHours(5),
            100);

        Assert.True(snapshot.HasData);
        Assert.Equal(10, snapshot.Usage.TotalTokens);
        Assert.Equal(90, snapshot.RemainingPercent, precision: 6);
        Assert.Equal(1, snapshot.RecordCount);
    }

    [Fact]
    public void ClampsRemainingPercentageToZeroWhenOverBudget()
    {
        var snapshot = UsageCalculator.Aggregate(
            [CreateRecord(Now.AddMinutes(-1), 120, "over")],
            Now,
            TimeSpan.FromHours(5),
            100);

        Assert.Equal(100, snapshot.UsedPercent, precision: 6);
        Assert.Equal(0, snapshot.RemainingPercent, precision: 6);
    }

    [Fact]
    public void ReturnsEmptySnapshotWhenWindowHasNoRecords()
    {
        var snapshot = UsageCalculator.Aggregate(
            [CreateRecord(Now.AddDays(-1), 10, "old")],
            Now,
            TimeSpan.FromHours(5),
            100);

        Assert.False(snapshot.HasData);
        Assert.Equal(TokenUsage.Zero, snapshot.Usage);
        Assert.Equal(100, snapshot.RemainingPercent, precision: 6);
    }

    [Fact]
    public void ConvertsCumulativeRecordsToPositiveDeltas()
    {
        var source = "session.jsonl";
        var records = new[]
        {
            CreateRecord(Now.AddMinutes(-3), 100, "first", source, true),
            CreateRecord(Now.AddMinutes(-2), 140, "second", source, true),
            CreateRecord(Now.AddMinutes(-1), 190, "third", source, true)
        };

        var normalized = UsageCalculator.Normalize(records);

        Assert.Equal(3, normalized.Count);
        Assert.Equal(90, normalized.Sum(item => item.Usage.TotalTokens));
        Assert.All(normalized, item => Assert.False(item.IsCumulative));
    }

    [Fact]
    public void UsesTotalTokensForUsageAndBudgetPercentage()
    {
        var record = new UsageRecord(
            Now.AddMinutes(-1),
            new TokenUsage(1_000, 900, 0, 100, 40, 1_100),
            "session.jsonl",
            "cached",
            false);

        var snapshot = UsageCalculator.Aggregate([record], Now, TimeSpan.FromHours(5), 1_000);

        Assert.Equal(1_100, snapshot.Usage.TotalTokens);
        Assert.Equal(0, snapshot.RemainingPercent, precision: 6);
    }

    [Fact]
    public void SumsTokensByLocalCalendarDate()
    {
        var now = new DateTimeOffset(2026, 8, 11, 12, 0, 0, TimeSpan.FromHours(8));
        var records = new[]
        {
            CreateRecord(new DateTimeOffset(2026, 8, 11, 9, 0, 0, TimeSpan.FromHours(8)), 10, "today"),
            CreateRecord(new DateTimeOffset(2026, 8, 10, 23, 30, 0, TimeSpan.FromHours(8)), 200, "yesterday"),
            CreateRecord(new DateTimeOffset(2026, 8, 9, 23, 30, 0, TimeSpan.FromHours(8)), 900, "older")
        };

        Assert.Equal(10, UsageCalculator.SumTokensForLocalCalendarDate(records, now, daysAgo: 0));
        Assert.Equal(200, UsageCalculator.SumTokensForLocalCalendarDate(records, now, daysAgo: 1));
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
