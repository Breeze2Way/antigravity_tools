namespace AntigravityUsageWidget.Models;

public sealed record WidgetViewState(
    UsageSnapshot FiveHour,
    UsageSnapshot SevenDay,
    UsageSnapshot ThirtyDay,
    DateTimeOffset RefreshedAt,
    string Status,
    bool IsEstimate,
    double? OfficialRemainingPercent = null)
{
    public DateTimeOffset? ResetAt { get; init; }
    public double? FiveHourRemainingPercent { get; init; }
    public DateTimeOffset? FiveHourResetAt { get; init; }
    public DateTimeOffset? WeeklyResetAt { get; init; }
    public double RecentTokensPerMinute { get; init; }
    public long TodayTokens { get; init; }
    public long YesterdayTokens { get; init; }
}
