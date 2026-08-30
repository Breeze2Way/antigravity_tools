using CodexUsageWidget.Data;
using CodexUsageWidget.Models;

namespace CodexUsageWidget.Services;

public sealed class UsageRefreshService
{
    private readonly Func<CodexDataPaths, DataReadResult> read;
    private readonly CodexDataPaths paths;
    private readonly Func<OfficialUsageSnapshot?>? readOfficialUsage;
    private WidgetViewState? lastSuccessful;
    private double? lastFiveHourRemainingPercent;
    private DateTimeOffset? lastFiveHourResetAt;
    private double? lastOfficialFiveHourRemainingPercent;
    private DateTimeOffset? lastOfficialFiveHourResetAt;
    private double? lastOfficialRemainingPercent;
    private DateTimeOffset? lastResetAt;

    public UsageRefreshService(
        CodexDataReader reader,
        CodexDataPaths paths,
        Func<double?>? readOfficialRemainingPercent = null)
        : this(reader.Read, paths, AdaptOfficialPercentReader(readOfficialRemainingPercent))
    {
    }

    public UsageRefreshService(
        CodexDataReader reader,
        CodexDataPaths paths,
        Func<OfficialUsageSnapshot?>? readOfficialUsage)
        : this(reader.Read, paths, readOfficialUsage)
    {
    }

    public UsageRefreshService(
        Func<CodexDataPaths, DataReadResult> read,
        CodexDataPaths paths,
        Func<double?>? readOfficialRemainingPercent = null)
        : this(read, paths, AdaptOfficialPercentReader(readOfficialRemainingPercent))
    {
    }

    public UsageRefreshService(
        Func<CodexDataPaths, DataReadResult> read,
        CodexDataPaths paths,
        Func<OfficialUsageSnapshot?>? readOfficialUsage)
    {
        this.read = read;
        this.paths = paths;
        this.readOfficialUsage = readOfficialUsage;
    }

    public WidgetViewState Refresh(
        DateTimeOffset now,
        WidgetSettings settings,
        bool refreshOfficial = true)
    {
        var result = read(paths);
        var fiveHourRemainingPercent = lastFiveHourRemainingPercent;
        var weeklyRemainingPercent = lastOfficialRemainingPercent;
        var fiveHourResetAt = lastFiveHourResetAt;
        var weeklyResetAt = lastResetAt;
        double? localFiveHourRemainingPercent = null;
        DateTimeOffset? localFiveHourResetAt = null;
        double? localWeeklyRemainingPercent = null;
        DateTimeOffset? localWeeklyResetAt = null;
        var hasLocalRateLimit = false;
        var hasLocalWeeklyRateLimit = false;

        if (result.LatestFiveHourRateLimit is { } localFiveHourRateLimit)
        {
            lastFiveHourRemainingPercent = localFiveHourRateLimit.RemainingPercent;
            localFiveHourRemainingPercent = localFiveHourRateLimit.RemainingPercent;
            lastFiveHourResetAt = localFiveHourRateLimit.ResetAt;
            localFiveHourResetAt = localFiveHourRateLimit.ResetAt;
            hasLocalRateLimit = true;
        }

        if (result.LatestWeeklyRateLimit is { } localWeeklyRateLimit)
        {
            localWeeklyRemainingPercent = localWeeklyRateLimit.RemainingPercent;
            localWeeklyResetAt = localWeeklyRateLimit.ResetAt;
            hasLocalRateLimit = true;
            hasLocalWeeklyRateLimit = true;
        }

        if (result.LatestRateLimit is { } legacyRateLimit &&
            result.LatestFiveHourRateLimit is null &&
            result.LatestWeeklyRateLimit is null)
        {
            if (legacyRateLimit.IsFiveHour)
            {
                lastFiveHourRemainingPercent = legacyRateLimit.RemainingPercent;
                localFiveHourRemainingPercent = legacyRateLimit.RemainingPercent;
                lastFiveHourResetAt = legacyRateLimit.ResetAt;
                localFiveHourResetAt = legacyRateLimit.ResetAt;
            }
            else
            {
                localWeeklyRemainingPercent = legacyRateLimit.RemainingPercent;
                localWeeklyResetAt = legacyRateLimit.ResetAt;
            }

            hasLocalRateLimit = true;
            hasLocalWeeklyRateLimit = !legacyRateLimit.IsFiveHour;
        }

        var officialPercentRead = false;
        if (refreshOfficial)
        {
            try
            {
                var currentOfficialUsage = readOfficialUsage?.Invoke();
                if (currentOfficialUsage is not null)
                {
                    if (currentOfficialUsage.RemainingPercent.HasValue)
                    {
                        lastOfficialRemainingPercent = currentOfficialUsage.RemainingPercent;
                        officialPercentRead = true;
                    }

                    if (currentOfficialUsage.FiveHourRemainingPercent.HasValue)
                    {
                        lastOfficialFiveHourRemainingPercent = currentOfficialUsage.FiveHourRemainingPercent;
                    }

                    if (currentOfficialUsage.ResetAfter.HasValue)
                    {
                        lastResetAt = now + currentOfficialUsage.ResetAfter.Value;
                    }

                    if (currentOfficialUsage.FiveHourResetAfter.HasValue)
                    {
                        lastOfficialFiveHourResetAt = now + currentOfficialUsage.FiveHourResetAfter.Value;
                    }
                }
            }
            catch
            {
                // Keep the last successful official value when the UI is unavailable.
            }
        }
        fiveHourRemainingPercent = lastOfficialFiveHourRemainingPercent ??
            localFiveHourRemainingPercent ??
            lastFiveHourRemainingPercent;
        fiveHourResetAt = lastOfficialFiveHourResetAt ??
            localFiveHourResetAt ??
            lastFiveHourResetAt;
        weeklyRemainingPercent = lastOfficialRemainingPercent ?? localWeeklyRemainingPercent;
        weeklyResetAt = lastResetAt ?? localWeeklyResetAt;
        var recentTokensPerMinute = UsageRateCalculator.CalculateTokensPerMinute(
            result.Records,
            now,
            TimeSpan.FromMinutes(5));
        if (result.Warning is not null && result.Records.Count == 0 && lastSuccessful is not null)
        {
            return lastSuccessful with
            {
                Status = $"{result.Warning} · 保留上次数据",
                OfficialRemainingPercent = weeklyRemainingPercent,
                ResetAt = weeklyResetAt,
                WeeklyResetAt = weeklyResetAt,
                FiveHourRemainingPercent = fiveHourRemainingPercent,
                FiveHourResetAt = fiveHourResetAt,
                RecentTokensPerMinute = lastSuccessful.RecentTokensPerMinute
            };
        }

        var budgetTokens = settings.WeeklyBudgetConfigured ? settings.WeeklyBudgetTokens : 0;
        var fiveHour = UsageCalculator.Aggregate(
            result.Records,
            now,
            TimeSpan.FromHours(5),
            budgetTokens);
        var sevenDay = UsageCalculator.Aggregate(
            result.Records,
            now,
            TimeSpan.FromDays(7),
            budgetTokens);
        var thirtyDay = UsageCalculator.Aggregate(
            result.Records,
            now,
            TimeSpan.FromDays(30),
            budgetTokens);
        var todayTokens = UsageCalculator.SumTokensForLocalCalendarDate(result.Records, now, daysAgo: 0);
        var yesterdayTokens = UsageCalculator.SumTokensForLocalCalendarDate(result.Records, now, daysAgo: 1);
        var status = result.Warning ?? (result.Records.Count == 0 ? "暂无记录 · 本地估算" : "本地估算");
        if (fiveHourRemainingPercent.HasValue || weeklyRemainingPercent.HasValue)
        {
            var statusParts = new List<string>();
            if (fiveHourRemainingPercent.HasValue)
            {
                statusParts.Add(lastOfficialFiveHourRemainingPercent.HasValue
                    ? $"官方五小时剩余 {fiveHourRemainingPercent.Value:0.#}%"
                    : hasLocalRateLimit
                    ? $"本地五小时剩余 {fiveHourRemainingPercent.Value:0.#}%"
                    : $"五小时剩余 {fiveHourRemainingPercent.Value:0.#}%");
            }

            if (weeklyRemainingPercent.HasValue)
            {
                statusParts.Add(hasLocalWeeklyRateLimit && !officialPercentRead
                    ? $"本地周剩余 {weeklyRemainingPercent.Value:0.#}%"
                    : $"官方周剩余 {weeklyRemainingPercent.Value:0.#}%");
            }

            status = string.Join(" · ", statusParts);
        }

        var state = new WidgetViewState(
            fiveHour,
            sevenDay,
            thirtyDay,
            now,
            status,
            IsEstimate: true,
            OfficialRemainingPercent: weeklyRemainingPercent)
        {
            ResetAt = weeklyResetAt,
            WeeklyResetAt = weeklyResetAt,
            FiveHourRemainingPercent = fiveHourRemainingPercent,
            FiveHourResetAt = fiveHourResetAt,
            RecentTokensPerMinute = recentTokensPerMinute,
            TodayTokens = todayTokens,
            YesterdayTokens = yesterdayTokens
        };

        if (result.Warning is null)
        {
            lastSuccessful = state;
        }

        return state;
    }

    public WidgetViewState RefreshLocal(DateTimeOffset now, WidgetSettings settings)
    {
        return Refresh(now, settings, refreshOfficial: false);
    }

    private static Func<OfficialUsageSnapshot?>? AdaptOfficialPercentReader(
        Func<double?>? readOfficialRemainingPercent)
    {
        if (readOfficialRemainingPercent is null)
        {
            return null;
        }

        return ()
            => readOfficialRemainingPercent() is { } percentage
                ? new OfficialUsageSnapshot(percentage, null)
                : null;
    }
}
