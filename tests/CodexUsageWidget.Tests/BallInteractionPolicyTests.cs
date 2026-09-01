using CodexUsageWidget.Services;

namespace CodexUsageWidget.Tests;

public sealed class BallInteractionPolicyTests
{
    [Fact]
    public void TreatsAStationaryReleaseAsAClick()
    {
        Assert.True(BallInteractionPolicy.ShouldRefreshAfterDrag(0, 0));
    }

    [Fact]
    public void TreatsSmallPointerJitterAsAClick()
    {
        Assert.True(BallInteractionPolicy.ShouldRefreshAfterDrag(3, -3));
    }

    [Fact]
    public void TreatsARealMoveAsADrag()
    {
        Assert.False(BallInteractionPolicy.ShouldRefreshAfterDrag(8, 0));
        Assert.False(BallInteractionPolicy.ShouldRefreshAfterDrag(0, -8));
    }
}
