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
                    : uri.AbsolutePath.EndsWith("GetUserStatus", StringComparison.Ordinal)
                        ? "{}"
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
        Assert.Equal(3, calls.Count);
    }

    [Fact]
    public void EnrichesTheSummaryWithTheSelectedModelFromUserStatus()
    {
        var calls = new Collection<string>();
        var transport = new RecordingTransport((uri, _, _) =>
        {
            calls.Add(uri.AbsolutePath);
            return uri.AbsolutePath switch
            {
                "/exa.language_server_pb.LanguageServerService/GetUnleashData" => "{}",
                "/exa.language_server_pb.LanguageServerService/RetrieveUserQuotaSummary" =>
                    "{\"response\":{\"groups\":[{\"displayName\":\"Gemini Models\",\"buckets\":[{\"displayName\":\"5h\",\"window\":\"5h\",\"remainingFraction\":0.7}]},{\"displayName\":\"Claude and GPT models\",\"buckets\":[{\"displayName\":\"5h\",\"window\":\"5h\",\"remainingFraction\":0.1}]}]}}",
                "/exa.language_server_pb.LanguageServerService/GetUserStatus" =>
                    "{\"userStatus\":{\"cascadeModelConfigData\":{\"defaultOverrideModelConfig\":{\"modelOrAlias\":{\"model\":\"MODEL_PLACEHOLDER_M318\"}},\"clientModelConfigs\":[{\"label\":\"Gemini 3.8 Flash (High)\",\"modelOrAlias\":{\"model\":\"MODEL_PLACEHOLDER_M318\"},\"quotaInfo\":{\"remainingFraction\":0.7}}]}}}",
                _ => throw new InvalidOperationException("unexpected endpoint")
            };
        });
        var reader = new AntigravityStatusReader(
            () => new AntigravityServerEndpoint(
                [new Uri("https://127.0.0.1:55601")],
                "csrf"),
            transport);

        var snapshot = reader.ReadUsage();

        Assert.NotNull(snapshot);
        Assert.Equal("MODEL_PLACEHOLDER_M318", snapshot!.SelectedModelId);
        Assert.Equal("Gemini 3.8 Flash (High)", snapshot.SelectedModelLabel);
        Assert.Equal(3, calls.Count);
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
