namespace AntigravityUsageWidget.Tests;

public sealed class AntigravityUsageRefreshServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 4, 4, 0, 0, TimeSpan.Zero);

    [Fact]
    public void BuildsWidgetStateFromOfficialAntigravityQuotas()
    {
        var service = new AntigravityUsageRefreshService(() => Snapshot());

        var state = service.Refresh(Now);

        Assert.Equal(66.95, state.FiveHourRemainingPercent!.Value, precision: 6);
        Assert.Equal(94.49, state.OfficialRemainingPercent!.Value, precision: 6);
        Assert.Equal("Pro", state.Quota!.PlanName);
        Assert.False(state.IsEstimate);
        Assert.Contains("Antigravity", state.Status);
    }

    [Fact]
    public void RetainsLastSuccessfulQuotaWhenTheServerBecomesUnavailable()
    {
        var reads = 0;
        var service = new AntigravityUsageRefreshService(() => reads++ == 0 ? Snapshot() : null);

        var first = service.Refresh(Now);
        var later = service.Refresh(Now.AddMinutes(1));

        Assert.Equal(first.Quota, later.Quota);
        Assert.Equal(first.RefreshedAt, later.RefreshedAt);
        Assert.Contains("保留上次数据", later.Status);
        Assert.Equal(2, reads);
    }

    private static AntigravityQuotaSnapshot Snapshot()
    {
        return new AntigravityQuotaSnapshot(
            "Pro",
            [
                new("5h", "Gemini Models", 66.95, Now.AddHours(3), AntigravityQuotaPeriod.Short),
                new("weekly", "Gemini Models", 94.49, Now.AddDays(6), AntigravityQuotaPeriod.Weekly)
            ],
            Now);
    }
}
