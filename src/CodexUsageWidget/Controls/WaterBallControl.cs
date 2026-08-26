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
    private double? remainingPercent;
    private double? fiveHourRemainingPercent;
    private double? weeklyRemainingPercent;
    private string centerText = "--";
    private double tokensPerMinute;
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

        DrawProgressRing(
            drawingContext,
            center,
            radius - 1.5,
            weeklyRemainingPercent,
            thickness: 2.2,
            opacity: 0.88 + alertPulse * 0.08);
        DrawProgressRing(
            drawingContext,
            center,
            radius - 8.0,
            fiveHourRemainingPercent,
            thickness: 4.2,
            opacity: 0.94 + alertPulse * 0.12);

        DrawCenterText(drawingContext, center, radius);
    }

    private static void DrawProgressRing(
        DrawingContext drawingContext,
        WpfPoint center,
        double radius,
        double? remainingPercent,
        double thickness,
        double opacity)
    {
        if (radius <= 0)
        {
            return;
        }

        var trackBrush = new SolidColorBrush(MediaColor.FromArgb(72, 172, 190, 208));
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

        var activeColor = WaterBallDisplay.GetColor(remainingPercent);
        var activeBrush = new SolidColorBrush(ToMediaColor(activeColor, opacity));
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
