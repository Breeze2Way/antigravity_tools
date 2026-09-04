using AntigravityUsageWidget.Data;

namespace AntigravityUsageWidget.Services;

public static class AntigravityTokenUsageAggregator
{
    public static AntigravityTokenUsageSummary Aggregate(
        IEnumerable<AntigravityTokenUsageRecord> records,
        DateTimeOffset now)
    {
        var today = now.ToLocalTime().Date;
        var yesterday = today.AddDays(-1);
        long todayTokens = 0;
        long yesterdayTokens = 0;

        foreach (var record in records)
        {
            var localDate = record.Timestamp.ToLocalTime().Date;
            if (localDate == today)
            {
                todayTokens += record.TotalTokens;
            }
            else if (localDate == yesterday)
            {
                yesterdayTokens += record.TotalTokens;
            }
        }

        return new AntigravityTokenUsageSummary(todayTokens, yesterdayTokens);
    }
}
