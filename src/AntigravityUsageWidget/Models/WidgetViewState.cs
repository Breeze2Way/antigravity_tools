using AntigravityUsageWidget.Services;

namespace AntigravityUsageWidget.Models;

public sealed record WidgetViewState(
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
    public AntigravityDisplayQuota? Quota { get; init; }
}
