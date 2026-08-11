using System.IO;

namespace CodexUsageWidget.Data;

public sealed record CodexDataPaths(string StateDatabasePath, string SessionsDirectory)
{
    public static CodexDataPaths ForCurrentUser()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var codexRoot = Path.Combine(userProfile, ".codex");
        return new CodexDataPaths(
            Path.Combine(codexRoot, "state_5.sqlite"),
            Path.Combine(codexRoot, "sessions"));
    }
}
