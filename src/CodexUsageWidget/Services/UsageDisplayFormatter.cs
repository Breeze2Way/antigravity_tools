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
}
