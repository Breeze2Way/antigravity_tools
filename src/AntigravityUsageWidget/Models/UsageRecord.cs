namespace AntigravityUsageWidget.Models;

public sealed record UsageRecord(
    DateTimeOffset Timestamp,
    TokenUsage Usage,
    string SourcePath,
    string Identity,
    bool IsCumulative);
