using AntigravityUsageWidget.Data;

namespace AntigravityUsageWidget.Services;

public sealed record AntigravityDisplayQuota(
    string? PlanName,
    double? ShortRemainingPercent,
    DateTimeOffset? ShortResetAt,
    double? WeeklyRemainingPercent,
    DateTimeOffset? WeeklyResetAt,
    IReadOnlyList<AntigravityQuotaRow> Rows);

public static class AntigravityQuotaAggregator
{
    public static AntigravityDisplayQuota Aggregate(AntigravityQuotaSnapshot snapshot)
    {
        var rowsForSelectedModel = FindRowsForSelectedModel(snapshot);
        var shortQuota = FindLowest(rowsForSelectedModel, AntigravityQuotaPeriod.Short);
        var weeklyQuota = FindLowest(rowsForSelectedModel, AntigravityQuotaPeriod.Weekly);
        return new AntigravityDisplayQuota(
            snapshot.PlanName,
            shortQuota?.RemainingPercent,
            shortQuota?.ResetAt,
            weeklyQuota?.RemainingPercent,
            weeklyQuota?.ResetAt,
            snapshot.Rows);
    }

    private static IReadOnlyList<AntigravityQuotaRow> FindRowsForSelectedModel(
        AntigravityQuotaSnapshot snapshot)
    {
        if (!string.IsNullOrWhiteSpace(snapshot.SelectedModelId))
        {
            var exactRows = snapshot.Rows
                .Where(row => string.Equals(row.ModelId, snapshot.SelectedModelId, StringComparison.Ordinal))
                .ToArray();
            if (exactRows.Length > 0)
            {
                return exactRows;
            }
        }

        var selectedGroup = GetSelectedGroup(snapshot.SelectedModelLabel);
        if (selectedGroup is not null)
        {
            var groupRows = snapshot.Rows
                .Where(row => string.Equals(row.Group, selectedGroup, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (groupRows.Length > 0)
            {
                return groupRows;
            }
        }

        return snapshot.Rows;
    }

    private static string? GetSelectedGroup(string? selectedModelLabel)
    {
        if (string.IsNullOrWhiteSpace(selectedModelLabel))
        {
            return null;
        }

        if (selectedModelLabel.Contains("Gemini", StringComparison.OrdinalIgnoreCase))
        {
            return "Gemini Models";
        }

        if (selectedModelLabel.Contains("Claude", StringComparison.OrdinalIgnoreCase) ||
            selectedModelLabel.Contains("GPT", StringComparison.OrdinalIgnoreCase))
        {
            return "Claude and GPT models";
        }

        return null;
    }

    private static AntigravityQuotaRow? FindLowest(
        IReadOnlyList<AntigravityQuotaRow> rows,
        AntigravityQuotaPeriod period)
    {
        return rows
            .Where(row => row.Period == period)
            .OrderBy(row => row.RemainingPercent)
            .FirstOrDefault();
    }
}
