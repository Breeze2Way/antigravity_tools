using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace CodexUsageWidget.Tests;

public sealed class CodexDataReaderTests
{
    [Fact]
    public void ReusesUnchangedRolloutFileOnSubsequentReads()
    {
        using var temp = new TemporaryDirectory();
        var sessions = Directory.CreateDirectory(Path.Combine(temp.Path, "sessions")).FullName;
        var rollout = Path.Combine(sessions, "rollout-cache.jsonl");
        File.WriteAllText(rollout, UsageJson("2026-08-11T08:00:00Z", 13));
        var paths = new CodexDataPaths("missing.db", sessions);
        var reader = new CodexDataReader();

        reader.Read(paths);
        reader.Read(paths);

        Assert.Equal(0, reader.FilesReadForLastCall);
    }

    [Fact]
    public void ReadsValidUsageAndSkipsMalformedLines()
    {
        using var temp = new TemporaryDirectory();
        var sessions = Directory.CreateDirectory(Path.Combine(temp.Path, "sessions")).FullName;
        var rollout = Path.Combine(sessions, "rollout-test.jsonl");
        File.WriteAllLines(rollout,
        [
            UsageJson("2026-08-11T08:00:00Z", 13),
            "{not-json",
            "{\"type\":\"message\"}"
        ]);

        var result = new CodexDataReader().Read(new CodexDataPaths("missing.db", sessions));

        var record = Assert.Single(result.Records);
        Assert.Equal(13, record.Usage.TotalTokens);
        Assert.Equal(1, result.MalformedLineCount);
        Assert.Null(result.Warning);
    }

    [Fact]
    public void ReportsMissingSessionsDirectoryWithoutThrowing()
    {
        using var temp = new TemporaryDirectory();

        var result = new CodexDataReader().Read(
            new CodexDataPaths(Path.Combine(temp.Path, "missing.db"), Path.Combine(temp.Path, "missing-sessions")));

        Assert.Empty(result.Records);
        Assert.NotNull(result.Warning);
    }

    [Fact]
    public void UsesRolloutPathsFromReadOnlyStateDatabase()
    {
        using var temp = new TemporaryDirectory();
        var rollout = Path.Combine(temp.Path, "indexed-rollout.jsonl");
        File.WriteAllText(rollout, UsageJson("2026-08-11T09:00:00Z", 21));
        var database = Path.Combine(temp.Path, "state.sqlite");

        using (var connection = new SqliteConnection($"Data Source={database};Pooling=False"))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "CREATE TABLE threads (rollout_path TEXT); INSERT INTO threads (rollout_path) VALUES ($path);";
            command.Parameters.AddWithValue("$path", rollout);
            command.ExecuteNonQuery();
        }

        var result = new CodexDataReader().Read(
            new CodexDataPaths(database, Path.Combine(temp.Path, "sessions-not-needed")));

        var record = Assert.Single(result.Records);
        Assert.Equal(21, record.Usage.TotalTokens);
        Assert.Null(result.Warning);
    }

    [Fact]
    public void IgnoresSubagentRolloutPathsFromStateDatabase()
    {
        using var temp = new TemporaryDirectory();
        var normalRollout = Path.Combine(temp.Path, "normal-rollout.jsonl");
        var subagentRollout = Path.Combine(temp.Path, "subagent-rollout.jsonl");
        File.WriteAllText(normalRollout, UsageJson("2026-08-11T09:00:00Z", 21));
        File.WriteAllText(subagentRollout, UsageJson("2026-08-11T09:01:00Z", 99));
        var database = Path.Combine(temp.Path, "state.sqlite");

        using (var connection = new SqliteConnection($"Data Source={database};Pooling=False"))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "CREATE TABLE threads (rollout_path TEXT, source TEXT); INSERT INTO threads (rollout_path, source) VALUES ($normal, 'vscode'), ($subagent, '{\"subagent\":{}}');";
            command.Parameters.AddWithValue("$normal", normalRollout);
            command.Parameters.AddWithValue("$subagent", subagentRollout);
            command.ExecuteNonQuery();
        }

        var result = new CodexDataReader().Read(
            new CodexDataPaths(database, Path.Combine(temp.Path, "sessions-not-needed")));

        var record = Assert.Single(result.Records);
        Assert.Equal(21, record.Usage.TotalTokens);
    }

    [Fact]
    public void ReadsLatestRateLimitMetadataWithoutOpeningTheCodexUi()
    {
        using var temp = new TemporaryDirectory();
        var sessions = Directory.CreateDirectory(Path.Combine(temp.Path, "sessions")).FullName;
        var rollout = Path.Combine(sessions, "rollout-rate-limit.jsonl");
        File.WriteAllText(
            rollout,
            "{\"timestamp\":\"2026-08-11T08:00:00Z\",\"rate_limits\":{\"primary\":{\"used_percent\":12.5,\"window_minutes\":10080,\"resets_at\":1788144616}}}");

        var result = new CodexDataReader().Read(new CodexDataPaths("missing.db", sessions));

        Assert.Empty(result.Records);
        Assert.Equal(87.5, result.LatestRateLimit!.RemainingPercent, precision: 6);
    }

    [Fact]
    public void KeepsLatestFiveHourAndWeeklyRateLimitsSeparately()
    {
        using var temp = new TemporaryDirectory();
        var sessions = Directory.CreateDirectory(Path.Combine(temp.Path, "sessions")).FullName;
        var rollout = Path.Combine(sessions, "rollout-rate-limits.jsonl");
        File.WriteAllLines(
            rollout,
            [
                "{\"timestamp\":\"2026-08-11T08:00:00Z\",\"payload\":{\"rate_limits\":{\"primary\":{\"used_percent\":10,\"window_minutes\":300,\"resets_at\":1787718550},\"secondary\":{\"used_percent\":20,\"window_minutes\":10080,\"resets_at\":1788305350}}}}",
                "{\"timestamp\":\"2026-08-11T09:00:00Z\",\"payload\":{\"rate_limits\":{\"primary\":{\"used_percent\":25,\"window_minutes\":300,\"resets_at\":1787718550},\"secondary\":{\"used_percent\":22,\"window_minutes\":10080,\"resets_at\":1788305350}}}}"
            ]);

        var result = new CodexDataReader().Read(new CodexDataPaths("missing.db", sessions));

        Assert.Equal(75, result.LatestFiveHourRateLimit!.RemainingPercent, precision: 6);
        Assert.Equal(78, result.LatestWeeklyRateLimit!.RemainingPercent, precision: 6);
    }

    private static string UsageJson(string timestamp, long totalTokens) => JsonSerializer.Serialize(new
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
        public TemporaryDirectory() => Path = Directory.CreateTempSubdirectory("CodexUsageWidgetTests").FullName;

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
