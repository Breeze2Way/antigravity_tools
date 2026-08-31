namespace CodexUsageWidget.Services;

public static class OfficialRefreshPolicy
{
    public static readonly TimeSpan AutomaticReadCooldown = TimeSpan.FromMinutes(10);

    public static bool ShouldReadOnManualRefresh => true;

    public static bool ShouldReadAutomatically(bool userActive)
    {
        return !userActive;
    }

    public static bool ShouldReadAutomatically(
        bool userActive,
        DateTimeOffset now,
        DateTimeOffset? lastReadAt)
    {
        if (userActive)
        {
            return false;
        }

        return !lastReadAt.HasValue || now - lastReadAt.Value >= AutomaticReadCooldown;
    }
}
