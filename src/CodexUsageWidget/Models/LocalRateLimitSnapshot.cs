namespace CodexUsageWidget.Models;

public sealed record LocalRateLimitSnapshot(
    DateTimeOffset RecordedAt,
    double UsedPercent,
    TimeSpan Window,
    DateTimeOffset ResetAt)
{
    public double RemainingPercent => Math.Clamp(100d - UsedPercent, 0d, 100d);

    public bool IsFiveHour => Window >= TimeSpan.FromHours(4) && Window <= TimeSpan.FromHours(6);

    public bool IsWeekly => Window >= TimeSpan.FromDays(6);
}
