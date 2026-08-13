namespace CodexUsageWidget.Services;

public static class WaterBallAnimationPolicy
{
    private static readonly TimeSpan NormalInterval = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan ActiveInterval = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan UnavailableInterval = TimeSpan.FromMilliseconds(160);

    public static TimeSpan GetInterval(double? remainingPercent, double tokensPerMinute, bool isHovered)
    {
        if (!remainingPercent.HasValue || !double.IsFinite(remainingPercent.Value))
        {
            return UnavailableInterval;
        }

        if (isHovered || (double.IsFinite(tokensPerMinute) && tokensPerMinute >= 120_000))
        {
            return ActiveInterval;
        }

        return NormalInterval;
    }
}
