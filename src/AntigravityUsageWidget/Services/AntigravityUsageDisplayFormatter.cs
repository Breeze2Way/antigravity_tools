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
        var lines = new List<string>
        {
            FormatAggregate("Weekly quota", "周额度", quota.WeeklyRemainingPercent, english),
            FormatAggregate("5-hour quota", "五小时额度", quota.ShortRemainingPercent, english),
            $"{(english ? "Updated" : "更新时间")}: {refreshedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss}"
        };
        return string.Join(Environment.NewLine, lines);
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
