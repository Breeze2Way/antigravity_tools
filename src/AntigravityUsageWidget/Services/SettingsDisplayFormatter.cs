namespace AntigravityUsageWidget.Services;

public static class SettingsDisplayFormatter
{
    public static string FormatOpacityPercent(double percentage)
    {
        if (!double.IsFinite(percentage))
        {
            return "--";
        }

        var rounded = Math.Clamp(
            Math.Round(percentage, MidpointRounding.AwayFromZero),
            0,
            100);
        return $"{rounded:0}%";
    }
}
