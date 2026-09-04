namespace AntigravityUsageWidget.Tests;

public sealed class WaterBallAnimationPolicyTests
{
    [Fact]
    public void UsesLowerFrameRateForNormalState()
    {
        Assert.Equal(
            TimeSpan.FromMilliseconds(240),
            WaterBallAnimationPolicy.GetInterval(60, 0, false));
    }

    [Fact]
    public void UsesModerateFrameRateForHoverOrFastUsage()
    {
        Assert.Equal(
            TimeSpan.FromMilliseconds(160),
            WaterBallAnimationPolicy.GetInterval(60, 220_000, false));
        Assert.Equal(
            TimeSpan.FromMilliseconds(160),
            WaterBallAnimationPolicy.GetInterval(60, 0, true));
    }

    [Fact]
    public void UsesSlowerRefreshWhenOfficialPercentageIsUnavailable()
    {
        Assert.Equal(
            TimeSpan.FromMilliseconds(360),
            WaterBallAnimationPolicy.GetInterval(null, 0, false));
    }
}
