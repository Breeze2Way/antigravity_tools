using System.Globalization;
using System.Text.Json;

namespace AntigravityUsageWidget.Data;

public static class AntigravityQuotaParser
{
    public static AntigravityQuotaSnapshot? Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            var planName = TryReadPlanName(root);
            var rows = new List<AntigravityQuotaRow>();

            if (root.TryGetProperty("response", out var response) &&
                response.ValueKind == JsonValueKind.Object)
            {
                ParseSummaryGroups(response, rows);
            }

            if (root.TryGetProperty("userStatus", out var userStatus) &&
                userStatus.ValueKind == JsonValueKind.Object)
            {
                planName ??= TryReadPlanName(userStatus);
                ParseModelConfigs(userStatus, rows);
            }

            ParseModelConfigs(root, rows);
            rows = rows
                .GroupBy(row => $"{row.Group}\u001f{row.Label}\u001f{row.Period}", StringComparer.Ordinal)
                .Select(group => group.First())
                .ToList();
            return rows.Count == 0
                ? null
                : new AntigravityQuotaSnapshot(planName, rows, DateTimeOffset.UtcNow);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static void ParseSummaryGroups(JsonElement response, List<AntigravityQuotaRow> rows)
    {
        if (!response.TryGetProperty("groups", out var groups) ||
            groups.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var group in groups.EnumerateArray())
        {
            var groupName = TryGetString(group, "displayName");
            if (!group.TryGetProperty("buckets", out var buckets) ||
                buckets.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var bucket in buckets.EnumerateArray())
            {
                var label = TryGetString(bucket, "displayName");
                if (string.IsNullOrWhiteSpace(label) ||
                    !TryReadRemainingPercent(bucket, out var remainingPercent))
                {
                    continue;
                }

                var window = TryGetString(bucket, "window") ?? label;
                rows.Add(new AntigravityQuotaRow(
                    label,
                    groupName,
                    remainingPercent,
                    TryReadDateTimeOffset(bucket, "resetTime"),
                    GetPeriod(window)));
            }
        }
    }

    private static void ParseModelConfigs(JsonElement parent, List<AntigravityQuotaRow> rows)
    {
        if (parent.TryGetProperty("cascadeModelConfigData", out var configData) &&
            configData.ValueKind == JsonValueKind.Object)
        {
            ParseModelConfigArray(configData, rows);
        }
        else
        {
            ParseModelConfigArray(parent, rows);
        }
    }

    private static void ParseModelConfigArray(JsonElement parent, List<AntigravityQuotaRow> rows)
    {
        if (!parent.TryGetProperty("clientModelConfigs", out var configs) ||
            configs.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var config in configs.EnumerateArray())
        {
            if (config.TryGetProperty("isInternal", out var isInternal) &&
                isInternal.ValueKind == JsonValueKind.True)
            {
                continue;
            }

            var label = TryGetString(config, "label");
            if (string.IsNullOrWhiteSpace(label) ||
                !config.TryGetProperty("quotaInfo", out var quotaInfo) ||
                !TryReadRemainingPercent(quotaInfo, out var remainingPercent))
            {
                continue;
            }

            rows.Add(new AntigravityQuotaRow(
                label,
                null,
                remainingPercent,
                TryReadDateTimeOffset(quotaInfo, "resetTime"),
                GetPeriod(label)));
        }
    }

    private static string? TryReadPlanName(JsonElement root)
    {
        if (!root.TryGetProperty("userStatus", out var userStatus))
        {
            userStatus = root;
        }

        if (!userStatus.TryGetProperty("planStatus", out var planStatus) ||
            !planStatus.TryGetProperty("planInfo", out var planInfo))
        {
            return null;
        }

        return TryGetString(planInfo, "planName");
    }

    private static bool TryReadRemainingPercent(JsonElement parent, out double remainingPercent)
    {
        remainingPercent = 0;
        if (!TryGetDouble(parent, "remainingFraction", out var fraction) ||
            !double.IsFinite(fraction) ||
            fraction < 0 ||
            fraction > 1)
        {
            return false;
        }

        remainingPercent = fraction * 100;
        return true;
    }

    private static DateTimeOffset? TryReadDateTimeOffset(JsonElement parent, string propertyName)
    {
        var value = TryGetString(parent, propertyName);
        return DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var timestamp)
            ? timestamp
            : null;
    }

    private static AntigravityQuotaPeriod GetPeriod(string value)
    {
        var normalized = value.Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();
        if (normalized.Contains("week", StringComparison.Ordinal) ||
            normalized.Contains("weekly", StringComparison.Ordinal))
        {
            return AntigravityQuotaPeriod.Weekly;
        }

        if (normalized.Contains("5h", StringComparison.Ordinal) ||
            normalized.Contains("fivehour", StringComparison.Ordinal) ||
            normalized.Contains("hour", StringComparison.Ordinal))
        {
            return AntigravityQuotaPeriod.Short;
        }

        return AntigravityQuotaPeriod.Short;
    }

    private static string? TryGetString(JsonElement parent, string propertyName)
    {
        return parent.TryGetProperty(propertyName, out var property) &&
            property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static bool TryGetDouble(JsonElement parent, string propertyName, out double value)
    {
        value = 0;
        if (!parent.TryGetProperty(propertyName, out var property))
        {
            return false;
        }

        if (property.ValueKind == JsonValueKind.Number)
        {
            return property.TryGetDouble(out value);
        }

        return property.ValueKind == JsonValueKind.String &&
            double.TryParse(property.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }
}
