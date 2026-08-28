namespace CodexUsageWidget.Services;

public static class OfficialRefreshPolicy
{
    public static bool ShouldReadAutomatically(bool userActive)
    {
        return !userActive;
    }
}
