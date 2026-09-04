using System.IO;

namespace AntigravityUsageWidget.Services;

public static class AppIcon
{
    public const string FileName = "悬浮球.ico";

    public static string GetPath(string baseDirectory)
    {
        return Path.Combine(baseDirectory, FileName);
    }
}
