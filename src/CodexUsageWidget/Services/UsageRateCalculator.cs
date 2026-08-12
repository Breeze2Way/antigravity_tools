using CodexUsageWidget.Models;

namespace CodexUsageWidget.Services;

public static class UsageRateCalculator
{
    public static double CalculateTokensPerMinute(
        IEnumerable<UsageRecord> records,
        DateTimeOffset now,
        TimeSpan window)
    {
        if (window <= TimeSpan.Zero)
        {
            return 0;
        }

        var uniqueRecords = records
            .GroupBy(record => record.Identity, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToArray();
        var start = now - window;
        var tokens = UsageCalculator.Normalize(uniqueRecords)
            .Where(record => record.Timestamp >= start && record.Timestamp <= now)
            .Sum(record => (double)Math.Max(0, record.Usage.TotalTokens));

        return tokens / window.TotalMinutes;
    }
}
