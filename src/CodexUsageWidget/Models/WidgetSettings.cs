namespace CodexUsageWidget.Models;

public sealed record WidgetSettings(
    long WeeklyBudgetTokens = 0,
    int RefreshSeconds = 30,
    double Opacity = 0.92,
    bool Topmost = true,
    bool AutoStart = false,
    double Left = double.NaN,
    double Top = double.NaN)
{
    public bool WeeklyBudgetConfigured { get; init; }
}
