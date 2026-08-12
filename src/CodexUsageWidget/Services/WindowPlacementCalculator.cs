using WpfPoint = System.Windows.Point;
using WpfRect = System.Windows.Rect;
using WpfSize = System.Windows.Size;

namespace CodexUsageWidget.Services;

public static class WindowPlacementCalculator
{
    public static WpfPoint CalculateSettingsPosition(
        WpfRect anchorBounds,
        WpfSize settingsSize,
        WpfRect workArea,
        double gap = 12)
    {
        var centeredTop = anchorBounds.Top + (anchorBounds.Height - settingsSize.Height) / 2;
        var centeredLeft = anchorBounds.Left + (anchorBounds.Width - settingsSize.Width) / 2;
        var candidates = new[]
        {
            new WpfPoint(anchorBounds.Right + gap, centeredTop),
            new WpfPoint(anchorBounds.Left - settingsSize.Width - gap, centeredTop),
            new WpfPoint(centeredLeft, anchorBounds.Bottom + gap),
            new WpfPoint(centeredLeft, anchorBounds.Top - settingsSize.Height - gap)
        };

        foreach (var candidate in candidates)
        {
            if (Fits(candidate, settingsSize, workArea))
            {
                return candidate;
            }
        }

        var rightSpace = workArea.Right - anchorBounds.Right;
        var leftSpace = anchorBounds.Left - workArea.Left;
        var fallback = rightSpace >= leftSpace ? candidates[0] : candidates[1];
        return Clamp(fallback, settingsSize, workArea);
    }

    private static bool Fits(WpfPoint position, WpfSize size, WpfRect workArea)
    {
        return position.X >= workArea.Left &&
            position.Y >= workArea.Top &&
            position.X + size.Width <= workArea.Right &&
            position.Y + size.Height <= workArea.Bottom;
    }

    private static WpfPoint Clamp(WpfPoint position, WpfSize size, WpfRect workArea)
    {
        var maxX = Math.Max(workArea.Left, workArea.Right - size.Width);
        var maxY = Math.Max(workArea.Top, workArea.Bottom - size.Height);
        return new WpfPoint(
            Math.Clamp(position.X, workArea.Left, maxX),
            Math.Clamp(position.Y, workArea.Top, maxY));
    }
}
