namespace AntigravityUsageWidget.Tests;

public sealed class AntigravityLanguageServerDiscoveryTests
{
    [Fact]
    public void ParsesAntigravityTokenAndRejectsOtherLanguageServers()
    {
        const string commandLine = "language_server.exe --app_data_dir antigravity --csrf_token csrf-value --extension_server_port 55602";

        Assert.True(AntigravityLanguageServerDiscovery.TryParseCommandLine(commandLine, out var token));
        Assert.Equal("csrf-value", token);
        Assert.False(AntigravityLanguageServerDiscovery.TryParseCommandLine(
            "language_server.exe --app_data_dir windsurf --csrf_token other",
            out _));
    }

    [Fact]
    public void ParsesOnlyPortsOwnedByTheSelectedProcess()
    {
        const string netstat = """
          TCP    127.0.0.1:55601        0.0.0.0:0              LISTENING       31732
          TCP    127.0.0.1:55602        0.0.0.0:0              LISTENING       31732
          TCP    127.0.0.1:55603        0.0.0.0:0              LISTENING       11111
        """;

        var ports = AntigravityLanguageServerDiscovery.ParseListeningPorts(netstat, processId: 31732);

        Assert.Equal([55601, 55602], ports);
    }
}
