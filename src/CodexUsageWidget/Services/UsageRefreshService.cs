using CodexUsageWidget.Data;
using CodexUsageWidget.Models;

namespace CodexUsageWidget.Services;

public sealed class UsageRefreshService
{
    private readonly Func<CodexDataPaths, DataReadResult> read;
    private readonly CodexDataPaths paths;
    private readonly Func<OfficialUsageSnapshot?>? readOfficialUsage;
    private WidgetViewState? lastSuccessful;
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
        var officialRemainingPercent = lastOfficialRemainingPercent;
        var resetAt = lastResetAt;
        var hasLocalRateLimit = result.LatestRateLimit is not null;
        if (result.LatestRateLimit is { } localRateLimit)
        {
            lastOfficialRemainingPercent = localRateLimit.RemainingPercent;
            officialRemainingPercent = localRateLimit.RemainingPercent;
            lastResetAt = localRateLimit.ResetAt;
            resetAt = localRateLimit.ResetAt;
        }
        else if (refreshOfficial)
        {
            try
            {
                var currentOfficialUsage = readOfficialUsage?.Invoke();
                if (currentOfficialUsage is not null)
                {
                    if (currentOfficialUsage.RemainingPercent.HasValue)
                    {
                        lastOfficialRemainingPercent = currentOfficialUsage.RemainingPercent;
                        officialRemainingPercent = currentOfficialUsage.RemainingPercent;
                    }

                    if (currentOfficialUsage.ResetAfter.HasValue)
                    {
                        lastResetAt = now + currentOfficialUsage.ResetAfter.Value;
                        resetAt = lastResetAt;
                    }
                }
            }
            catch
            {
                // Keep the last successful official value when the UI is unavailable.
            }
        }
        var recentTokensPerMinute = UsageRateCalculator.CalculateTokensPerMinute(
            result.Records,
            now,
            TimeSpan.FromMinutes(5));
        if (result.Warning is not null && result.Records.Count == 0 && lastSuccessful is not null)
        {
            return lastSuccessful with
            {
                Status = $"{result.Warning} · 保留上次数据",
                OfficialRemainingPercent = officialRemainingPercent,
                ResetAt = resetAt,
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
        var status = result.Warning ?? (result.Records.Count == 0 ? "暂无记录 · 本地估算" : "本地估算");
        if (officialRemainingPercent.HasValue)
        {
            status = hasLocalRateLimit
                ? $"本地周剩余 {officialRemainingPercent.Value:0.#}%"
                : $"官方周剩余 {officialRemainingPercent.Value:0.#}%";
        }

        var state = new WidgetViewState(
            fiveHour,
            sevenDay,
            thirtyDay,
            now,
            status,
            IsEstimate: true,
            OfficialRemainingPercent: officialRemainingPercent)
        {
            ResetAt = resetAt,
            RecentTokensPerMinute = recentTokensPerMinute
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
