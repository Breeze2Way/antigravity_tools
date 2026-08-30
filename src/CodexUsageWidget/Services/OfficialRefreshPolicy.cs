namespace CodexUsageWidget.Services;

public static class OfficialRefreshPolicy
{
    public static bool ShouldReadOnManualRefresh => false;

    public static bool ShouldReadAutomatically(bool userActive)
    {
        return !userActive;
    }
}
