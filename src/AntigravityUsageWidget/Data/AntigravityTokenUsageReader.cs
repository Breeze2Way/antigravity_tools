using Microsoft.Data.Sqlite;
using System.IO;

namespace AntigravityUsageWidget.Data;

public sealed class AntigravityTokenUsageReader
{
    private readonly string conversationsDirectory;

    public AntigravityTokenUsageReader(string? conversationsDirectory = null)
    {
        this.conversationsDirectory = conversationsDirectory ?? GetDefaultConversationsDirectory();
    }

    public AntigravityTokenUsageSummary Read(DateTimeOffset now)
    {
        if (!Directory.Exists(conversationsDirectory))
        {
            return new AntigravityTokenUsageSummary(0, 0);
        }

        var records = new List<AntigravityTokenUsageRecord>();
        foreach (var databasePath in Directory.EnumerateFiles(conversationsDirectory, "*.db"))
        {
            try
            {
                ReadDatabase(databasePath, records);
            }
            catch (SqliteException)
            {
                // A conversation can be temporarily locked or use a newer schema.
                // The remaining conversation databases are still useful.
            }
            catch (IOException)
            {
                // Antigravity may rotate or remove a database while it is being read.
            }
            catch (UnauthorizedAccessException)
            {
                // One inaccessible conversation must not hide the others.
            }
        }

        return Services.AntigravityTokenUsageAggregator.Aggregate(records, now);
    }

    private static void ReadDatabase(string databasePath, ICollection<AntigravityTokenUsageRecord> records)
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadOnly,
            Cache = SqliteCacheMode.Shared,
            Pooling = false
        }.ToString();
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT metadata FROM steps WHERE metadata IS NOT NULL";
        using var reader = command.ExecuteReader();
        while (reader.Read() && !reader.IsDBNull(0))
        {
            var metadata = (byte[])reader[0];
            var usage = AntigravityTokenUsageMetadataParser.Parse(metadata);
            if (usage is not null)
            {
                records.Add(usage);
            }
        }
    }

    private static string GetDefaultConversationsDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".gemini",
            "antigravity",
            "conversations");
    }
}
