namespace AntigravityUsageWidget.Tests;

public sealed class AntigravityQuotaParserTests
{
    [Fact]
    public void ParsesQuotaSummaryGroupsAndConvertsFractionsToPercentages()
    {
        const string json = """
        {
          "response": {
            "groups": [
              {
                "displayName": "Gemini Models",
                "buckets": [
                  { "displayName": "Weekly Limit Remaining", "window": "weekly", "remainingFraction": 0.94491875, "resetTime": "2026-09-11T02:11:54Z" },
                  { "displayName": "Five Hour Limit Remaining", "window": "5h", "remainingFraction": 0.6695125, "resetTime": "2026-09-04T07:11:54Z" }
                ]
              },
              {
                "displayName": "Claude and GPT models",
                "buckets": [
                  { "displayName": "Weekly Limit Remaining", "window": "weekly", "remainingFraction": 1, "resetTime": "2026-09-11T04:04:58Z" },
                  { "displayName": "Five Hour Limit Remaining", "window": "5h", "remainingFraction": 1, "resetTime": "2026-09-04T09:04:58Z" }
                ]
              }
            ]
          }
        }
        """;

        var snapshot = AntigravityQuotaParser.Parse(json);

        Assert.NotNull(snapshot);
        Assert.Equal(4, snapshot!.Rows.Count);
        Assert.Equal("Gemini Models", snapshot.Rows[0].Group);
        Assert.Equal(94.491875, snapshot.Rows[0].RemainingPercent, precision: 6);
        Assert.Equal(AntigravityQuotaPeriod.Weekly, snapshot.Rows[0].Period);
        Assert.Equal(
            new DateTimeOffset(2026, 9, 11, 2, 11, 54, TimeSpan.Zero),
            snapshot.Rows[0].ResetAt);
    }

    [Fact]
    public void ParsesUserStatusModelRowsAsShortPeriodQuotas()
    {
        const string json = """
        {
          "userStatus": {
            "planStatus": { "planInfo": { "planName": "Pro" } },
            "cascadeModelConfigData": {
              "clientModelConfigs": [
                { "label": "Gemini 3.8 Flash (Medium)", "quotaInfo": { "remainingFraction": 0.67, "resetTime": "2026-09-04T07:11:54Z" } },
                { "label": "internal model", "isInternal": true, "quotaInfo": { "remainingFraction": 0.1 } },
                { "label": "", "quotaInfo": { "remainingFraction": 0.2 } }
              ]
            }
          }
        }
        """;

        var snapshot = AntigravityQuotaParser.Parse(json);

        Assert.NotNull(snapshot);
        var row = Assert.Single(snapshot!.Rows);
        Assert.Equal("Pro", snapshot.PlanName);
        Assert.Equal("Gemini 3.8 Flash (Medium)", row.Label);
        Assert.Equal(67, row.RemainingPercent, precision: 6);
        Assert.Equal(AntigravityQuotaPeriod.Short, row.Period);
    }

    [Fact]
    public void ParsesTheSelectedModelFromUserStatus()
    {
        const string json = """
        {
          "userStatus": {
            "cascadeModelConfigData": {
              "defaultOverrideModelConfig": {
                "modelOrAlias": { "model": "MODEL_PLACEHOLDER_M318" }
              },
              "clientModelConfigs": [
                {
                  "label": "Gemini 3.8 Flash (High)",
                  "modelOrAlias": { "model": "MODEL_PLACEHOLDER_M318" },
                  "modelId": "gemini-3.8-flash-high",
                  "quotaInfo": { "remainingFraction": 0.8 }
                }
              ]
            }
          }
        }
        """;

        var snapshot = AntigravityQuotaParser.Parse(json);

        Assert.NotNull(snapshot);
        Assert.Equal("MODEL_PLACEHOLDER_M318", snapshot!.SelectedModelId);
        Assert.Equal("Gemini 3.8 Flash (High)", snapshot.SelectedModelLabel);
        Assert.Equal("MODEL_PLACEHOLDER_M318", snapshot.Rows[0].ModelId);
    }

    [Fact]
    public void SkipsInvalidRowsAndReturnsNullWhenNothingIsUsable()
    {
        const string json = """
        {
          "response": {
            "groups": [
              { "displayName": "Bad", "buckets": [
                { "displayName": "broken", "window": "5h", "remainingFraction": 1.2 },
                { "displayName": "missing fraction", "window": "weekly" },
                { "displayName": "", "window": "weekly", "remainingFraction": 0.5 }
              ] }
            ]
          }
        }
        """;

        Assert.Null(AntigravityQuotaParser.Parse(json));
        Assert.Null(AntigravityQuotaParser.Parse("not-json"));
    }
}
