using System.IO;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using CodexUsageWidget.Models;

namespace CodexUsageWidget.Data;

public sealed record DataReadResult(
    IReadOnlyList<UsageRecord> Records,
    int MalformedLineCount,
    string? Warning,
    LocalRateLimitSnapshot? LatestRateLimit = null);

public sealed class CodexDataReader
{
    public DataReadResult Read(CodexDataPaths paths)
    {
        var warnings = new List<string>();
        var files = LocateRolloutFiles(paths, warnings);
        var records = new List<UsageRecord>();
        var malformedLineCount = 0;
        LocalRateLimitSnapshot? latestRateLimit = null;

        foreach (var file in files)
        {
            try
            {
                using var stream = new FileStream(
                    file,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete);
                using var reader = new StreamReader(stream);

                string? line;
                while ((line = reader.ReadLine()) is not null)
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    try
                    {
                        using var document = JsonDocument.Parse(line);
                        if (UsageJsonParser.TryParseRateLimit(line, out var rateLimit) &&
                            rateLimit is not null &&
                            (latestRateLimit is null || rateLimit.RecordedAt > latestRateLimit.RecordedAt))
                        {
                            latestRateLimit = rateLimit;
                        }

                        if (UsageJsonParser.TryParse(line, file, out var record) && record is not null)
                        {
                            records.Add(record);
                        }
                    }
                    catch (JsonException)
                    {
                        malformedLineCount++;
                    }
                }
            }
            catch (IOException exception)
            {
                warnings.Add($"无法读取 {Path.GetFileName(file)}: {exception.Message}");
            }
            catch (UnauthorizedAccessException exception)
            {
                warnings.Add($"无法访问 {Path.GetFileName(file)}: {exception.Message}");
            }
        }

        return new DataReadResult(
            records,
            malformedLineCount,
            warnings.Count == 0 ? null : string.Join("; ", warnings),
            latestRateLimit);
    }

    private static IReadOnlyList<string> LocateRolloutFiles(CodexDataPaths paths, List<string> warnings)
    {
        var indexedPaths = ReadIndexedRolloutPaths(paths.StateDatabasePath);
        if (indexedPaths.Count > 0)
        {
            return indexedPaths
                .Select(NormalizePath)
                .Where(File.Exists)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        if (!Directory.Exists(paths.SessionsDirectory))
        {
            warnings.Add("未找到 Codex sessions 目录");
            return [];
        }

        try
        {
            return Directory
                .EnumerateFiles(paths.SessionsDirectory, "*.jsonl", SearchOption.AllDirectories)
                .ToArray();
        }
        catch (IOException exception)
        {
            warnings.Add($"无法枚举 Codex sessions 目录: {exception.Message}");
            return [];
        }
        catch (UnauthorizedAccessException exception)
        {
            warnings.Add($"无法访问 Codex sessions 目录: {exception.Message}");
            return [];
        }
    }

    private static IReadOnlyList<string> ReadIndexedRolloutPaths(string databasePath)
    {
        if (!File.Exists(databasePath))
        {
            return [];
        }

        try
        {
            using var connection = new SqliteConnection(
                new SqliteConnectionStringBuilder
                {
                    DataSource = databasePath,
                    Mode = SqliteOpenMode.ReadOnly,
                    Cache = SqliteCacheMode.Shared,
                    Pooling = false
                }.ToString());
            connection.Open();

            try
            {
                return ReadIndexedRolloutPaths(connection, includeSource: true);
            }
            catch (SqliteException)
            {
                // Older state databases may not have the source column.
                return ReadIndexedRolloutPaths(connection, includeSource: false);
            }
        }
        catch (SqliteException)
        {
            return [];
        }
        catch (IOException)
        {
            return [];
        }
        catch (UnauthorizedAccessException)
        {
            return [];
        }
    }

    private static IReadOnlyList<string> ReadIndexedRolloutPaths(
        SqliteConnection connection,
        bool includeSource)
    {
        using var command = connection.CreateCommand();
        command.CommandText = includeSource
            ? "SELECT rollout_path, source FROM threads WHERE rollout_path IS NOT NULL AND rollout_path <> ''"
            : "SELECT rollout_path FROM threads WHERE rollout_path IS NOT NULL AND rollout_path <> ''";
        using var reader = command.ExecuteReader();
        var paths = new List<string>();
        while (reader.Read())
        {
            if (reader.IsDBNull(0))
            {
                continue;
            }

            if (includeSource && !reader.IsDBNull(1) && IsSubagentSource(reader.GetString(1)))
            {
                continue;
            }

            paths.Add(reader.GetString(0));
        }

        return paths;
    }

    private static bool IsSubagentSource(string source)
    {
        return source.Contains("subagent", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizePath(string path)
    {
        var normalized = path.Trim();
        if (normalized.StartsWith(@"\\?\", StringComparison.Ordinal))
        {
            normalized = normalized[4..];
        }

        return normalized;
    }
}
