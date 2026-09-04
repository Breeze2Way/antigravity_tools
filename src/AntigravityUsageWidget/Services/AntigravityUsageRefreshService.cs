using AntigravityUsageWidget.Data;
using AntigravityUsageWidget.Models;

namespace AntigravityUsageWidget.Services;

public sealed class AntigravityUsageRefreshService
{
    private readonly Func<AntigravityQuotaSnapshot?> read;
    private WidgetViewState? lastSuccessful;
    private AntigravityDisplayQuota? lastQuota;

    public AntigravityUsageRefreshService(Func<AntigravityQuotaSnapshot?> read)
    {
        this.read = read;
    }

    public WidgetViewState Refresh(DateTimeOffset now, bool refreshOfficial = true)
    {
        if (refreshOfficial)
        {
            try
            {
                var snapshot = read();
                if (snapshot is not null)
                {
                    lastQuota = AntigravityQuotaAggregator.Aggregate(snapshot);
                    lastSuccessful = CreateState(now, lastQuota, "Antigravity 官方配额");
                }
                else if (lastSuccessful is not null)
                {
                    return lastSuccessful with
                    {
                        Status = "Antigravity 配额暂不可用 · 保留上次数据"
                    };
                }
            }
            catch
            {
                if (lastSuccessful is not null)
                {
                    return lastSuccessful with
                    {
                        Status = "Antigravity 配额读取失败 · 保留上次数据"
                    };
                }
            }
        }

        return lastSuccessful ?? CreateState(
            now,
            lastQuota ?? new AntigravityDisplayQuota(null, null, null, null, null, []),
            "Antigravity 配额不可用 · 请启动 Antigravity");
    }

    private static WidgetViewState CreateState(
        DateTimeOffset now,
        AntigravityDisplayQuota quota,
        string status)
    {
        return new WidgetViewState(
            now,
            status,
            IsEstimate: false,
            OfficialRemainingPercent: quota.WeeklyRemainingPercent)
        {
            Quota = quota,
            ResetAt = quota.WeeklyResetAt,
            WeeklyResetAt = quota.WeeklyResetAt,
            FiveHourRemainingPercent = quota.ShortRemainingPercent,
            FiveHourResetAt = quota.ShortResetAt
        };
    }
}
