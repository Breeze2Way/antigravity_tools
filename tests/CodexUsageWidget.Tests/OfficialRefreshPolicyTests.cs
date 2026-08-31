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

    [Fact]
    public void AllowsTheFirstAutomaticReadAfterTheQuietWindow()
    {
        var now = DateTimeOffset.UtcNow;

        Assert.True(OfficialRefreshPolicy.ShouldReadAutomatically(false, now, null));
    }

    [Fact]
    public void BlocksAutomaticReadsDuringTheCooldown()
    {
        var lastReadAt = DateTimeOffset.UtcNow;

        Assert.False(OfficialRefreshPolicy.ShouldReadAutomatically(
            userActive: false,
            now: lastReadAt.AddMinutes(9),
            lastReadAt));
    }

    [Fact]
    public void AllowsAutomaticReadsAfterTheCooldown()
    {
        var lastReadAt = DateTimeOffset.UtcNow;

        Assert.True(OfficialRefreshPolicy.ShouldReadAutomatically(
            userActive: false,
            now: lastReadAt.AddMinutes(10),
            lastReadAt));
    }

    [Fact]
    public void NeverReadsAutomaticallyWhileTheUserIsActive()
    {
        var lastReadAt = DateTimeOffset.UtcNow.AddHours(-1);

        Assert.False(OfficialRefreshPolicy.ShouldReadAutomatically(
            userActive: true,
            now: DateTimeOffset.UtcNow,
            lastReadAt));
    }
}
