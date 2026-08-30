namespace CodexUsageWidget.Services;

public static class OfficialRefreshPolicy
{
    public static bool ShouldReadOnManualRefresh => true;

    public static bool ShouldReadAutomatically(bool userActive)
    {
        return !userActive;
    }
}
