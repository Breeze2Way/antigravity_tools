namespace CodexUsageWidget.Models;

public sealed record UsageSnapshot(
    TokenUsage Usage,
    int RecordCount,
    bool HasData,
    double UsedPercent,
    double RemainingPercent);
