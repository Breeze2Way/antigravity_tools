using CodexUsageWidget.Data;
using CodexUsageWidget.Models;

namespace CodexUsageWidget.Services;

public sealed class UsageRefreshService
{
    private readonly Func<CodexDataPaths, DataReadResult> read;
    private readonly CodexDataPaths paths;
    private readonly Func<double?>? readOfficialRemainingPercent;
    private WidgetViewState? lastSuccessful;

    public UsageRefreshService(
        CodexDataReader reader,
        CodexDataPaths paths,
        Func<double?>? readOfficialRemainingPercent = null)
        : this(reader.Read, paths, readOfficialRemainingPercent)
    {
    }

    public UsageRefreshService(
        Func<CodexDataPaths, DataReadResult> read,
        CodexDataPaths paths,
        Func<double?>? readOfficialRemainingPercent = null)
    {
        this.read = read;
        this.paths = paths;
        this.readOfficialRemainingPercent = readOfficialRemainingPercent;
    }

    public WidgetViewState Refresh(DateTimeOffset now, WidgetSettings settings)
    {
        var result = read(paths);
        if (result.Warning is not null && result.Records.Count == 0 && lastSuccessful is not null)
        {
            return lastSuccessful with
            {
                Status = $"{result.Warning} · 保留上次数据"
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
        double? officialRemainingPercent = null;
        try
        {
            officialRemainingPercent = readOfficialRemainingPercent?.Invoke();
        }
        catch
        {
            // Local token data remains useful when the desktop UI is unavailable.
        }

        if (officialRemainingPercent.HasValue)
        {
            status = $"官方周剩余 {officialRemainingPercent.Value:0.#}%";
        }

        var state = new WidgetViewState(
            fiveHour,
            sevenDay,
            thirtyDay,
            now,
            status,
            IsEstimate: true,
            OfficialRemainingPercent: officialRemainingPercent);

        if (result.Warning is null)
        {
            lastSuccessful = state;
        }

        return state;
    }
}
