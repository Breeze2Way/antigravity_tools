using AntigravityUsageWidget.Data;
using AntigravityUsageWidget.Models;

namespace AntigravityUsageWidget.Services;

public sealed class AntigravityUsageRefreshService
{
    private readonly Func<AntigravityQuotaSnapshot?> read;
    private readonly Func<DateTimeOffset, AntigravityTokenUsageSummary> readTokenUsage;
    private WidgetViewState? lastSuccessful;
    private AntigravityDisplayQuota? lastQuota;
    private AntigravityTokenUsageSummary lastTokenUsage = new(0, 0);

    public AntigravityUsageRefreshService(
        Func<AntigravityQuotaSnapshot?> read,
        Func<DateTimeOffset, AntigravityTokenUsageSummary>? readTokenUsage = null)
    {
        this.read = read;
        this.readTokenUsage = readTokenUsage ?? (_ => new AntigravityTokenUsageSummary(0, 0));
    }

    public WidgetViewState Refresh(DateTimeOffset now, bool refreshOfficial = true)
    {
        try
        {
            lastTokenUsage = readTokenUsage(now);
        }
        catch
        {
            // Token history is supplementary; quota refresh should remain available.
        }

        if (refreshOfficial)
        {
            try
            {
                var snapshot = read();
                if (snapshot is not null)
                {
                    lastQuota = AntigravityQuotaAggregator.Aggregate(snapshot);
                    lastSuccessful = CreateState(now, lastQuota, lastTokenUsage, "Antigravity 官方配额");
                }
                else if (lastSuccessful is not null)
                {
                    return WithTokenUsage(lastSuccessful with
                    {
                        Status = "Antigravity 配额暂不可用 · 保留上次数据"
                    });
                }
            }
            catch
            {
                if (lastSuccessful is not null)
                {
                    return WithTokenUsage(lastSuccessful with
                    {
                        Status = "Antigravity 配额读取失败 · 保留上次数据"
                    });
                }
            }
        }

        return lastSuccessful is not null
            ? WithTokenUsage(lastSuccessful)
            : CreateState(
                now,
                lastQuota ?? new AntigravityDisplayQuota(null, null, null, null, null, []),
                lastTokenUsage,
                "Antigravity 配额不可用 · 请启动 Antigravity");
    }

    private static WidgetViewState CreateState(
        DateTimeOffset now,
        AntigravityDisplayQuota quota,
        AntigravityTokenUsageSummary tokenUsage,
        string status)
    {
        return new WidgetViewState(
            now,
            status,
            IsEstimate: false,
            OfficialRemainingPercent: quota.WeeklyRemainingPercent)
        {
            Quota = quota,
            TodayTokens = tokenUsage.TodayTokens,
            YesterdayTokens = tokenUsage.YesterdayTokens,
            ResetAt = quota.WeeklyResetAt,
            WeeklyResetAt = quota.WeeklyResetAt,
            FiveHourRemainingPercent = quota.ShortRemainingPercent,
            FiveHourResetAt = quota.ShortResetAt
        };
    }

    private WidgetViewState WithTokenUsage(WidgetViewState state)
    {
        return state with
        {
            TodayTokens = lastTokenUsage.TodayTokens,
            YesterdayTokens = lastTokenUsage.YesterdayTokens
        };
    }
}
