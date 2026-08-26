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

    public static WaterBallColor GetSoftComplementaryColor(double? remainingPercent)
    {
        var color = GetColor(remainingPercent);
        var (hue, saturation, lightness) = ToHsl(color);
        var complementaryHue = (hue + 180) % 360;
        var softenedSaturation = saturation * 0.85;
        var (red, green, blue) = FromHsl(complementaryHue, softenedSaturation, lightness);
        return new WaterBallColor(
            BlendWithWhite(red, 0.10),
            BlendWithWhite(green, 0.10),
            BlendWithWhite(blue, 0.10));
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

    private static (double Hue, double Saturation, double Lightness) ToHsl(WaterBallColor color)
    {
        var red = color.Red / 255d;
        var green = color.Green / 255d;
        var blue = color.Blue / 255d;
        var max = Math.Max(red, Math.Max(green, blue));
        var min = Math.Min(red, Math.Min(green, blue));
        var delta = max - min;
        var lightness = (max + min) / 2;
        if (delta <= double.Epsilon)
        {
            return (0, 0, lightness);
        }

        var saturation = delta / (1 - Math.Abs(2 * lightness - 1));
        var hue = max switch
        {
            _ when Math.Abs(max - red) <= double.Epsilon => 60 * (((green - blue) / delta) % 6),
            _ when Math.Abs(max - green) <= double.Epsilon => 60 * ((blue - red) / delta + 2),
            _ => 60 * ((red - green) / delta + 4)
        };

        return ((hue + 360) % 360, saturation, lightness);
    }

    private static (double Red, double Green, double Blue) FromHsl(
        double hue,
        double saturation,
        double lightness)
    {
        var chroma = (1 - Math.Abs(2 * lightness - 1)) * saturation;
        var second = chroma * (1 - Math.Abs((hue / 60 % 2) - 1));
        var match = lightness - chroma / 2;
        var rgb = hue switch
        {
            < 60 => (chroma, second, 0d),
            < 120 => (second, chroma, 0d),
            < 180 => (0d, chroma, second),
            < 240 => (0d, second, chroma),
            < 300 => (second, 0d, chroma),
            _ => (chroma, 0d, second)
        };

        return (rgb.Item1 + match, rgb.Item2 + match, rgb.Item3 + match);
    }

    private static byte BlendWithWhite(double channel, double amount)
    {
        var blended = channel * (1 - amount) + amount;
        return (byte)Math.Clamp(
            Math.Round(blended * 255, MidpointRounding.AwayFromZero),
            0,
            255);
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
