namespace CodexUsageWidget.Models;

public sealed record WidgetSettings(
    long WeeklyBudgetTokens = 0,
    int RefreshSeconds = 120,
    double Opacity = 0.92,
    bool Topmost = true,
    bool AutoStart = false,
    double Left = double.NaN,
    double Top = double.NaN)
{
    public bool WeeklyBudgetConfigured { get; init; }

    public string WeeklyRingColor { get; init; } = "#A6FFA6";

    public string WeeklyRingGradientColor { get; init; } = "#004080";

    public string WeeklyRingTrackColor { get; init; } = "#0B2942";

    public bool WeeklyRingGradientEnabled { get; init; } = true;

    public string Language { get; init; } = "zh-CN";
}
