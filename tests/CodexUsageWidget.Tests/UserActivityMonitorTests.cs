using CodexUsageWidget.Services;

namespace CodexUsageWidget.Tests;

public sealed class UserActivityMonitorTests
{
    [Fact]
    public void TreatsRecentInputAsActive()
    {
        Assert.True(UserActivityMonitor.IsWithinPauseWindow(
            currentTick: 10_000,
            lastInputTick: 8_500,
            pauseMilliseconds: 2_000));
    }

    [Fact]
    public void AllowsOfficialRefreshAfterPauseWindow()
    {
        Assert.False(UserActivityMonitor.IsWithinPauseWindow(
            currentTick: 10_000,
            lastInputTick: 7_900,
            pauseMilliseconds: 2_000));
    }

    [Fact]
    public void HandlesTickCounterWrapAround()
    {
        Assert.True(UserActivityMonitor.IsWithinPauseWindow(
            currentTick: 250,
            lastInputTick: uint.MaxValue - 500,
            pauseMilliseconds: 1_000));
    }

    [Fact]
    public void ReportsRemainingQuietTimeBeforeOfficialRead()
    {
        Assert.Equal(
            3_500u,
            UserActivityMonitor.GetRemainingQuietMilliseconds(
                currentTick: 10_000,
                lastInputTick: 8_500,
                quietMilliseconds: 5_000));
    }

    [Fact]
    public void ReportsNoDelayAfterContinuousQuietPeriod()
    {
        Assert.Equal(
            0u,
            UserActivityMonitor.GetRemainingQuietMilliseconds(
                currentTick: 10_000,
                lastInputTick: 4_500,
                quietMilliseconds: 5_000));
    }
}
