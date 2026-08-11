namespace CodexUsageWidget.Tests;

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
}
