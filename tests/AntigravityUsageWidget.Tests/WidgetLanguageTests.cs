namespace AntigravityUsageWidget.Tests;

public sealed class WidgetLanguageTests
{
    [Theory]
    [InlineData(null, "zh-CN")]
    [InlineData("", "zh-CN")]
    [InlineData("zh-CN", "zh-CN")]
    [InlineData("en-US", "en-US")]
    [InlineData("EN-us", "en-US")]
    [InlineData("fr-FR", "zh-CN")]
    public void NormalizesSupportedLanguage(string? value, string expected)
    {
        Assert.Equal(expected, WidgetLanguage.Normalize(value));
    }

    [Fact]
    public void IdentifiesEnglishLanguage()
    {
        Assert.True(WidgetLanguage.IsEnglish("en-US"));
        Assert.False(WidgetLanguage.IsEnglish("zh-CN"));
    }
}
