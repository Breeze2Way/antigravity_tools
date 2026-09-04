namespace AntigravityUsageWidget.Data;

public sealed record OfficialUsageSnapshot(
    double? RemainingPercent,
    TimeSpan? ResetAfter,
    double? FiveHourRemainingPercent = null,
    TimeSpan? FiveHourResetAfter = null);
