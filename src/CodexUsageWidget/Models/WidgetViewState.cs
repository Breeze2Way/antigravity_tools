namespace CodexUsageWidget.Models;

public sealed record WidgetViewState(
    UsageSnapshot FiveHour,
    UsageSnapshot SevenDay,
    UsageSnapshot ThirtyDay,
    DateTimeOffset RefreshedAt,
    string Status,
    bool IsEstimate,
    double? OfficialRemainingPercent = null);
