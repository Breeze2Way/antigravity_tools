using System.Globalization;

namespace CodexUsageWidget.Services;

public static class UsageDisplayFormatter
{
    public static string FormatMillions(long tokens)
    {
        var millions = Math.Max(0, tokens) / 1_000_000d;
        return $"{millions.ToString("0.0", CultureInfo.InvariantCulture)}M";
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
        var remainingHours = Math.Max(0, remaining.TotalHours);
        var localResetAt = resetAt.Value.ToOffset(now.Offset);
        return $"重置时间：{localResetAt:yyyy-MM-dd HH:mm} [剩余 {remainingHours.ToString("0.0", CultureInfo.InvariantCulture)} 小时]";
    }
}
