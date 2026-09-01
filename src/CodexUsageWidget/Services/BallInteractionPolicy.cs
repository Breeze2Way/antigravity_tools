namespace CodexUsageWidget.Services;

public static class BallInteractionPolicy
{
    public const double ClickMoveTolerance = 4;

    public static bool ShouldRefreshAfterDrag(double horizontalDelta, double verticalDelta)
    {
        return Math.Abs(horizontalDelta) < ClickMoveTolerance &&
            Math.Abs(verticalDelta) < ClickMoveTolerance;
    }
}
