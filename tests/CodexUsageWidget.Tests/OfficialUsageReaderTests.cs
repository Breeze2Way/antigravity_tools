namespace CodexUsageWidget.Tests;

public sealed class OfficialUsageReaderTests
{
    [Fact]
    public void RetriesUiReadUntilAValueIsAvailable()
    {
        var attempts = 0;
        var waits = 0;

        var result = OfficialUsageReader.WaitUntil(
            () => ++attempts >= 3,
            maxAttempts: 4,
            wait: () => waits++);

        Assert.True(result);
        Assert.Equal(3, attempts);
        Assert.Equal(2, waits);
    }

    [Theory]
    [InlineData("54%", 54)]
    [InlineData("  37.5% ", 37.5)]
    public void ParsesOfficialPercentage(string text, double expected)
    {
        Assert.True(OfficialUsageReader.TryParsePercentage(text, out var percentage));
        Assert.Equal(expected, percentage, precision: 6);
    }

    [Theory]
    [InlineData("剩余 54%")]
    [InlineData("54")]
    [InlineData("101%")]
    [InlineData("")]
    public void RejectsNonStandaloneOrInvalidPercentage(string text)
    {
        Assert.False(OfficialUsageReader.TryParsePercentage(text, out _));
    }
}
