namespace CodexUsageWidget.Tests;

public sealed class WaterBallAnimationPolicyTests
{
    [Fact]
    public void UsesTenFramesPerSecondForNormalState()
    {
        Assert.Equal(
            TimeSpan.FromMilliseconds(100),
            WaterBallAnimationPolicy.GetInterval(60, 0, false));
    }

    [Fact]
    public void UsesTwentyFramesPerSecondForHoverOrFastUsage()
    {
        Assert.Equal(
            TimeSpan.FromMilliseconds(50),
            WaterBallAnimationPolicy.GetInterval(60, 220_000, false));
        Assert.Equal(
            TimeSpan.FromMilliseconds(50),
            WaterBallAnimationPolicy.GetInterval(60, 0, true));
    }

    [Fact]
    public void UsesSlowerRefreshWhenOfficialPercentageIsUnavailable()
    {
        Assert.Equal(
            TimeSpan.FromMilliseconds(160),
            WaterBallAnimationPolicy.GetInterval(null, 0, false));
    }
}
