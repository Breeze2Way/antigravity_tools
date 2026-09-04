using System.Diagnostics;
using System.Management;
using System.Text.RegularExpressions;

namespace AntigravityUsageWidget.Data;

public sealed record AntigravityServerEndpoint(
    IReadOnlyList<Uri> BaseUris,
    string CsrfToken);

public static class AntigravityLanguageServerDiscovery
{
    private static readonly Regex TokenRegex = new(
        @"--csrf_token(?:=|\s+)(?<token>[^\s]+)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private static readonly Regex AntigravityMarkerRegex = new(
        @"(?:^|[\\/\s=_-])antigravity(?:$|[\\/\s._-])",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public static AntigravityServerEndpoint? Find()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT ProcessId, Name, CommandLine FROM Win32_Process WHERE Name = 'language_server.exe'");
            foreach (ManagementObject process in searcher.Get())
            {
                var commandLine = process["CommandLine"] as string;
                if (!TryParseCommandLine(commandLine, out var csrfToken) ||
                    !TryReadProcessId(process, out var processId))
                {
                    continue;
                }

                var ports = ParseListeningPorts(ReadNetstat(), processId);
                if (ports.Count == 0)
                {
                    continue;
                }

                var uris = ports
                    .SelectMany(port => new[]
                    {
                        new Uri($"https://127.0.0.1:{port}/"),
                        new Uri($"http://127.0.0.1:{port}/")
                    })
                    .ToArray();
                return new AntigravityServerEndpoint(uris, csrfToken);
            }
        }
        catch (ManagementException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }

        return null;
    }

    public static bool TryParseCommandLine(string? commandLine, out string csrfToken)
    {
        csrfToken = string.Empty;
        if (string.IsNullOrWhiteSpace(commandLine) ||
            !AntigravityMarkerRegex.IsMatch(commandLine))
        {
            return false;
        }

        var match = TokenRegex.Match(commandLine);
        if (!match.Success)
        {
            return false;
        }

        csrfToken = match.Groups["token"].Value;
        return csrfToken.Length > 0;
    }

    public static IReadOnlyList<int> ParseListeningPorts(string? netstatOutput, int processId)
    {
        if (string.IsNullOrWhiteSpace(netstatOutput))
        {
            return [];
        }

        var ports = new List<int>();
        foreach (var line in netstatOutput.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 5 ||
                !parts[0].Equals("TCP", StringComparison.OrdinalIgnoreCase) ||
                !parts[^2].Equals("LISTENING", StringComparison.OrdinalIgnoreCase) ||
                !int.TryParse(parts[^1], out var ownerProcessId) ||
                ownerProcessId != processId)
            {
                continue;
            }

            var endpoint = parts[1];
            var separator = endpoint.LastIndexOf(':');
            if (separator >= 0 && int.TryParse(endpoint[(separator + 1)..], out var port))
            {
                ports.Add(port);
            }
        }

        return ports.Distinct().OrderBy(port => port).ToArray();
    }

    private static bool TryReadProcessId(ManagementObject process, out int processId)
    {
        processId = 0;
        return process["ProcessId"] is uint value && (processId = (int)value) > 0;
    }

    private static string ReadNetstat()
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "netstat.exe",
                Arguments = "-ano -p tcp",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };
        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit(2_000);
        return output;
    }
}
