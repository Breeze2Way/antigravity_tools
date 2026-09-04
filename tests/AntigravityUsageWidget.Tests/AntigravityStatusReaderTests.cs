using System.Collections.ObjectModel;

namespace AntigravityUsageWidget.Tests;

public sealed class AntigravityStatusReaderTests
{
    [Fact]
    public void FallsBackFromSummaryToUserStatusToModelConfigs()
    {
        var calls = new Collection<string>();
        var transport = new RecordingTransport((uri, _, _) =>
        {
            calls.Add(uri.AbsolutePath);
            return uri.AbsolutePath switch
            {
                "/exa.language_server_pb.LanguageServerService/GetUnleashData" => "{}",
                "/exa.language_server_pb.LanguageServerService/RetrieveUserQuotaSummary" => "{}",
                "/exa.language_server_pb.LanguageServerService/GetUserStatus" => "{}",
                "/exa.language_server_pb.LanguageServerService/GetCommandModelConfigs" =>
                    "{\"clientModelConfigs\":[{\"label\":\"Gemini\",\"quotaInfo\":{\"remainingFraction\":0.5}}]}",
                _ => null
            };
        });
        var reader = new AntigravityStatusReader(
            () => new AntigravityServerEndpoint(
                [new Uri("https://127.0.0.1:55601")],
                "test-token"),
            transport);

        var snapshot = reader.ReadUsage();

        Assert.NotNull(snapshot);
        Assert.Single(snapshot!.Rows);
        Assert.Equal(
            [
                "/exa.language_server_pb.LanguageServerService/GetUnleashData",
                "/exa.language_server_pb.LanguageServerService/RetrieveUserQuotaSummary",
                "/exa.language_server_pb.LanguageServerService/GetUserStatus",
                "/exa.language_server_pb.LanguageServerService/GetCommandModelConfigs"
            ],
            calls);
        Assert.All(transport.Requests, request => Assert.Equal("test-token", request.CsrfToken));
    }

    [Fact]
    public void StopsAtFirstUsableQuotaEndpoint()
    {
        var calls = new Collection<string>();
        var transport = new RecordingTransport((uri, _, _) =>
        {
            calls.Add(uri.AbsolutePath);
            return uri.AbsolutePath.EndsWith("GetUnleashData", StringComparison.Ordinal)
                ? "{}"
                : uri.AbsolutePath.EndsWith("RetrieveUserQuotaSummary", StringComparison.Ordinal)
                    ? "{\"response\":{\"groups\":[{\"displayName\":\"Gemini\",\"buckets\":[{\"displayName\":\"Weekly Limit Remaining\",\"window\":\"weekly\",\"remainingFraction\":0.75}]}]}}"
                    : throw new InvalidOperationException("unexpected endpoint");
        });
        var reader = new AntigravityStatusReader(
            () => new AntigravityServerEndpoint(
                [new Uri("https://127.0.0.1:55601")],
                "csrf"),
            transport);

        var snapshot = reader.ReadUsage();

        Assert.NotNull(snapshot);
        Assert.Equal(75, snapshot!.Rows[0].RemainingPercent, precision: 6);
        Assert.Equal(2, calls.Count);
    }

    private sealed class RecordingTransport : IAntigravityRpcTransport
    {
        private readonly Func<Uri, string, string, string?> handler;

        public RecordingTransport(Func<Uri, string, string, string?> handler) => this.handler = handler;

        public List<Request> Requests { get; } = [];

        public string? Post(Uri endpoint, string csrfToken, string body)
        {
            Requests.Add(new Request(endpoint, csrfToken, body));
            return handler(endpoint, csrfToken, body);
        }

        public sealed record Request(Uri Endpoint, string CsrfToken, string Body);
    }
}
