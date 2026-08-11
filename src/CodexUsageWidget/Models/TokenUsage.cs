namespace CodexUsageWidget.Models;

public readonly record struct TokenUsage(
    long InputTokens,
    long CachedInputTokens,
    long CacheWriteInputTokens,
    long OutputTokens,
    long ReasoningOutputTokens,
    long TotalTokens)
{
    public static TokenUsage operator +(TokenUsage left, TokenUsage right) => new(
        checked(left.InputTokens + right.InputTokens),
        checked(left.CachedInputTokens + right.CachedInputTokens),
        checked(left.CacheWriteInputTokens + right.CacheWriteInputTokens),
        checked(left.OutputTokens + right.OutputTokens),
        checked(left.ReasoningOutputTokens + right.ReasoningOutputTokens),
        checked(left.TotalTokens + right.TotalTokens));

    public static TokenUsage operator -(TokenUsage left, TokenUsage right) => new(
        Math.Max(0, left.InputTokens - right.InputTokens),
        Math.Max(0, left.CachedInputTokens - right.CachedInputTokens),
        Math.Max(0, left.CacheWriteInputTokens - right.CacheWriteInputTokens),
        Math.Max(0, left.OutputTokens - right.OutputTokens),
        Math.Max(0, left.ReasoningOutputTokens - right.ReasoningOutputTokens),
        Math.Max(0, left.TotalTokens - right.TotalTokens));

    public static TokenUsage Zero => default;
}
