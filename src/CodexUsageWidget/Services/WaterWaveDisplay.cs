namespace CodexUsageWidget.Services;

public static class WaterWaveDisplay
{
    private const double FullIntensityTokensPerMinute = 250_000;

    public static double GetIntensity(double tokensPerMinute)
    {
        if (!double.IsFinite(tokensPerMinute) || tokensPerMinute <= 0)
        {
            return 0;
        }

        return Math.Clamp(tokensPerMinute / FullIntensityTokensPerMinute, 0, 1);
    }

    public static double GetAmplitude(double tokensPerMinute, double radius)
    {
        if (!double.IsFinite(radius) || radius <= 0)
        {
            return 0;
        }

        var intensity = GetIntensity(tokensPerMinute);
        return radius * (0.035 + intensity * 0.11);
    }

    public static double GetSpeed(double tokensPerMinute)
    {
        return 0.8 + GetIntensity(tokensPerMinute) * 2.4;
    }

    public static double GetFrequency(double tokensPerMinute)
    {
        return 1.5 + GetIntensity(tokensPerMinute) * 1.6;
    }
}
