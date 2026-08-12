using System.Text.Json;

namespace CodexUsageWidget.Tests;

public sealed class UsageRefreshServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 11, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void BuildsFiveHourAndSevenDaySnapshotsFromOneRead()
    {
        using var temp = new TemporaryDirectory();
        var sessions = Directory.CreateDirectory(Path.Combine(temp.Path, "sessions")).FullName;
        File.WriteAllLines(Path.Combine(sessions, "rollout.jsonl"),
        [
            UsageJson(Now.AddHours(-1), 100),
            UsageJson(Now.AddHours(-6), 200)
        ]);

        var service = new UsageRefreshService(
            new CodexDataReader(),
            new CodexDataPaths(Path.Combine(temp.Path, "missing.db"), sessions));

        var state = service.Refresh(Now, new WidgetSettings(1_000, 30)
        {
            WeeklyBudgetConfigured = true
        });

        Assert.Equal(100, state.FiveHour.Usage.TotalTokens);
        Assert.Equal(300, state.SevenDay.Usage.TotalTokens);
        Assert.Equal(300, state.ThirtyDay.Usage.TotalTokens);
        Assert.Equal(90, state.FiveHour.RemainingPercent, precision: 6);
        Assert.Equal(70, state.SevenDay.RemainingPercent, precision: 6);
        Assert.True(state.IsEstimate);
        Assert.Contains("本地估算", state.Status);
    }

    [Fact]
    public void RetainsLastSuccessfulSnapshotWhenReadReturnsWarning()
    {
        var first = new DataReadResult(
            [new UsageRecord(
                Now.AddHours(-1),
                new TokenUsage(50, 0, 0, 0, 0, 50),
                "session.jsonl",
                "record",
                false)],
            0,
            null);
        var reads = 0;
        var service = new UsageRefreshService(
            _ => reads++ == 0 ? first : new DataReadResult([], 0, "读取失败"),
            new CodexDataPaths("state.db", "sessions"));

        var configured = new WidgetSettings(1_000, 30)
        {
            WeeklyBudgetConfigured = true
        };
        var initial = service.Refresh(Now, configured);
        var retained = service.Refresh(Now.AddMinutes(1), configured);

        Assert.Equal(initial.FiveHour, retained.FiveHour);
        Assert.Equal(initial.SevenDay, retained.SevenDay);
        Assert.Equal(initial.RefreshedAt, retained.RefreshedAt);
        Assert.Contains("读取失败", retained.Status);
    }

    [Fact]
    public void UsesOfficialRemainingPercentWhenProviderReturnsIt()
    {
        var service = new UsageRefreshService(
            _ => new DataReadResult([], 0, null),
            new CodexDataPaths("state.db", "sessions"),
            () => 54);

        var state = service.Refresh(Now, new WidgetSettings());

        Assert.Equal(54, state.OfficialRemainingPercent);
        Assert.Contains("官方周剩余", state.Status);
    }

    [Fact]
    public void RetainsLastOfficialPercentWhenLaterReadFails()
    {
        var reads = 0;
        var service = new UsageRefreshService(
            _ => new DataReadResult([], 0, null),
            new CodexDataPaths("state.db", "sessions"),
            () => reads++ == 0 ? 54 : null);

        var first = service.Refresh(Now, new WidgetSettings());
        var later = service.Refresh(Now.AddMinutes(2), new WidgetSettings());

        Assert.Equal(54, first.OfficialRemainingPercent);
        Assert.Equal(54, later.OfficialRemainingPercent);
        Assert.Equal(2, reads);
    }

    [Fact]
    public void LocalRefreshUsesCachedOfficialPercentWithoutReadingOfficialUsage()
    {
        var reads = 0;
        var service = new UsageRefreshService(
            _ => new DataReadResult([], 0, null),
            new CodexDataPaths("state.db", "sessions"),
            () => reads++ == 0 ? 54 : throw new InvalidOperationException("official read should be skipped"));

        service.Refresh(Now, new WidgetSettings());
        var local = service.RefreshLocal(Now.AddMinutes(1), new WidgetSettings());

        Assert.Equal(54, local.OfficialRemainingPercent);
        Assert.Equal(1, reads);
    }

    [Fact]
    public void IncludesRecentTokenRateForWaterWaveAnimation()
    {
        var records = new DataReadResult(
            [
                new UsageRecord(
                    Now.AddMinutes(-1),
                    new TokenUsage(1_000, 0, 0, 0, 0, 1_000),
                    "session.jsonl",
                    "recent",
                    false)
            ],
            0,
            null);
        var service = new UsageRefreshService(
            _ => records,
            new CodexDataPaths("state.db", "sessions"));

        var state = service.Refresh(Now, new WidgetSettings());

        Assert.Equal(200, state.RecentTokensPerMinute, precision: 6);
    }

    private static string UsageJson(DateTimeOffset timestamp, long totalTokens) => JsonSerializer.Serialize(new
    {
        timestamp,
        payload = new
        {
            info = new
            {
                last_token_usage = new
                {
                    input_tokens = totalTokens,
                    cached_input_tokens = 0,
                    cache_write_input_tokens = 0,
                    output_tokens = 0,
                    reasoning_output_tokens = 0,
                    total_tokens = totalTokens
                }
            }
        }
    });

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory() => Path = Directory.CreateTempSubdirectory("CodexUsageWidgetRefreshTests").FullName;

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
