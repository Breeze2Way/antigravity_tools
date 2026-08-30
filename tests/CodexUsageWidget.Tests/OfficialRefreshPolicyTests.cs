namespace CodexUsageWidget.Tests;

public sealed class OfficialRefreshPolicyTests
{
    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void SkipsAutomaticUiReadWhileUserIsActive(bool userActive, bool expected)
    {
        Assert.Equal(expected, OfficialRefreshPolicy.ShouldReadAutomatically(userActive));
    }

    [Fact]
    public void ManualRefreshReadsOfficialDataWhenExplicitlyRequested()
    {
        Assert.True(OfficialRefreshPolicy.ShouldReadOnManualRefresh);
    }
}
