namespace CodexUsageWidget.Tests;

public sealed class ColorParserTests
{
    [Theory]
    [InlineData("#58B7E8", 88, 183, 232)]
    [InlineData("58b7e8", 88, 183, 232)]
    [InlineData("  #FFFFFF  ", 255, 255, 255)]
    public void ParsesSixDigitHexColor(string text, byte red, byte green, byte blue)
    {
        Assert.True(ColorParser.TryParseHex(text, out var color));
        Assert.Equal(new WaterBallColor(red, green, blue), color);
    }

    [Theory]
    [InlineData("")]
    [InlineData("#12345")]
    [InlineData("#1234567")]
    [InlineData("#GGGGGG")]
    public void RejectsInvalidHexColor(string text)
    {
        Assert.False(ColorParser.TryParseHex(text, out _));
    }

    [Fact]
    public void FormatsColorAsUppercaseHex()
    {
        Assert.Equal("#58B7E8", ColorParser.ToHex(new WaterBallColor(88, 183, 232)));
    }

    [Fact]
    public void ConvertsSelectedDrawingColor()
    {
        var selectedColor = System.Drawing.Color.FromArgb(88, 183, 232);

        Assert.Equal(
            new WaterBallColor(88, 183, 232),
            ColorParser.FromDrawingColor(selectedColor));
    }
}
