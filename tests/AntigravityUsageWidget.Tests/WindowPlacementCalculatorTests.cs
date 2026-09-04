using System.Windows;
using AntigravityUsageWidget.Services;

namespace AntigravityUsageWidget.Tests;

public sealed class WindowPlacementCalculatorTests
{
    private static readonly Rect WorkArea = new(0, 0, 1920, 1040);
    private static readonly Size SettingsSize = new(360, 245);

    [Fact]
    public void RestoresMinimizedWindowToNormalState()
    {
        Assert.Equal(WindowState.Normal, WindowRestorePolicy.GetRestoredState(WindowState.Minimized));
    }

    [Fact]
    public void PlacesSettingsToLeftWhenBallIsAtRightEdge()
    {
        var ball = new Rect(1852, 24, 68, 68);

        var position = WindowPlacementCalculator.CalculateSettingsPosition(
            ball,
            SettingsSize,
            WorkArea);

        Assert.Equal(1480, position.X);
        Assert.InRange(position.Y, WorkArea.Top, WorkArea.Bottom - SettingsSize.Height);
    }

    [Fact]
    public void PlacesSettingsToRightWhenBallIsAtLeftEdge()
    {
        var ball = new Rect(0, 450, 68, 68);

        var position = WindowPlacementCalculator.CalculateSettingsPosition(
            ball,
            SettingsSize,
            WorkArea);

        Assert.Equal(80, position.X);
        Assert.InRange(position.Y, WorkArea.Top, WorkArea.Bottom - SettingsSize.Height);
    }

    [Fact]
    public void ClampsSettingsInsideWorkAreaWhenNoSideFits()
    {
        var smallWorkArea = new Rect(0, 0, 300, 180);
        var ball = new Rect(120, 70, 68, 68);

        var position = WindowPlacementCalculator.CalculateSettingsPosition(
            ball,
            SettingsSize,
            smallWorkArea);

        Assert.Equal(smallWorkArea.Left, position.X);
        Assert.Equal(smallWorkArea.Top, position.Y);
    }

    [Fact]
    public void ClampsSavedBallPositionInsideWorkArea()
    {
        var position = WindowPlacementCalculator.ClampInsideWorkArea(
            new Point(-694, 54),
            new Size(68, 68),
            WorkArea);

        Assert.Equal(WorkArea.Left, position.X);
        Assert.Equal(54, position.Y);
    }
}
