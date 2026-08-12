namespace CodexUsageWidget.Services;

public static class WaterBallEffects
{
    public static double GetGlowOpacity(double? remainingPercent, double tokensPerMinute, bool isHovered)
    {
        if (!remainingPercent.HasValue || !double.IsFinite(remainingPercent.Value))
        {
            return isHovered ? 0.18 : 0.10;
        }

        var usageBoost = Math.Clamp(tokensPerMinute / 250_000d, 0, 1) * 0.18;
        var hoverBoost = isHovered ? 0.12 : 0;
        var alertBoost = remainingPercent.Value <= 20 ? 0.08 : 0;
        return Math.Clamp(0.10 + usageBoost + hoverBoost + alertBoost, 0, 0.45);
    }

    public static double GetAlertPulse(double? remainingPercent, double phase)
    {
        if (!remainingPercent.HasValue || !double.IsFinite(remainingPercent.Value) || remainingPercent.Value > 20)
        {
            return 0;
        }

        return 0.5 + 0.5 * Math.Sin(phase * Math.PI * 2);
    }

    public static double GetAlertRingThickness(double? remainingPercent, double phase)
    {
        return 1.5 + GetAlertPulse(remainingPercent, phase) * 1.5;
    }

    public static double GetBubbleVisibility(double? remainingPercent, double tokensPerMinute, int bubbleIndex, double phase)
    {
        if (!remainingPercent.HasValue || !double.IsFinite(remainingPercent.Value) || bubbleIndex < 0)
        {
            return 0;
        }

        var usageFactor = Math.Clamp(tokensPerMinute / 220_000d, 0, 1);
        var bubblePhase = phase * (0.35 + usageFactor * 0.65) + bubbleIndex * 1.7;
        var motion = 0.5 + 0.5 * Math.Sin(bubblePhase);
        return Math.Clamp((0.12 + usageFactor * 0.70) * motion, 0, 1);
    }

    public static double GetShellOpacity(double? remainingPercent, bool isHovered)
    {
        var baseOpacity = remainingPercent.HasValue && double.IsFinite(remainingPercent.Value) ? 0.22 : 0.12;
        return Math.Clamp(baseOpacity + (isHovered ? 0.08 : 0), 0, 1);
    }
}
