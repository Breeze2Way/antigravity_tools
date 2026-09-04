using System.Globalization;
using System.Text.Json;
using AntigravityUsageWidget.Models;

namespace AntigravityUsageWidget.Data;

public static class UsageJsonParser
{
    private static readonly string[] TokenProperties =
    [
        "input_tokens",
        "cached_input_tokens",
        "cache_write_input_tokens",
        "output_tokens",
        "reasoning_output_tokens",
        "total_tokens"
    ];

    public static bool TryParse(string json, string sourcePath, out UsageRecord? record)
    {
        record = null;

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (!root.TryGetProperty("timestamp", out var timestampElement) ||
                timestampElement.ValueKind != JsonValueKind.String ||
                !DateTimeOffset.TryParse(
                    timestampElement.GetString(),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var timestamp))
            {
                return false;
            }

            if (!root.TryGetProperty("payload", out var payload) ||
                !payload.TryGetProperty("info", out var info))
            {
                return false;
            }

            var isCumulative = false;
            JsonElement usageElement;
            if (info.TryGetProperty("total_token_usage", out var totalUsage))
            {
                usageElement = totalUsage;
                isCumulative = true;
            }
            else if (info.TryGetProperty("last_token_usage", out var lastUsage))
            {
                usageElement = lastUsage;
            }
            else
            {
                return false;
            }

            if (!TryReadUsage(usageElement, out var usage))
            {
                return false;
            }

            var kind = isCumulative ? "total" : "last";
            var identity = string.Join(
                '|',
                sourcePath,
                timestamp.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
                kind,
                string.Join(',', TokenProperties.Select(property => usageElement.GetProperty(property).GetInt64())));

            record = new UsageRecord(timestamp, usage, sourcePath, identity, isCumulative);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (OverflowException)
        {
            return false;
        }
    }

    public static bool TryParseRateLimit(string json, out LocalRateLimitSnapshot? snapshot)
    {
        snapshot = null;

        if (!TryParseRateLimits(json, out var snapshots))
        {
            return false;
        }

        snapshot = snapshots[0];
        return true;
    }

    public static bool TryParseRateLimits(
        string json,
        out IReadOnlyList<LocalRateLimitSnapshot> snapshots)
    {
        snapshots = [];

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (!root.TryGetProperty("timestamp", out var timestampElement) ||
                timestampElement.ValueKind != JsonValueKind.String ||
                !DateTimeOffset.TryParse(
                    timestampElement.GetString(),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var recordedAt))
            {
                return false;
            }

            JsonElement rateLimits;
            if (!root.TryGetProperty("rate_limits", out rateLimits) &&
                (!root.TryGetProperty("payload", out var payload) ||
                 !payload.TryGetProperty("rate_limits", out rateLimits)))
            {
                return false;
            }

            var parsed = new List<LocalRateLimitSnapshot>();
            foreach (var propertyName in new[] { "primary", "secondary" })
            {
                if (rateLimits.TryGetProperty(propertyName, out var limit) &&
                    TryParseRateLimit(limit, recordedAt, out var parsedLimit))
                {
                    parsed.Add(parsedLimit);
                }
            }

            if (parsed.Count == 0)
            {
                return false;
            }

            snapshots = parsed;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (OverflowException)
        {
            return false;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    private static bool TryParseRateLimit(
        JsonElement element,
        DateTimeOffset recordedAt,
        out LocalRateLimitSnapshot snapshot)
    {
        snapshot = default!;
        if (!TryReadDouble(element, "used_percent", out var usedPercent) ||
            usedPercent < 0d || usedPercent > 100d ||
            !TryReadLong(element, "window_minutes", out var windowMinutes) ||
            windowMinutes <= 0 ||
            !TryReadLong(element, "resets_at", out var resetSeconds))
        {
            return false;
        }

        snapshot = new LocalRateLimitSnapshot(
            recordedAt,
            usedPercent,
            TimeSpan.FromMinutes(windowMinutes),
            DateTimeOffset.FromUnixTimeSeconds(resetSeconds));
        return true;
    }

    private static bool TryReadDouble(JsonElement parent, string propertyName, out double value)
    {
        value = 0d;
        return parent.TryGetProperty(propertyName, out var element) &&
               element.ValueKind == JsonValueKind.Number &&
               element.TryGetDouble(out value) &&
               !double.IsNaN(value) &&
               !double.IsInfinity(value);
    }

    private static bool TryReadLong(JsonElement parent, string propertyName, out long value)
    {
        value = 0;
        return parent.TryGetProperty(propertyName, out var element) &&
               element.ValueKind == JsonValueKind.Number &&
               element.TryGetInt64(out value);
    }

    private static bool TryReadUsage(JsonElement element, out TokenUsage usage)
    {
        usage = default;
        var values = new long[TokenProperties.Length];
        for (var index = 0; index < TokenProperties.Length; index++)
        {
            var property = TokenProperties[index];
            if (!element.TryGetProperty(property, out var value) ||
                value.ValueKind != JsonValueKind.Number ||
                !value.TryGetInt64(out var number) ||
                number < 0)
            {
                return false;
            }

            values[index] = number;
        }

        usage = new TokenUsage(values[0], values[1], values[2], values[3], values[4], values[5]);
        return true;
    }
}
