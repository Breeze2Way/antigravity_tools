using System.Globalization;

namespace CodexUsageWidget.Services;

public static class ColorParser
{
    public const string DefaultWeeklyRingColorHex = "#58B7E8";
    public const string DefaultWeeklyRingGradientColorHex = "#8BDCF5";

    public static bool TryParseHex(string? text, out WaterBallColor color)
    {
        color = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var value = text.Trim();
        if (value.StartsWith('#'))
        {
            value = value[1..];
        }

        if (value.Length != 6 ||
            !byte.TryParse(value[0..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var red) ||
            !byte.TryParse(value[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var green) ||
            !byte.TryParse(value[4..6], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var blue))
        {
            return false;
        }

        color = new WaterBallColor(red, green, blue);
        return true;
    }

    public static string ToHex(WaterBallColor color)
    {
        return $"#{color.Red:X2}{color.Green:X2}{color.Blue:X2}";
    }
}
