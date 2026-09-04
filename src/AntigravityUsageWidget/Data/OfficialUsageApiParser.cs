using System.Text.Json;
using System.Globalization;

namespace AntigravityUsageWidget.Data;

public static class OfficialUsageApiParser
{
    public static OfficialUsageSnapshot? Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (!root.TryGetProperty("rate_limit", out var rateLimit) ||
                rateLimit.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            // The aggregate `allowed` flag can be false when only one window is
            // exhausted. Each window's used percentage is the authoritative
            // value for its own remaining quota.
            var primary = ParseWindow(rateLimit, "primary_window");
            var secondary = ParseWindow(rateLimit, "secondary_window");
            if (primary is null && secondary is null)
            {
                return null;
            }

            return new OfficialUsageSnapshot(
                secondary?.RemainingPercent,
                secondary?.ResetAfter,
                primary?.RemainingPercent,
                primary?.ResetAfter);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static UsageWindow? ParseWindow(JsonElement parent, string propertyName)
    {
        if (!parent.TryGetProperty(propertyName, out var window) ||
            window.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var usedPercent = TryGetDouble(window, "used_percent");
        if (!usedPercent.HasValue)
        {
            return null;
        }

        var remainingPercent = Math.Clamp(100 - usedPercent.Value, 0, 100);
        var resetAfter = TryGetDouble(window, "reset_after_seconds") is { } seconds
            ? TimeSpan.FromSeconds(Math.Max(0, seconds))
            : TryGetUnixResetAfter(window);
        return new UsageWindow(remainingPercent, resetAfter);
    }

    private static TimeSpan? TryGetUnixResetAfter(JsonElement window)
    {
        if (TryGetDouble(window, "reset_at") is not { } resetAt)
        {
            return null;
        }

        try
        {
            var resetTime = DateTimeOffset.FromUnixTimeSeconds((long)resetAt);
            return resetTime > DateTimeOffset.UtcNow
                ? resetTime - DateTimeOffset.UtcNow
                : TimeSpan.Zero;
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    private static double? TryGetDouble(JsonElement parent, string propertyName)
    {
        if (!parent.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number))
        {
            return double.IsFinite(number) ? number : null;
        }

        if (value.ValueKind == JsonValueKind.String &&
            double.TryParse(
                value.GetString(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out number))
        {
            return double.IsFinite(number) ? number : null;
        }

        return null;
    }

    private sealed record UsageWindow(double RemainingPercent, TimeSpan? ResetAfter);
}
