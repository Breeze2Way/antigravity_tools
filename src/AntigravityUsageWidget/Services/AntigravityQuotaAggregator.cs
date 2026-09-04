using AntigravityUsageWidget.Data;

namespace AntigravityUsageWidget.Services;

public sealed record AntigravityDisplayQuota(
    string? PlanName,
    double? ShortRemainingPercent,
    DateTimeOffset? ShortResetAt,
    double? WeeklyRemainingPercent,
    DateTimeOffset? WeeklyResetAt,
    IReadOnlyList<AntigravityQuotaRow> Rows);

public static class AntigravityQuotaAggregator
{
    public static AntigravityDisplayQuota Aggregate(AntigravityQuotaSnapshot snapshot)
    {
        var shortQuota = FindLowest(snapshot.Rows, AntigravityQuotaPeriod.Short);
        var weeklyQuota = FindLowest(snapshot.Rows, AntigravityQuotaPeriod.Weekly);
        return new AntigravityDisplayQuota(
            snapshot.PlanName,
            shortQuota?.RemainingPercent,
            shortQuota?.ResetAt,
            weeklyQuota?.RemainingPercent,
            weeklyQuota?.ResetAt,
            snapshot.Rows);
    }

    private static AntigravityQuotaRow? FindLowest(
        IReadOnlyList<AntigravityQuotaRow> rows,
        AntigravityQuotaPeriod period)
    {
        return rows
            .Where(row => row.Period == period)
            .OrderBy(row => row.RemainingPercent)
            .FirstOrDefault();
    }
}
