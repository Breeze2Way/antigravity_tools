using System.Globalization;
using AntigravityUsageWidget.Data;

namespace AntigravityUsageWidget.Services;

public static class AntigravityUsageDisplayFormatter
{
    public static string FormatTokenUsage(
        AntigravityTokenUsageSummary summary,
        bool english)
    {
        return english
            ? $"Today tokens:{FormatTokensInMillions(summary.TodayTokens)} (Yesterday:{FormatTokensInMillions(summary.YesterdayTokens)})"
            : $"今日token:{FormatTokensInMillions(summary.TodayTokens)}(昨日：{FormatTokensInMillions(summary.YesterdayTokens)})";
    }

    public static string FormatTooltipDetails(
        AntigravityDisplayQuota quota,
        DateTimeOffset refreshedAt,
        bool english,
        AntigravityTokenUsageSummary? tokenUsage = null)
    {
        var lines = new List<string>();
        if (tokenUsage is not null)
        {
            lines.Add(FormatTokenUsage(tokenUsage, english));
        }

        var groups = quota.Rows
            .GroupBy(row => row.Group ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (groups.Count == 0)
        {
            lines.Add(FormatGroupQuotaLine(
                english ? "Models" : "模型",
                quota.Rows,
                quota.ShortRemainingPercent,
                quota.WeeklyRemainingPercent,
                english));
        }
        else
        {
            foreach (var group in groups)
            {
                lines.Add(FormatGroupQuotaLine(
                    FormatGroupName(group.Key, english),
                    group,
                    null,
                    null,
                    english));
            }
        }

        lines.Add(string.Empty);
        lines.Add($"{(english ? "Updated" : "更新时间")}:{refreshedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss}");
        return string.Join(Environment.NewLine, lines);
    }

    private static string FormatGroupName(string group, bool english)
    {
        if (group.Contains("Gemini", StringComparison.OrdinalIgnoreCase))
        {
            return "Gemini";
        }

        if (group.Contains("Claude", StringComparison.OrdinalIgnoreCase) ||
            group.Contains("GPT", StringComparison.OrdinalIgnoreCase))
        {
            return "Claude";
        }

        return string.IsNullOrWhiteSpace(group)
            ? english ? "Models" : "模型"
            : group.Trim();
    }

    private static string FormatGroupQuotaLine(
        string groupName,
        IEnumerable<AntigravityQuotaRow> rows,
        double? fallbackShortPercent,
        double? fallbackWeeklyPercent,
        bool english)
    {
        var shortPercent = rows
            .Where(candidate => candidate.Period == AntigravityQuotaPeriod.Short)
            .Select(candidate => (double?)candidate.RemainingPercent)
            .OrderBy(value => value)
            .FirstOrDefault() ?? fallbackShortPercent;
        var weeklyPercent = rows
            .Where(candidate => candidate.Period == AntigravityQuotaPeriod.Weekly)
            .Select(candidate => (double?)candidate.RemainingPercent)
            .OrderBy(value => value)
            .FirstOrDefault() ?? fallbackWeeklyPercent;
        var shortLabel = "5h";
        var weeklyLabel = english ? "Weekly" : "周";
        return $"{groupName} : [{shortLabel}:{FormatPercentOrUnavailable(shortPercent, english)}] " +
               $"[{weeklyLabel}:{FormatPercentOrUnavailable(weeklyPercent, english)}]";
    }

    private static string FormatPercentOrUnavailable(double? value, bool english)
    {
        return value.HasValue
            ? FormatPercent(value.Value)
            : english ? "unavailable" : "不可用";
    }

    private static string FormatPercent(double value)
    {
        return $"{value.ToString("0.#", CultureInfo.InvariantCulture)}%";
    }

    private static string FormatTokensInMillions(long tokens)
    {
        return $"{(tokens / 1_000_000d).ToString("0.0", CultureInfo.InvariantCulture)}M";
    }

    public static string? FormatResetDetails(
        DateTimeOffset? shortResetAt,
        DateTimeOffset? weeklyResetAt,
        DateTimeOffset now,
        bool english = false)
    {
        var details = new List<string>();
        AddReset(details, shortResetAt, now, english, english ? "5-hour reset" : "五小时重置时间");
        AddReset(details, weeklyResetAt, now, english, english ? "Weekly reset" : "周重置时间");
        return details.Count == 0 ? null : string.Join(Environment.NewLine, details);
    }

    private static void AddReset(
        List<string> details,
        DateTimeOffset? resetAt,
        DateTimeOffset now,
        bool english,
        string label)
    {
        if (!resetAt.HasValue)
        {
            return;
        }

        var remainingHours = Math.Max(0, Math.Floor((resetAt.Value - now).TotalHours));
        var hours = remainingHours.ToString("0", CultureInfo.InvariantCulture);
        details.Add(english
            ? $"{label}: {resetAt.Value.ToLocalTime():yyyy-MM-dd HH:mm} [{hours}h remaining]"
            : $"{label}:{resetAt.Value.ToLocalTime():yyyy-MM-dd HH:mm} [剩余 {hours}h]");
    }
}
