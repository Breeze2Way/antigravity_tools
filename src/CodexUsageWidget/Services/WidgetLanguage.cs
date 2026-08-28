namespace CodexUsageWidget.Services;

public static class WidgetLanguage
{
    public const string Chinese = "zh-CN";
    public const string English = "en-US";

    public static string Normalize(string? language)
    {
        return string.Equals(language, English, StringComparison.OrdinalIgnoreCase)
            ? English
            : Chinese;
    }

    public static bool IsEnglish(string? language)
    {
        return string.Equals(Normalize(language), English, StringComparison.Ordinal);
    }
}
