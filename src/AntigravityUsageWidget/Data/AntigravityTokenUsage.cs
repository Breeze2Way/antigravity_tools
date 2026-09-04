namespace AntigravityUsageWidget.Data;

public sealed record AntigravityTokenUsageRecord(
    DateTimeOffset Timestamp,
    long InputTokens,
    long OutputTokens)
{
    public long TotalTokens => InputTokens + OutputTokens;
}

public sealed record AntigravityTokenUsageSummary(
    long TodayTokens,
    long YesterdayTokens);

public static class AntigravityTokenUsageMetadataParser
{
    public static AntigravityTokenUsageRecord? Parse(ReadOnlySpan<byte> metadata)
    {
        try
        {
            var topLevel = ProtobufReader.ReadFields(metadata);
            var timestampPayload = GetBytes(topLevel, 1);
            var modelUsagePayload = GetBytes(topLevel, 9);
            if (timestampPayload is null || modelUsagePayload is null)
            {
                return null;
            }

            var timestampFields = ProtobufReader.ReadFields(timestampPayload);
            var seconds = GetVarint(timestampFields, 1);
            var nanos = GetVarint(timestampFields, 2) ?? 0;
            var usageFields = ProtobufReader.ReadFields(modelUsagePayload);
            var inputTokens = GetVarint(usageFields, 2);
            var outputTokens = GetVarint(usageFields, 3);
            if (!seconds.HasValue || !inputTokens.HasValue || !outputTokens.HasValue ||
                inputTokens.Value < 0 || outputTokens.Value < 0 || nanos < 0 || nanos >= 1_000_000_000)
            {
                return null;
            }

            return new AntigravityTokenUsageRecord(
                DateTimeOffset.FromUnixTimeSeconds(seconds.Value).AddTicks(nanos / 100),
                inputTokens.Value,
                outputTokens.Value);
        }
        catch (ArgumentException)
        {
            return null;
        }
        catch (OverflowException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static byte[]? GetBytes(IReadOnlyList<ProtobufField> fields, int number)
    {
        return fields.FirstOrDefault(field => field.Number == number)?.Bytes;
    }

    private static long? GetVarint(IReadOnlyList<ProtobufField> fields, int number)
    {
        return fields.FirstOrDefault(field => field.Number == number)?.Varint;
    }

    private sealed record ProtobufField(int Number, long? Varint, byte[]? Bytes);

    private static class ProtobufReader
    {
        public static IReadOnlyList<ProtobufField> ReadFields(ReadOnlySpan<byte> payload)
        {
            var fields = new List<ProtobufField>();
            var offset = 0;
            while (offset < payload.Length)
            {
                var tag = ReadVarint(payload, ref offset);
                var number = checked((int)(tag >> 3));
                var wireType = (int)(tag & 7);
                if (number <= 0)
                {
                    throw new InvalidOperationException("Invalid protobuf field number.");
                }

                switch (wireType)
                {
                    case 0:
                        fields.Add(new ProtobufField(number, ReadVarint(payload, ref offset), null));
                        break;
                    case 1:
                        Skip(payload, ref offset, 8);
                        break;
                    case 2:
                    {
                        var length = checked((int)ReadVarint(payload, ref offset));
                        if (length < 0 || offset + length > payload.Length)
                        {
                            throw new InvalidOperationException("Invalid protobuf length.");
                        }

                        fields.Add(new ProtobufField(number, null, payload.Slice(offset, length).ToArray()));
                        offset += length;
                        break;
                    }
                    case 5:
                        Skip(payload, ref offset, 4);
                        break;
                    default:
                        throw new InvalidOperationException("Unsupported protobuf wire type.");
                }
            }

            return fields;
        }

        private static long ReadVarint(ReadOnlySpan<byte> payload, ref int offset)
        {
            ulong value = 0;
            var shift = 0;
            while (offset < payload.Length && shift < 64)
            {
                var current = payload[offset++];
                value |= (ulong)(current & 0x7F) << shift;
                if ((current & 0x80) == 0)
                {
                    return checked((long)value);
                }

                shift += 7;
            }

            throw new InvalidOperationException("Invalid protobuf varint.");
        }

        private static void Skip(ReadOnlySpan<byte> payload, ref int offset, int count)
        {
            if (count < 0 || offset + count > payload.Length)
            {
                throw new InvalidOperationException("Invalid protobuf fixed-width field.");
            }

            offset += count;
        }
    }
}
