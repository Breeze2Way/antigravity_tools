namespace AntigravityUsageWidget.Tests;

public sealed class AntigravityTokenUsageTests
{
    [Fact]
    public void ParsesModelUsageAndTimestampFromStepMetadata()
    {
        var metadata = ProtobufFixture.Metadata(
            timestamp: new DateTimeOffset(2026, 9, 4, 10, 30, 0, TimeSpan.FromHours(8)),
            inputTokens: 1_250_000,
            outputTokens: 250_000);

        var usage = AntigravityTokenUsageMetadataParser.Parse(metadata);

        Assert.NotNull(usage);
        Assert.Equal(new DateTimeOffset(2026, 9, 4, 2, 30, 0, TimeSpan.Zero), usage.Timestamp);
        Assert.Equal(1_250_000, usage.InputTokens);
        Assert.Equal(250_000, usage.OutputTokens);
        Assert.Equal(1_500_000, usage.TotalTokens);
    }

    [Fact]
    public void AggregatesTodayAndYesterdayUsingLocalDates()
    {
        var now = new DateTimeOffset(2026, 9, 4, 8, 0, 0, TimeSpan.FromHours(8));
        var records = new[]
        {
            new AntigravityTokenUsageRecord(
                new DateTimeOffset(2026, 9, 4, 0, 5, 0, TimeSpan.Zero), 2_000_000, 100_000),
            new AntigravityTokenUsageRecord(
                new DateTimeOffset(2026, 9, 3, 15, 55, 0, TimeSpan.Zero), 300_000, 200_000),
            new AntigravityTokenUsageRecord(
                new DateTimeOffset(2026, 9, 2, 15, 0, 0, TimeSpan.Zero), 99_000, 1_000)
        };

        var summary = AntigravityTokenUsageAggregator.Aggregate(records, now);

        Assert.Equal(2_100_000, summary.TodayTokens);
        Assert.Equal(500_000, summary.YesterdayTokens);
    }

    [Fact]
    public void FormatsDailyTotalsInMillionsWithOneDecimalAndConfiguredLanguage()
    {
        var summary = new AntigravityTokenUsageSummary(3_450_039, 500_000);

        var chinese = AntigravityUsageDisplayFormatter.FormatTokenUsage(summary, english: false);
        var english = AntigravityUsageDisplayFormatter.FormatTokenUsage(summary, english: true);

        Assert.Contains("今日token:3.5M(昨日：0.5M)", chinese);
        Assert.Contains("Today tokens:3.5M (Yesterday:0.5M)", english);
        Assert.DoesNotContain("Today", chinese);
        Assert.DoesNotContain("今日", english);
    }

    [Fact]
    public void ReadsTokenUsageFromAllConversationDatabasesWithoutNeedingAService()
    {
        var directory = Path.Combine(Path.GetTempPath(), "AntigravityUsageWidgetTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            CreateConversationDatabase(
                Path.Combine(directory, "first.db"),
                ProtobufFixture.Metadata(
                    new DateTimeOffset(2026, 9, 4, 10, 30, 0, TimeSpan.FromHours(8)),
                    1_000_000,
                    100_000));
            CreateConversationDatabase(
                Path.Combine(directory, "second.db"),
                ProtobufFixture.Metadata(
                    new DateTimeOffset(2026, 9, 3, 10, 30, 0, TimeSpan.FromHours(8)),
                    200_000,
                    300_000));

            var reader = new AntigravityTokenUsageReader(directory);
            var summary = reader.Read(new DateTimeOffset(2026, 9, 4, 12, 0, 0, TimeSpan.FromHours(8)));

            Assert.Equal(1_100_000, summary.TodayTokens);
            Assert.Equal(500_000, summary.YesterdayTokens);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static void CreateConversationDatabase(string path, byte[] metadata)
    {
        var connectionString = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder
        {
            DataSource = path,
            Pooling = false
        }.ToString();
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection(connectionString);
        connection.Open();
        using var create = connection.CreateCommand();
        create.CommandText = "CREATE TABLE steps (idx INTEGER, metadata BLOB);";
        create.ExecuteNonQuery();
        using var insert = connection.CreateCommand();
        insert.CommandText = "INSERT INTO steps (idx, metadata) VALUES (0, $metadata);";
        insert.Parameters.AddWithValue("$metadata", metadata);
        insert.ExecuteNonQuery();
    }

    private static class ProtobufFixture
    {
        public static byte[] Metadata(DateTimeOffset timestamp, long inputTokens, long outputTokens)
        {
            var timestampMessage = Message(
                VarintField(1, timestamp.ToUnixTimeSeconds()),
                VarintField(2, 0));
            var usageMessage = Message(
                VarintField(1, 1319),
                VarintField(2, inputTokens),
                VarintField(3, outputTokens));
            return Message(
                BytesField(1, timestampMessage),
                BytesField(9, usageMessage));
        }

        private static byte[] Message(params byte[][] fields) => fields.SelectMany(field => field).ToArray();

        private static byte[] VarintField(int fieldNumber, long value)
        {
            return Varint((fieldNumber << 3) | 0).Concat(Varint(value)).ToArray();
        }

        private static byte[] BytesField(int fieldNumber, byte[] value)
        {
            return Varint((fieldNumber << 3) | 2).Concat(Varint(value.Length)).Concat(value).ToArray();
        }

        private static byte[] Varint(long value)
        {
            var bytes = new List<byte>();
            var unsigned = unchecked((ulong)value);
            do
            {
                var current = (byte)(unsigned & 0x7F);
                unsigned >>= 7;
                bytes.Add(unsigned == 0 ? current : (byte)(current | 0x80));
            }
            while (unsigned != 0);

            return bytes.ToArray();
        }
    }
}
