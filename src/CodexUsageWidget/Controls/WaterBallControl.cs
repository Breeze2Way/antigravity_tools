using System.Globalization;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Media;
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
    private string centerText = "--";
    private double tokensPerMinute;
    private double wavePhase;
    private long lastRenderTimestamp;
    private bool animationAttached;

    public WaterBallControl()
    {
        Loaded += Control_Loaded;
        Unloaded += Control_Unloaded;
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
        var bucketGeometry = new EllipseGeometry(center, radius, radius);
        var color = WaterBallDisplay.GetColor(remainingPercent);
        var waterBrush = new SolidColorBrush(ToMediaColor(color));
        var waterHighlight = new SolidColorBrush(Lighten(color, 0.35));
        var bucketBrush = new SolidColorBrush(ToMediaColor(WaterBallDisplay.GetBackgroundColor(remainingPercent)));
        var rimBrush = new SolidColorBrush(Lighten(color, 0.55));

        drawingContext.DrawEllipse(bucketBrush, new MediaPen(rimBrush, 2), center, radius, radius);

        var fillRatio = WaterBallDisplay.GetFillRatio(remainingPercent);
        if (fillRatio.HasValue && radius > 0)
        {
            var diameter = radius * 2;
            var waterTop = center.Y + radius - diameter * fillRatio.Value;
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
                wavePhase,
                amplitudeScale: 1);
            drawingContext.DrawGeometry(waterBrush, null, wave);

            var highlightWave = CreateWaveGeometry(
                center,
                radius,
                waterTop,
                amplitude,
                frequency * 1.35,
                -wavePhase * 0.75 + 1.2,
                amplitudeScale: 0.32,
                fill: false);
            drawingContext.DrawGeometry(
                null,
                new MediaPen(waterHighlight, 1.1),
                highlightWave);
            drawingContext.Pop();
        }

        DrawCenterText(drawingContext, center, radius, rimBrush);
    }

    private static MediaColor ToMediaColor(WaterBallColor color)
    {
        return MediaColor.FromRgb(color.Red, color.Green, color.Blue);
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
        double radius,
        System.Windows.Media.Brush foreground)
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
        drawingContext.DrawEllipse(null, new MediaPen(foreground, 0.8), center, radius - 4, radius - 4);
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
            context.LineTo(
                new WpfPoint(left + (right - left) * normalizedX, waterTop + offset),
                true,
                false);
        }

        if (fill)
        {
            context.LineTo(new WpfPoint(right, bottom), true, false);
            context.LineTo(new WpfPoint(left, bottom), true, false);
        }

        return geometry;
    }

    private static double WaveOffset(
        double normalizedX,
        double amplitude,
        double frequency,
        double phase)
    {
        var primary = Math.Sin(normalizedX * Math.PI * 2 * frequency + phase);
        var secondary = Math.Sin(normalizedX * Math.PI * 2 * frequency * 0.47 - phase * 0.63 + 1.1);
        return primary * amplitude + secondary * amplitude * 0.28;
    }

    private void Control_Loaded(object sender, RoutedEventArgs e)
    {
        if (animationAttached)
        {
            return;
        }

        animationAttached = true;
        lastRenderTimestamp = Stopwatch.GetTimestamp();
        CompositionTarget.Rendering += CompositionTarget_Rendering;
    }

    private void Control_Unloaded(object sender, RoutedEventArgs e)
    {
        if (!animationAttached)
        {
            return;
        }

        animationAttached = false;
        CompositionTarget.Rendering -= CompositionTarget_Rendering;
    }

    private void CompositionTarget_Rendering(object? sender, EventArgs e)
    {
        var timestamp = Stopwatch.GetTimestamp();
        var elapsed = (timestamp - lastRenderTimestamp) / (double)Stopwatch.Frequency;
        lastRenderTimestamp = timestamp;
        if (!double.IsFinite(elapsed) || elapsed <= 0)
        {
            return;
        }

        wavePhase += WaterWaveDisplay.GetSpeed(tokensPerMinute) * Math.Min(elapsed, 0.1);
        InvalidateVisual();
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
