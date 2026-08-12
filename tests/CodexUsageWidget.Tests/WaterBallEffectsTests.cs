namespace CodexUsageWidget.Tests;

public sealed class WaterBallEffectsTests
{
    [Fact]
    public void FasterUsageAndHoverIncreaseGlow()
    {
        var calm = WaterBallEffects.GetGlowOpacity(60, 0, false);
        var fast = WaterBallEffects.GetGlowOpacity(60, 220_000, false);
        var hovered = WaterBallEffects.GetGlowOpacity(60, 0, true);

        Assert.True(fast > calm);
        Assert.True(hovered > calm);
        Assert.InRange(fast, 0, 1);
        Assert.InRange(hovered, 0, 1);
    }

    [Fact]
    public void LowRemainingProducesAlertPulseOnlyBelowTwentyPercent()
    {
        Assert.Equal(0, WaterBallEffects.GetAlertPulse(25, 0.5), precision: 6);
        Assert.NotEqual(0, WaterBallEffects.GetAlertPulse(10, 0.5));
    }

    [Fact]
    public void LowRemainingAddsAlertRingThickness()
    {
        var normal = WaterBallEffects.GetAlertRingThickness(60, 0.5);
        var alert = WaterBallEffects.GetAlertRingThickness(10, 0.5);

        Assert.Equal(1.5, normal, precision: 6);
        Assert.True(alert > normal);
    }

    [Fact]
    public void BubbleVisibilityUsesUsageAndRemainingState()
    {
        var calm = WaterBallEffects.GetBubbleVisibility(80, 0, 2, 0.4);
        var fast = WaterBallEffects.GetBubbleVisibility(80, 220_000, 2, 0.4);
        var empty = WaterBallEffects.GetBubbleVisibility(null, 220_000, 2, 0.4);

        Assert.True(fast > calm);
        Assert.Equal(0, empty, precision: 6);
        Assert.InRange(fast, 0, 1);
    }
}
