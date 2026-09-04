namespace AntigravityUsageWidget.Tests;

public sealed class UsageJsonParserTests
{
    [Fact]
    public void ParsesLastTokenUsageIntoARecord()
    {
        const string json = "{\"timestamp\":\"2026-08-11T08:00:00Z\",\"type\":\"event_msg\",\"payload\":{\"info\":{\"last_token_usage\":{\"input_tokens\":10,\"cached_input_tokens\":4,\"cache_write_input_tokens\":1,\"output_tokens\":3,\"reasoning_output_tokens\":2,\"total_tokens\":13}}}}";

        Assert.True(UsageJsonParser.TryParse(json, "session.jsonl", out var record));
        Assert.Equal(new TokenUsage(10, 4, 1, 3, 2, 13), record!.Usage);
        Assert.Equal(DateTimeOffset.Parse("2026-08-11T08:00:00Z"), record.Timestamp);
        Assert.False(record.IsCumulative);
    }

    [Fact]
    public void ParsesCumulativeTokenUsageAsCumulative()
    {
        const string json = "{\"timestamp\":\"2026-08-11T08:00:00Z\",\"payload\":{\"info\":{\"total_token_usage\":{\"input_tokens\":100,\"cached_input_tokens\":40,\"cache_write_input_tokens\":0,\"output_tokens\":10,\"reasoning_output_tokens\":5,\"total_tokens\":110}}}}";

        Assert.True(UsageJsonParser.TryParse(json, "session.jsonl", out var record));
        Assert.Equal(new TokenUsage(100, 40, 0, 10, 5, 110), record!.Usage);
        Assert.True(record.IsCumulative);
    }

    [Fact]
    public void PrefersCumulativeUsageWhenBothUsageShapesArePresent()
    {
        const string json = "{\"timestamp\":\"2026-08-11T08:00:00Z\",\"payload\":{\"info\":{\"total_token_usage\":{\"input_tokens\":1000,\"cached_input_tokens\":400,\"cache_write_input_tokens\":0,\"output_tokens\":100,\"reasoning_output_tokens\":20,\"total_tokens\":1100},\"last_token_usage\":{\"input_tokens\":10,\"cached_input_tokens\":4,\"cache_write_input_tokens\":0,\"output_tokens\":3,\"reasoning_output_tokens\":2,\"total_tokens\":13}}}}";

        Assert.True(UsageJsonParser.TryParse(json, "session.jsonl", out var record));
        Assert.Equal(new TokenUsage(1000, 400, 0, 100, 20, 1100), record!.Usage);
        Assert.True(record.IsCumulative);
    }

    [Fact]
    public void ReturnsFalseForMalformedOrUnrelatedJson()
    {
        Assert.False(UsageJsonParser.TryParse("{not-json", "session.jsonl", out _));
        Assert.False(UsageJsonParser.TryParse("{\"timestamp\":\"2026-08-11T08:00:00Z\",\"payload\":{}}", "session.jsonl", out _));
    }

    [Fact]
    public void ParsesPrimaryRateLimitFromSessionMetadata()
    {
        const string json = "{\"timestamp\":\"2026-08-11T08:00:00Z\",\"rate_limits\":{\"primary\":{\"used_percent\":12.5,\"window_minutes\":10080,\"resets_at\":1788144616}}}";

        Assert.True(UsageJsonParser.TryParseRateLimit(json, out var snapshot));
        Assert.Equal(12.5, snapshot!.UsedPercent, precision: 6);
        Assert.Equal(87.5, snapshot.RemainingPercent, precision: 6);
        Assert.Equal(TimeSpan.FromMinutes(10080), snapshot.Window);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1788144616), snapshot.ResetAt);
    }

    [Fact]
    public void ParsesPrimaryRateLimitFromTheActualPayloadShape()
    {
        const string json = "{\"timestamp\":\"2026-08-24T03:18:11Z\",\"payload\":{\"rate_limits\":{\"primary\":{\"used_percent\":1,\"window_minutes\":10080,\"resets_at\":1788144616}}}}";

        Assert.True(UsageJsonParser.TryParseRateLimit(json, out var snapshot));
        Assert.Equal(1, snapshot!.UsedPercent, precision: 6);
        Assert.Equal(99, snapshot.RemainingPercent, precision: 6);
    }

    [Fact]
    public void ParsesShortAndWeeklyRateLimitsByWindowLength()
    {
        const string json = "{\"timestamp\":\"2026-08-26T02:02:53Z\",\"payload\":{\"rate_limits\":{\"primary\":{\"used_percent\":7,\"window_minutes\":300,\"resets_at\":1787718550},\"secondary\":{\"used_percent\":1,\"window_minutes\":10080,\"resets_at\":1788305350}}}}";

        Assert.True(UsageJsonParser.TryParseRateLimits(json, out var snapshots));
        Assert.Equal(2, snapshots.Count);
        Assert.Equal(93, snapshots.Single(snapshot => snapshot.IsFiveHour).RemainingPercent, precision: 6);
        Assert.Equal(99, snapshots.Single(snapshot => snapshot.IsWeekly).RemainingPercent, precision: 6);
    }
}
