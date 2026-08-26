namespace CodexUsageWidget.Tests;

public sealed class WaterBallDisplayTests
{
    [Fact]
    public void MissingPercentageProducesEmptyBallAndPlaceholder()
    {
        Assert.Null(WaterBallDisplay.GetFillRatio(null));
        Assert.Equal("--", WaterBallDisplay.FormatCenterText(null));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(53, 0.53)]
    [InlineData(100, 1)]
    [InlineData(-10, 0)]
    [InlineData(140, 1)]
    public void ConvertsPercentageToClampedFillRatio(double percentage, double expected)
    {
        Assert.Equal(expected, WaterBallDisplay.GetFillRatio(percentage)!.Value, precision: 6);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(25, 90)]
    [InlineData(50, 180)]
    [InlineData(100, 360)]
    [InlineData(-10, 0)]
    [InlineData(140, 360)]
    public void MapsRemainingPercentageToRingSweep(double percentage, double expected)
    {
        Assert.Equal(expected, WaterBallDisplay.GetRingSweepAngle(percentage)!.Value, precision: 6);
    }

    [Fact]
    public void MissingRemainingPercentageProducesNoRingSweep()
    {
        Assert.Null(WaterBallDisplay.GetRingSweepAngle(null));
    }

    [Theory]
    [InlineData(20.0, true)]
    [InlineData(20.1, false)]
    [InlineData(null, false)]
    public void FlagsOnlyLowValidRemainingValues(double? percentage, bool expected)
    {
        Assert.Equal(expected, WaterBallDisplay.IsLowRemaining(percentage));
    }

    [Theory]
    [InlineData(0, 239, 68, 68)]
    [InlineData(20, 250, 204, 21)]
    [InlineData(60, 59, 130, 246)]
    [InlineData(100, 34, 197, 94)]
    public void MapsRemainingPercentageToContinuousColorStops(
        double percentage,
        byte red,
        byte green,
        byte blue)
    {
        Assert.Equal(new WaterBallColor(red, green, blue), WaterBallDisplay.GetColor(percentage));
    }

    [Fact]
    public void UsesDifferentTintedBackgroundColorsAtFortyAndSixtyPercent()
    {
        var atForty = WaterBallDisplay.GetBackgroundColor(40);
        var atSixty = WaterBallDisplay.GetBackgroundColor(60);

        Assert.NotEqual(atForty, atSixty);
    }

    [Fact]
    public void UsesNeutralColorWhenOfficialPercentageIsUnavailable()
    {
        Assert.Equal(new WaterBallColor(128, 140, 153), WaterBallDisplay.GetColor(null));
    }

    [Theory]
    [InlineData(48, 20, 38)]
    [InlineData(48, 0, 48)]
    public void CalculatesTextOriginFromActualTextWidth(double center, double textWidth, double expected)
    {
        Assert.Equal(expected, WaterBallDisplay.GetCenteredTextOrigin(center, textWidth), precision: 6);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(125000, 0.5)]
    [InlineData(250000, 1)]
    [InlineData(500000, 1)]
    [InlineData(-1, 0)]
    public void MapsTokenRateToWaveIntensity(double tokensPerMinute, double expected)
    {
        Assert.Equal(expected, WaterWaveDisplay.GetIntensity(tokensPerMinute), precision: 6);
    }

    [Fact]
    public void FasterTokenRateProducesLargerAndFasterWaves()
    {
        var calmAmplitude = WaterWaveDisplay.GetAmplitude(10_000, 31);
        var fastAmplitude = WaterWaveDisplay.GetAmplitude(220_000, 31);
        var calmSpeed = WaterWaveDisplay.GetSpeed(10_000);
        var fastSpeed = WaterWaveDisplay.GetSpeed(220_000);

        Assert.True(fastAmplitude > calmAmplitude);
        Assert.True(fastSpeed > calmSpeed);
    }
}
