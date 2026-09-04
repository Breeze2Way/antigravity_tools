using AntigravityUsageWidget.Models;

namespace AntigravityUsageWidget.Services;

public static class UsageCalculator
{
    public static UsageSnapshot Aggregate(
        IEnumerable<UsageRecord> records,
        DateTimeOffset now,
        TimeSpan window,
        long budgetTokens)
    {
        var uniqueRecords = records
            .GroupBy(record => record.Identity, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToArray();

        var normalized = Normalize(uniqueRecords);
        var start = now - window;
        var inWindow = normalized
            .Where(record => record.Timestamp >= start && record.Timestamp <= now)
            .ToArray();

        var usage = inWindow.Aggregate(TokenUsage.Zero, (total, record) => total + record.Usage);
        var hasData = inWindow.Length > 0;
        var usedPercent = CalculateUsedPercent(usage.TotalTokens, budgetTokens, hasData);

        return new UsageSnapshot(
            usage,
            inWindow.Length,
            hasData,
            usedPercent,
            100 - usedPercent);
    }

    public static IReadOnlyList<UsageRecord> Normalize(IEnumerable<UsageRecord> records)
    {
        var sourceRecords = records.ToArray();
        var normalized = sourceRecords
            .Where(record => !record.IsCumulative)
            .ToList();

        foreach (var group in sourceRecords
                     .Where(record => record.IsCumulative)
                     .GroupBy(record => record.SourcePath, StringComparer.Ordinal))
        {
            UsageRecord? previous = null;
            foreach (var current in group.OrderBy(record => record.Timestamp))
            {
                var delta = previous is null
                    ? TokenUsage.Zero
                    : SubtractOrReset(current.Usage, previous.Usage);

                normalized.Add(current with
                {
                    Usage = delta,
                    Identity = current.Identity + "|delta",
                    IsCumulative = false
                });
                previous = current;
            }
        }

        return normalized;
    }

    public static long SumTokensForLocalCalendarDate(
        IEnumerable<UsageRecord> records,
        DateTimeOffset now,
        int daysAgo)
    {
        if (daysAgo < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(daysAgo));
        }

        var targetDate = now.ToLocalTime().Date.AddDays(-daysAgo);
        return Normalize(records)
            .Where(record => record.Timestamp.ToLocalTime().Date == targetDate)
            .Sum(record => Math.Max(0, record.Usage.TotalTokens));
    }

    private static double CalculateUsedPercent(long totalTokens, long budgetTokens, bool hasData)
    {
        if (!hasData)
        {
            return 0;
        }

        if (budgetTokens <= 0)
        {
            return 100;
        }

        return Math.Clamp(totalTokens / (double)budgetTokens * 100, 0, 100);
    }

    private static TokenUsage SubtractOrReset(TokenUsage current, TokenUsage previous)
    {
        if (current.TotalTokens < previous.TotalTokens)
        {
            return current;
        }

        return current - previous;
    }
}
