using System.Globalization;
using AntigravityUsageWidget.Data;

namespace AntigravityUsageWidget.Services;

public static class AntigravityUsageDisplayFormatter
{
    public static string FormatTooltipDetails(
        AntigravityDisplayQuota quota,
        DateTimeOffset refreshedAt,
        bool english)
    {
        var lines = new List<string>();
        var groups = quota.Rows
            .GroupBy(row => row.Group ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (groups.Count == 0)
        {
            lines.Add(FormatAggregate("Weekly quota", "周额度", quota.WeeklyRemainingPercent, english));
            lines.Add(FormatAggregate("5-hour quota", "五小时额度", quota.ShortRemainingPercent, english));
        }
        else
        {
            foreach (var group in groups)
            {
                lines.Add(FormatGroupName(group.Key, english));
                lines.Add(FormatGroupQuota(group, AntigravityQuotaPeriod.Weekly, english));
                lines.Add(FormatGroupQuota(group, AntigravityQuotaPeriod.Short, english));
            }
        }

        lines.Add($"{(english ? "Updated" : "更新时间")}: {refreshedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss}");
        return string.Join(Environment.NewLine, lines);
    }

    private static string FormatGroupName(string group, bool english)
    {
        if (english)
        {
            return group switch
            {
                var value when value.Equals("Gemini Models", StringComparison.OrdinalIgnoreCase)
                    => "Gemini models",
                var value when value.Equals("Claude and GPT models", StringComparison.OrdinalIgnoreCase)
                    => "Claude and GPT models",
                _ => string.IsNullOrWhiteSpace(group) ? "Models" : group
            };
        }

        return group switch
        {
            var value when value.Equals("Gemini Models", StringComparison.OrdinalIgnoreCase)
                => "Gemini 模型",
            var value when value.Equals("Claude and GPT models", StringComparison.OrdinalIgnoreCase)
                => "Claude/GPT 模型",
            _ => "模型组"
        };
    }

    private static string FormatGroupQuota(
        IEnumerable<AntigravityQuotaRow> rows,
        AntigravityQuotaPeriod period,
        bool english)
    {
        var row = rows
            .Where(candidate => candidate.Period == period)
            .OrderBy(candidate => candidate.RemainingPercent)
            .FirstOrDefault();
        var label = period == AntigravityQuotaPeriod.Weekly
            ? english ? "Weekly quota" : "周额度"
            : english ? "5-hour quota" : "五小时额度";
        return row is null
            ? $"  {label}: {(english ? "unavailable" : "不可用")}"
            : $"  {label}: {FormatPercent(row.RemainingPercent)}";
    }

    private static string FormatAggregate(
        string englishLabel,
        string chineseLabel,
        double? remainingPercent,
        bool english)
    {
        var label = english ? englishLabel : chineseLabel;
        return remainingPercent.HasValue
            ? $"{label}: {FormatPercent(remainingPercent.Value)}"
            : $"{label}: {(english ? "unavailable" : "不可用")}";
    }

    private static string FormatPercent(double value)
    {
        return $"{value.ToString("0.#", CultureInfo.InvariantCulture)}%";
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
            : $"{label}：{resetAt.Value.ToLocalTime():yyyy-MM-dd HH:mm} [剩余 {hours}h]");
    }
}
