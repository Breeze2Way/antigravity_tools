using System.Globalization;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Media;
using System.Windows.Threading;
using CodexUsageWidget.Services;
using MediaBrush = System.Windows.Media.Brush;
using MediaBrushes = System.Windows.Media.Brushes;
using MediaColor = System.Windows.Media.Color;
using MediaPen = System.Windows.Media.Pen;
using WpfFlowDirection = System.Windows.FlowDirection;
using WpfPoint = System.Windows.Point;

namespace CodexUsageWidget.Controls;

public sealed class WaterBallControl : FrameworkElement
{
    private static readonly (double X, double Y, double Size)[] BubbleLayout =
    [
        (-0.28, 0.18, 1.2),
        (0.24, 0.28, 1.8),
        (-0.10, 0.42, 1.0),
        (0.35, 0.55, 1.3),
        (-0.34, 0.64, 1.5)
    ];

    private double? remainingPercent;
    private double? fiveHourRemainingPercent;
    private double? weeklyRemainingPercent;
    private string centerText = "--";
    private double tokensPerMinute;
    private WaterBallColor weeklyRingStartColor = ColorParser.DefaultWeeklyRingStartColor;
    private WaterBallColor weeklyRingEndColor = ColorParser.DefaultWeeklyRingEndColor;
    private WaterBallColor weeklyRingTrackColor = ColorParser.DefaultWeeklyRingTrackColor;
    private bool weeklyRingGradientEnabled = true;
    private double animationTime;
    private long lastRenderTimestamp;
    private readonly DispatcherTimer animationTimer;
    private bool animationAttached;
    private bool isHovered;

    public WaterBallControl()
    {
        Loaded += Control_Loaded;
        Unloaded += Control_Unloaded;
        animationTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = WaterBallAnimationPolicy.GetInterval(null, 0, false)
        };
        animationTimer.Tick += AnimationTimer_Tick;
        IsVisibleChanged += (_, _) =>
        {
            if (!animationAttached)
            {
                return;
            }

            if (IsVisible)
            {
                lastRenderTimestamp = Stopwatch.GetTimestamp();
                animationTimer.Start();
            }
            else
            {
                animationTimer.Stop();
            }
        };
        MouseEnter += (_, _) =>
        {
            isHovered = true;
            UpdateAnimationInterval();
            InvalidateVisual();
        };
        MouseLeave += (_, _) =>
        {
            isHovered = false;
            UpdateAnimationInterval();
            InvalidateVisual();
        };
    }

    public double? RemainingPercent
    {
        get => remainingPercent;
        set
        {
            if (Nullable.Equals(remainingPercent, value))
            {
                return;
            }

            remainingPercent = value;
            UpdateAnimationInterval();
            InvalidateVisual();
        }
    }

    public string CenterText
    {
        get => centerText;
        set
        {
            var next = string.IsNullOrWhiteSpace(value) ? "--" : value;
            if (string.Equals(centerText, next, StringComparison.Ordinal))
            {
                return;
            }

            centerText = next;
            InvalidateVisual();
        }
    }

    public double TokensPerMinute
    {
        get => tokensPerMinute;
        set
        {
            var next = double.IsFinite(value) ? Math.Max(0, value) : 0;
            if (Math.Abs(tokensPerMinute - next) < 0.01)
            {
                return;
            }

            tokensPerMinute = next;
            UpdateAnimationInterval();
            InvalidateVisual();
        }
    }

    public double? FiveHourRemainingPercent
    {
        get => fiveHourRemainingPercent;
        set
        {
            if (Nullable.Equals(fiveHourRemainingPercent, value))
            {
                return;
            }

            fiveHourRemainingPercent = value;
            InvalidateVisual();
        }
    }

    public double? WeeklyRemainingPercent
    {
        get => weeklyRemainingPercent;
        set
        {
            if (Nullable.Equals(weeklyRemainingPercent, value))
            {
                return;
            }

            weeklyRemainingPercent = value;
            InvalidateVisual();
        }
    }

    public WaterBallColor WeeklyRingStartColor
    {
        get => weeklyRingStartColor;
        set
        {
            if (weeklyRingStartColor == value)
            {
                return;
            }

            weeklyRingStartColor = value;
            InvalidateVisual();
        }
    }

    public WaterBallColor WeeklyRingEndColor
    {
        get => weeklyRingEndColor;
        set
        {
            if (weeklyRingEndColor == value)
            {
                return;
            }

            weeklyRingEndColor = value;
            InvalidateVisual();
        }
    }

    public bool WeeklyRingGradientEnabled
    {
        get => weeklyRingGradientEnabled;
        set
        {
            if (weeklyRingGradientEnabled == value)
            {
                return;
            }

            weeklyRingGradientEnabled = value;
            InvalidateVisual();
        }
    }

    public WaterBallColor WeeklyRingTrackColor
    {
        get => weeklyRingTrackColor;
        set
        {
            if (weeklyRingTrackColor == value)
            {
                return;
            }

            weeklyRingTrackColor = value;
            InvalidateVisual();
        }
    }

    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new WaterBallAutomationPeer(this);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        var size = Math.Min(ActualWidth, ActualHeight);
        if (size <= 0)
        {
            return;
        }

        var center = new WpfPoint(ActualWidth / 2, ActualHeight / 2);
        var radius = Math.Max(0, size / 2 - 3);
        var color = WaterBallDisplay.GetColor(remainingPercent);
        var glowOpacity = WaterBallEffects.GetGlowOpacity(remainingPercent, tokensPerMinute, isHovered);
        var alertPulse = WaterBallEffects.GetAlertPulse(remainingPercent, animationTime * 0.35);
        var shellOpacity = WaterBallEffects.GetShellOpacity(remainingPercent, isHovered);
        var rimBrush = new SolidColorBrush(Lighten(color, 0.55));
        var shellBrush = new SolidColorBrush(
            ToMediaColor(WaterBallDisplay.GetBackgroundColor(remainingPercent), shellOpacity));
        var glowBrush = new SolidColorBrush(ToMediaColor(color, glowOpacity + alertPulse * 0.08));

        drawingContext.DrawEllipse(glowBrush, null, center, radius + 2.5, radius + 2.5);
        drawingContext.DrawEllipse(
            shellBrush,
            new MediaPen(
                rimBrush,
                WaterBallEffects.GetAlertRingThickness(remainingPercent, animationTime * 0.35)),
            center,
            radius,
            radius);

        var reflectionBrush = new SolidColorBrush(
            MediaColor.FromArgb((byte)(isHovered ? 54 : 34), 255, 255, 255));
        drawingContext.DrawEllipse(
            reflectionBrush,
            null,
            new WpfPoint(center.X - radius * 0.32, center.Y - radius * 0.34),
            radius * 0.17,
            radius * 0.08);

        DrawInnerWater(
            drawingContext,
            center,
            WaterBallDisplay.GetInnerWaterRadius(radius),
            fiveHourRemainingPercent);

        DrawProgressRing(
            drawingContext,
            center,
            radius - 1.5,
            weeklyRemainingPercent,
            thickness: WaterBallDisplay.WeeklyRingThickness,
            opacity: WaterBallDisplay.WeeklyRingOpacity,
            weeklyRingStartColor,
            weeklyRingEndColor,
            weeklyRingTrackColor,
            weeklyRingGradientEnabled);

        DrawCenterText(drawingContext, center, radius);
    }

    private void DrawInnerWater(
        DrawingContext drawingContext,
        WpfPoint center,
        double radius,
        double? remainingPercent)
    {
        if (radius <= 0)
        {
            return;
        }

        var innerDiskBrush = new SolidColorBrush(MediaColor.FromArgb(46, 7, 16, 30));
        drawingContext.DrawEllipse(null, new MediaPen(innerDiskBrush, 1), center, radius, radius);
        if (!WaterBallDisplay.HasInnerWater(remainingPercent))
        {
            return;
        }

        var bucketGeometry = new EllipseGeometry(center, radius, radius);
        var waterColor = WaterBallDisplay.GetColor(remainingPercent);
        var waterBrush = new SolidColorBrush(ToMediaColor(waterColor, 0.80));
        var waterHighlight = new SolidColorBrush(Lighten(waterColor, 0.35));
        var fillRatio = WaterBallDisplay.GetFillRatio(remainingPercent)!.Value;
        var diameter = radius * 2;
        var waterTop = center.Y + radius - diameter * fillRatio;
        var waterRect = new Rect(center.X - radius, waterTop, diameter, diameter);

        drawingContext.PushClip(bucketGeometry);
        drawingContext.DrawRectangle(waterBrush, null, waterRect);

        var amplitude = WaterWaveDisplay.GetAmplitude(tokensPerMinute, radius);
        var frequency = WaterWaveDisplay.GetFrequency(tokensPerMinute);
        var wave = CreateWaveGeometry(
            center,
            radius,
            waterTop,
            amplitude,
            frequency,
            animationTime * WaterWaveDisplay.GetSpeed(tokensPerMinute),
            amplitudeScale: 1);
        drawingContext.DrawGeometry(waterBrush, null, wave);

        var highlightWave = CreateWaveGeometry(
            center,
            radius,
            waterTop,
            amplitude,
            frequency * 1.35,
            -animationTime * WaterWaveDisplay.GetSpeed(tokensPerMinute) * 0.75 + 1.2,
            amplitudeScale: 0.32,
            fill: false);
        drawingContext.DrawGeometry(null, new MediaPen(waterHighlight, 1.1), highlightWave);

        var waterlineHighlight = new SolidColorBrush(MediaColor.FromArgb(72, 255, 255, 255));
        var softHighlightWave = CreateWaveGeometry(
            center,
            radius,
            waterTop - 0.6,
            amplitude,
            frequency * 0.82,
            animationTime * WaterWaveDisplay.GetSpeed(tokensPerMinute) * 0.55 + 0.7,
            amplitudeScale: 0.52,
            fill: false);
        drawingContext.DrawGeometry(null, new MediaPen(waterlineHighlight, 0.75), softHighlightWave);

        var usageFactor = Math.Clamp(tokensPerMinute / 220_000d, 0, 1);
        for (var index = 0; index < BubbleLayout.Length; index++)
        {
            var bubble = BubbleLayout[index];
            var visibility = WaterBallEffects.GetBubbleVisibility(
                remainingPercent,
                tokensPerMinute,
                index,
                animationTime);
            if (visibility <= 0)
            {
                continue;
            }

            var rise = (animationTime * (0.08 + usageFactor * 0.16) + index * 0.23) % 0.72;
            var bubbleCenter = new WpfPoint(
                center.X + bubble.X * radius,
                center.Y + radius - (0.25 + ((bubble.Y + rise) % 0.72)) * radius * 1.5);
            var bubbleBrush = new SolidColorBrush(
                MediaColor.FromArgb(
                    (byte)Math.Round(Math.Clamp(visibility * 0.55, 0, 1) * 255),
                    255,
                    255,
                    255));
            drawingContext.DrawEllipse(null, new MediaPen(bubbleBrush, 0.75), bubbleCenter, bubble.Size, bubble.Size);
        }

        drawingContext.Pop();
    }

    private static void DrawProgressRing(
        DrawingContext drawingContext,
        WpfPoint center,
        double radius,
        double? remainingPercent,
        double thickness,
        double opacity,
        WaterBallColor startColor,
        WaterBallColor endColor,
        WaterBallColor trackColor,
        bool gradientEnabled)
    {
        if (radius <= 0)
        {
            return;
        }

        var trackBrush = new SolidColorBrush(
            MediaColor.FromArgb(
                WaterBallDisplay.WeeklyRingTrackAlpha,
                trackColor.Red,
                trackColor.Green,
                trackColor.Blue));
        drawingContext.DrawEllipse(
            null,
            new MediaPen(trackBrush, thickness),
            center,
            radius,
            radius);

        var sweepAngle = WaterBallDisplay.GetRingSweepAngle(remainingPercent);
        if (!sweepAngle.HasValue || sweepAngle.Value <= 0)
        {
            return;
        }

        var activeBrush = CreateWeeklyRingBrush(startColor, endColor, gradientEnabled, opacity);
        var activePen = new MediaPen(activeBrush, thickness)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round
        };
        if (sweepAngle.Value >= 359.5)
        {
            drawingContext.DrawEllipse(null, activePen, center, radius, radius);
            return;
        }

        drawingContext.DrawGeometry(
            null,
            activePen,
            CreateArcGeometry(center, radius, sweepAngle.Value));
    }

    private static MediaBrush CreateWeeklyRingBrush(
        WaterBallColor startColor,
        WaterBallColor endColor,
        bool gradientEnabled,
        double opacity)
    {
        if (!gradientEnabled)
        {
            return new SolidColorBrush(ToMediaColor(startColor, opacity));
        }

        var brush = new LinearGradientBrush
        {
            MappingMode = BrushMappingMode.RelativeToBoundingBox,
            StartPoint = new WpfPoint(0, 0),
            EndPoint = new WpfPoint(1, 1),
            Opacity = Math.Clamp(opacity, 0, 1)
        };
        brush.GradientStops.Add(new GradientStop(ToMediaColor(startColor), 0));
        brush.GradientStops.Add(new GradientStop(ToMediaColor(endColor), 1));
        return brush;
    }

    private static StreamGeometry CreateArcGeometry(
        WpfPoint center,
        double radius,
        double sweepAngle)
    {
        var start = PointOnCircle(center, radius, -90);
        var end = PointOnCircle(center, radius, -90 + sweepAngle);
        var geometry = new StreamGeometry();
        using var context = geometry.Open();
        context.BeginFigure(start, isFilled: false, isClosed: false);
        context.ArcTo(
            end,
            new System.Windows.Size(radius, radius),
            rotationAngle: 0,
            isLargeArc: sweepAngle > 180,
            SweepDirection.Clockwise,
            isStroked: true,
            isSmoothJoin: true);
        return geometry;
    }

    private static WpfPoint PointOnCircle(WpfPoint center, double radius, double angle)
    {
        var radians = angle * Math.PI / 180;
        return new WpfPoint(
            center.X + Math.Cos(radians) * radius,
            center.Y + Math.Sin(radians) * radius);
    }

    private static StreamGeometry CreateWaveGeometry(
        WpfPoint center,
        double radius,
        double waterTop,
        double amplitude,
        double frequency,
        double phase,
        double amplitudeScale,
        bool fill = true)
    {
        var left = center.X - radius;
        var right = center.X + radius;
        var bottom = center.Y + radius;
        var segments = 24;
        var geometry = new StreamGeometry();
        using var context = geometry.Open();
        var offset = WaveOffset(0, amplitude, frequency, phase) * amplitudeScale;
        context.BeginFigure(new WpfPoint(left, waterTop + offset), fill, fill);
        for (var index = 1; index <= segments; index++)
        {
            var normalizedX = index / (double)segments;
            offset = WaveOffset(normalizedX, amplitude, frequency, phase) * amplitudeScale;
            context.LineTo(new WpfPoint(left + (right - left) * normalizedX, waterTop + offset), true, false);
        }

        if (fill)
        {
            context.LineTo(new WpfPoint(right, bottom), true, false);
            context.LineTo(new WpfPoint(left, bottom), true, false);
        }

        return geometry;
    }

    private static double WaveOffset(double normalizedX, double amplitude, double frequency, double phase)
    {
        var primary = Math.Sin(normalizedX * Math.PI * 2 * frequency + phase);
        var secondary = Math.Sin(normalizedX * Math.PI * 2 * frequency * 0.47 - phase * 0.63 + 1.1);
        return primary * amplitude + secondary * amplitude * 0.28;
    }

    private static MediaColor ToMediaColor(WaterBallColor color)
    {
        return MediaColor.FromRgb(color.Red, color.Green, color.Blue);
    }

    private static MediaColor ToMediaColor(WaterBallColor color, double opacity)
    {
        return MediaColor.FromArgb(
            (byte)Math.Round(Math.Clamp(opacity, 0, 1) * 255, MidpointRounding.AwayFromZero),
            color.Red,
            color.Green,
            color.Blue);
    }

    private static MediaColor Lighten(WaterBallColor color, double amount)
    {
        return MediaColor.FromRgb(
            LightenChannel(color.Red, amount),
            LightenChannel(color.Green, amount),
            LightenChannel(color.Blue, amount));
    }

    private static byte LightenChannel(byte channel, double amount)
    {
        return (byte)Math.Round(channel + (255 - channel) * amount, MidpointRounding.AwayFromZero);
    }

    private void DrawCenterText(
        DrawingContext drawingContext,
        System.Windows.Point center,
        double radius)
    {
        var fontSize = Math.Max(14, radius * 0.42);
        var formattedText = new FormattedText(
            centerText,
            CultureInfo.CurrentUICulture,
            WpfFlowDirection.LeftToRight,
            new Typeface("Segoe UI"),
            fontSize,
            MediaBrushes.White,
            VisualTreeHelper.GetDpi(this).PixelsPerDip)
        {
            MaxTextHeight = radius * 2
        };

        var origin = new WpfPoint(
            WaterBallDisplay.GetCenteredTextOrigin(center.X, formattedText.Width),
            center.Y - formattedText.Height / 2);
        drawingContext.DrawText(formattedText, origin);
    }

    private void Control_Loaded(object sender, RoutedEventArgs e)
    {
        if (animationAttached)
        {
            return;
        }

        animationAttached = true;
        UpdateAnimationInterval();
        lastRenderTimestamp = Stopwatch.GetTimestamp();
        if (IsVisible)
        {
            animationTimer.Start();
        }
    }

    private void Control_Unloaded(object sender, RoutedEventArgs e)
    {
        if (!animationAttached)
        {
            return;
        }

        animationAttached = false;
        animationTimer.Stop();
    }

    private void AnimationTimer_Tick(object? sender, EventArgs e)
    {
        if (!IsVisible || Visibility != Visibility.Visible)
        {
            return;
        }

        var timestamp = Stopwatch.GetTimestamp();
        var elapsed = (timestamp - lastRenderTimestamp) / (double)Stopwatch.Frequency;
        lastRenderTimestamp = timestamp;
        if (!double.IsFinite(elapsed) || elapsed <= 0)
        {
            return;
        }

        var frameSeconds = Math.Min(elapsed, 0.1);
        animationTime += frameSeconds;
        InvalidateVisual();
    }

    private void UpdateAnimationInterval()
    {
        animationTimer.Interval = WaterBallAnimationPolicy.GetInterval(
            remainingPercent,
            tokensPerMinute,
            isHovered);
    }

    private sealed class WaterBallAutomationPeer : FrameworkElementAutomationPeer
    {
        public WaterBallAutomationPeer(WaterBallControl owner)
            : base(owner)
        {
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Custom;
        }

        protected override string GetNameCore()
        {
            return base.GetNameCore() is { Length: > 0 } name
                ? name
                : "Codex 剩余用量";
        }
    }
}
