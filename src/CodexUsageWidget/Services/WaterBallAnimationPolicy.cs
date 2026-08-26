namespace CodexUsageWidget.Services;

public static class WaterBallAnimationPolicy
{
    private static readonly TimeSpan NormalInterval = TimeSpan.FromMilliseconds(240);
    private static readonly TimeSpan ActiveInterval = TimeSpan.FromMilliseconds(160);
    private static readonly TimeSpan UnavailableInterval = TimeSpan.FromMilliseconds(360);

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
