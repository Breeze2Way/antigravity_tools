using AntigravityUsageWidget.Data;

namespace AntigravityUsageWidget.Tests;

public sealed class OfficialUsageApiParserTests
{
    [Fact]
    public void ParsesPrimaryAndSecondaryWindowsAsRemainingPercentages()
    {
        const string json = """
            {
              "rate_limit": {
                "allowed": true,
                "primary_window": {
                  "used_percent": 4,
                  "reset_after_seconds": 16182
                },
                "secondary_window": {
                  "used_percent": 1,
                  "reset_after_seconds": 602982
                }
              }
            }
            """;

        var result = OfficialUsageApiParser.Parse(json);

        Assert.NotNull(result);
        Assert.Equal(99, result!.RemainingPercent);
        Assert.Equal(TimeSpan.FromSeconds(602982), result.ResetAfter);
        Assert.Equal(96, result.FiveHourRemainingPercent);
        Assert.Equal(TimeSpan.FromSeconds(16182), result.FiveHourResetAfter);
    }

    [Fact]
    public void ReturnsNullForMalformedOrMissingRateLimitData()
    {
        Assert.Null(OfficialUsageApiParser.Parse("{\"rate_limit\":null}"));
        Assert.Null(OfficialUsageApiParser.Parse("not-json"));
    }

    [Fact]
    public void ClampsInvalidPercentagesToTheSupportedRange()
    {
        const string json = """
            {
              "rate_limit": {
                "primary_window": { "used_percent": -5 },
                "secondary_window": { "used_percent": 150 }
              }
            }
            """;

        var result = OfficialUsageApiParser.Parse(json);

        Assert.NotNull(result);
        Assert.Equal(100, result!.FiveHourRemainingPercent);
        Assert.Equal(0, result.RemainingPercent);
    }

    [Fact]
    public void KeepsWeeklyRemainingWhenTheAggregateLimitIsBlocked()
    {
        const string json = """
            {
              "rate_limit": {
                "allowed": false,
                "primary_window": { "used_percent": 100 },
                "secondary_window": { "used_percent": 30 }
              }
            }
            """;

        var result = OfficialUsageApiParser.Parse(json);

        Assert.NotNull(result);
        Assert.Equal(0, result!.FiveHourRemainingPercent);
        Assert.Equal(70, result.RemainingPercent);
    }
}
