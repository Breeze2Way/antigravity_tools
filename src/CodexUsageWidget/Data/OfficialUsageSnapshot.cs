namespace CodexUsageWidget.Data;

public sealed record OfficialUsageSnapshot(
    double? RemainingPercent,
    TimeSpan? ResetAfter);
