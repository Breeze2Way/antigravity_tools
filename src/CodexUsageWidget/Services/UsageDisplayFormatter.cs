using System.Globalization;

namespace CodexUsageWidget.Services;

public static class UsageDisplayFormatter
{
    public static string FormatMillions(long tokens)
    {
        var millions = Math.Max(0, tokens) / 1_000_000d;
        return $"{millions.ToString("0.0", CultureInfo.InvariantCulture)}M";
    }

    public static string FormatCompactMillions(long tokens)
    {
        var millions = Math.Max(0, tokens) / 1_000_000d;
        return $"{millions.ToString("0.##", CultureInfo.InvariantCulture)}M";
    }

    public static string FormatTooltipDetails(
        string officialText,
        long sevenDayTokens,
        long thirtyDayTokens,
        DateTimeOffset refreshedAt)
    {
        return FormatTooltipDetails(
            "--",
            officialText,
            todayTokens: 0,
            yesterdayTokens: 0,
            sevenDayTokens,
            thirtyDayTokens,
            refreshedAt);
    }

    public static string FormatTooltipDetails(
        string fiveHourText,
        string weeklyText,
        long sevenDayTokens,
        long thirtyDayTokens,
        DateTimeOffset refreshedAt)
    {
        return FormatTooltipDetails(
            fiveHourText,
            weeklyText,
            todayTokens: 0,
            yesterdayTokens: 0,
            sevenDayTokens,
            thirtyDayTokens,
            refreshedAt);
    }

    public static string FormatTooltipDetails(
        string fiveHourText,
        string weeklyText,
        long todayTokens,
        long yesterdayTokens,
        long sevenDayTokens,
        long thirtyDayTokens,
        DateTimeOffset refreshedAt)
    {
        return string.Join(
            Environment.NewLine,
            $"五小时剩余：{fiveHourText}",
            $"周剩余：{weeklyText}",
            $"用量：当日{FormatCompactMillions(todayTokens)}  昨日{FormatCompactMillions(yesterdayTokens)}",
            $"近 7 天总量：{FormatMillions(sevenDayTokens)}",
            $"近 30 天总量：{FormatMillions(thirtyDayTokens)}",
            $"更新时间：{refreshedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss}");
    }

    public static string FormatRemainingPercent(double percent, bool hasBudget)
    {
        if (!hasBudget || !double.IsFinite(percent))
        {
            return "--";
        }

        return $"{Math.Clamp(percent, 0, 100).ToString("0.#", CultureInfo.InvariantCulture)}%";
    }

    public static string? FormatResetDetails(DateTimeOffset? resetAt, DateTimeOffset now)
    {
        if (!resetAt.HasValue)
        {
            return null;
        }

        var remaining = resetAt.Value - now;
        var remainingHours = Math.Max(0, Math.Floor(remaining.TotalHours));
        var localResetAt = resetAt.Value.ToOffset(now.Offset);
        return $"重置时间：{localResetAt:yyyy-MM-dd HH:mm} [剩余 {remainingHours.ToString("0", CultureInfo.InvariantCulture)}h]";
    }

    public static string? FormatResetDetails(
        DateTimeOffset? fiveHourResetAt,
        DateTimeOffset? weeklyResetAt,
        DateTimeOffset now)
    {
        var details = new List<string>();
        var fiveHourDetails = FormatResetDetails(fiveHourResetAt, now);
        if (fiveHourDetails is not null)
        {
            details.Add(fiveHourDetails.Replace("重置时间：", "五小时重置时间：", StringComparison.Ordinal));
        }

        var weeklyDetails = FormatResetDetails(weeklyResetAt, now);
        if (weeklyDetails is not null)
        {
            details.Add(weeklyDetails.Replace("重置时间：", "周重置时间：", StringComparison.Ordinal));
        }

        return details.Count == 0 ? null : string.Join(Environment.NewLine, details);
    }
}
