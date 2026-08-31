using System.Net.Http.Headers;
using System.Net.Http;
using System.IO;
using System.Text.Json;

namespace CodexUsageWidget.Data;

public sealed class OfficialUsageApiReader
{
    private const string UsageEndpoint = "https://chatgpt.com/backend-api/wham/usage";
    private static readonly HttpClient HttpClient = CreateHttpClient();

    public OfficialUsageSnapshot? ReadUsage()
    {
        try
        {
            if (!TryReadAuth(out var accessToken, out var accountId))
            {
                return null;
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, UsageEndpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.TryAddWithoutValidation("ChatGPT-Account-Id", accountId);
            request.Headers.TryAddWithoutValidation("OAI-App-Brand", "codex");
            request.Headers.TryAddWithoutValidation("originator", "Codex Desktop");
            using var response = HttpClient.Send(request);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return OfficialUsageApiParser.Parse(response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (TaskCanceledException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    private static HttpClient CreateHttpClient()
    {
        return new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
    }

    private static bool TryReadAuth(out string accessToken, out string accountId)
    {
        accessToken = string.Empty;
        accountId = string.Empty;
        var authPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".codex",
            "auth.json");
        if (!File.Exists(authPath))
        {
            return false;
        }

        using var document = JsonDocument.Parse(File.ReadAllText(authPath));
        if (!document.RootElement.TryGetProperty("tokens", out var tokens) ||
            tokens.ValueKind != JsonValueKind.Object ||
            !TryGetString(tokens, "access_token", out accessToken) ||
            !TryGetString(tokens, "account_id", out accountId))
        {
            accessToken = string.Empty;
            accountId = string.Empty;
            return false;
        }

        return true;
    }

    private static bool TryGetString(JsonElement parent, string propertyName, out string value)
    {
        value = string.Empty;
        if (!parent.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = property.GetString() ?? string.Empty;
        return value.Length > 0;
    }
}
