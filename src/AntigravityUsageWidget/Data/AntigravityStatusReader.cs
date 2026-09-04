using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace AntigravityUsageWidget.Data;

public interface IAntigravityRpcTransport
{
    string? Post(Uri endpoint, string csrfToken, string body);
}

public sealed class AntigravityStatusReader
{
    private const string ServicePrefix = "/exa.language_server_pb.LanguageServerService/";
    private const string RequestBody = "{\"metadata\":{\"ideName\":\"antigravity\",\"extensionName\":\"antigravity\",\"locale\":\"en\"}}";
    private readonly Func<AntigravityServerEndpoint?> discover;
    private readonly IAntigravityRpcTransport transport;

    public AntigravityStatusReader(
        Func<AntigravityServerEndpoint?>? discover = null,
        IAntigravityRpcTransport? transport = null)
    {
        this.discover = discover ?? AntigravityLanguageServerDiscovery.Find;
        this.transport = transport ?? new HttpAntigravityRpcTransport();
    }

    public AntigravityQuotaSnapshot? ReadUsage()
    {
        var server = discover();
        if (server is null)
        {
            return null;
        }

        foreach (var baseUri in server.BaseUris)
        {
            if (Post(baseUri, "GetUnleashData", server.CsrfToken) is null)
            {
                continue;
            }

            foreach (var method in new[]
                     {
                         "RetrieveUserQuotaSummary",
                         "GetUserStatus",
                         "GetCommandModelConfigs"
                     })
            {
                var response = Post(baseUri, method, server.CsrfToken);
                var snapshot = AntigravityQuotaParser.Parse(response);
                if (snapshot is not null)
                {
                    return snapshot;
                }
            }
        }

        return null;
    }

    private string? Post(Uri baseUri, string method, string csrfToken)
    {
        var endpoint = new Uri(baseUri, ServicePrefix + method);
        return transport.Post(endpoint, csrfToken, RequestBody);
    }

    private sealed class HttpAntigravityRpcTransport : IAntigravityRpcTransport
    {
        private static readonly HttpClient Client = CreateClient();

        public string? Post(Uri endpoint, string csrfToken, string body)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                };
                request.Headers.TryAddWithoutValidation("Connect-Protocol-Version", "1");
                request.Headers.TryAddWithoutValidation("X-Codeium-Csrf-Token", csrfToken);
                using var response = Client.Send(request);
                return response.IsSuccessStatusCode
                    ? response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
                    : null;
            }
            catch (HttpRequestException)
            {
                return null;
            }
            catch (TaskCanceledException)
            {
                return null;
            }
        }

        private static HttpClient CreateClient()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (request, _, _, _) =>
                    request.RequestUri?.IsLoopback == true
            };
            return new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(3)
            };
        }
    }
}
