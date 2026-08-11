using System.Globalization;
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
        var bucketBrush = new SolidColorBrush(MediaColor.FromRgb(18, 32, 49));
        var rimBrush = new SolidColorBrush(Lighten(color, 0.55));

        drawingContext.DrawEllipse(bucketBrush, new MediaPen(rimBrush, 2), center, radius, radius);

        var fillRatio = WaterBallDisplay.GetFillRatio(remainingPercent);
        if (fillRatio.HasValue && radius > 0)
        {
            var diameter = radius * 2;
            var waterTop = center.Y + radius - diameter * fillRatio.Value;
            var waterRect = new Rect(center.X - radius, waterTop, diameter, diameter - Math.Max(0, waterTop - (center.Y + radius)));

            drawingContext.PushClip(bucketGeometry);
            drawingContext.DrawRectangle(waterBrush, null, waterRect);

            var wave = new StreamGeometry();
            using (var context = wave.Open())
            {
                var waveHeight = Math.Min(2.5, radius / 8);
                context.BeginFigure(new WpfPoint(center.X - radius, waterTop), true, true);
                context.LineTo(new WpfPoint(center.X - radius + radius * 0.55, waterTop - waveHeight), true, false);
                context.LineTo(new WpfPoint(center.X - radius + radius * 1.1, waterTop + waveHeight), true, false);
                context.LineTo(new WpfPoint(center.X + radius, waterTop - waveHeight * 0.35), true, false);
                context.LineTo(new WpfPoint(center.X + radius, center.Y + radius), true, false);
                context.LineTo(new WpfPoint(center.X - radius, center.Y + radius), true, false);
            }

            drawingContext.DrawGeometry(waterBrush, null, wave);
            drawingContext.DrawLine(
                new MediaPen(waterHighlight, 1.1),
                new WpfPoint(center.X - radius, waterTop),
                new WpfPoint(center.X + radius, waterTop));
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
