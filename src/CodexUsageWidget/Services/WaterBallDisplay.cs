using System.Globalization;

namespace CodexUsageWidget.Services;

public readonly record struct WaterBallColor(byte Red, byte Green, byte Blue);

public static class WaterBallDisplay
{
    public const double WeeklyRingThickness = 4.4;
    public const double WeeklyRingOpacity = 1.0;
    public const byte WeeklyRingTrackAlpha = 120;

    private static readonly WaterBallColor NeutralColor = new(128, 140, 153);
    private static readonly WaterBallColor RedColor = new(239, 68, 68);
    private static readonly WaterBallColor YellowColor = new(250, 204, 21);
    private static readonly WaterBallColor BlueColor = new(59, 130, 246);
    private static readonly WaterBallColor GreenColor = new(34, 197, 94);
    private static readonly WaterBallColor BucketBaseColor = new(18, 32, 49);

    public static double? GetFillRatio(double? remainingPercent)
    {
        if (!HasFinitePercent(remainingPercent))
        {
            return null;
        }

        return Math.Clamp(remainingPercent!.Value, 0, 100) / 100d;
    }

    public static double? GetRingSweepAngle(double? remainingPercent)
    {
        if (!HasFinitePercent(remainingPercent))
        {
            return null;
        }

        return Math.Clamp(remainingPercent!.Value, 0, 100) * 3.6;
    }

    public static bool HasInnerWater(double? remainingPercent)
    {
        return HasFinitePercent(remainingPercent);
    }

    public static double GetInnerWaterRadius(double outerRadius)
    {
        return Math.Max(0, outerRadius - 4);
    }

    public static string FormatCenterText(double? remainingPercent)
    {
        if (!HasFinitePercent(remainingPercent))
        {
            return "--";
        }

        var clamped = Math.Clamp(remainingPercent!.Value, 0, 100);
        return $"{clamped.ToString("0.#", CultureInfo.InvariantCulture)}%";
    }

    public static bool IsLowRemaining(double? remainingPercent)
    {
        return HasFinitePercent(remainingPercent) &&
            remainingPercent!.Value >= 0 &&
            remainingPercent.Value <= 20;
    }

    public static WaterBallColor GetColor(double? remainingPercent)
    {
        if (!HasFinitePercent(remainingPercent))
        {
            return NeutralColor;
        }

        var percent = Math.Clamp(remainingPercent!.Value, 0, 100);
        return percent switch
        {
            <= 20 => Interpolate(RedColor, YellowColor, percent / 20),
            <= 60 => Interpolate(YellowColor, BlueColor, (percent - 20) / 40),
            _ => Interpolate(BlueColor, GreenColor, (percent - 60) / 40)
        };
    }

    public static WaterBallColor GetInvertedColor(double? remainingPercent)
    {
        var color = GetColor(remainingPercent);
        return new WaterBallColor(
            (byte)(255 - color.Red),
            (byte)(255 - color.Green),
            (byte)(255 - color.Blue));
    }

    public static WaterBallColor GetBackgroundColor(double? remainingPercent)
    {
        return Interpolate(GetColor(remainingPercent), BucketBaseColor, 0.72);
    }

    public static double GetCenteredTextOrigin(double center, double textWidth)
    {
        return center - Math.Max(0, textWidth) / 2;
    }

    private static WaterBallColor Interpolate(WaterBallColor start, WaterBallColor end, double amount)
    {
        return new WaterBallColor(
            Blend(start.Red, end.Red, amount),
            Blend(start.Green, end.Green, amount),
            Blend(start.Blue, end.Blue, amount));
    }

    private static byte Blend(byte start, byte end, double amount)
    {
        return (byte)Math.Round(start + (end - start) * amount, MidpointRounding.AwayFromZero);
    }

    private static bool HasFinitePercent(double? remainingPercent)
    {
        return remainingPercent.HasValue &&
            double.IsFinite(remainingPercent.Value);
    }
}
