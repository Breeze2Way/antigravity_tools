namespace AntigravityUsageWidget.Data;

public enum AntigravityQuotaPeriod
{
    Unknown,
    Short,
    Weekly
}

public sealed record AntigravityQuotaRow(
    string Label,
    string? Group,
    double RemainingPercent,
    DateTimeOffset? ResetAt,
    AntigravityQuotaPeriod Period)
{
    public string? ModelId { get; init; }
}

public sealed record AntigravityQuotaSnapshot(
    string? PlanName,
    IReadOnlyList<AntigravityQuotaRow> Rows,
    DateTimeOffset RetrievedAt)
{
    public string? SelectedModelId { get; init; }
    public string? SelectedModelLabel { get; init; }
}
