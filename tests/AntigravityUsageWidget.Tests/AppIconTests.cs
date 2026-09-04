namespace AntigravityUsageWidget.Tests;

public sealed class AppIconTests
{
    [Fact]
    public void UsesFloatingBallIconFile()
    {
        Assert.Equal("悬浮球.ico", AppIcon.FileName);
        Assert.Equal(
            Path.Combine("publish", "悬浮球.ico"),
            AppIcon.GetPath("publish"));
    }

    [Fact]
    public void FloatingBallIconContainsAValidPngImage()
    {
        var iconPath = AppIcon.GetPath(AppContext.BaseDirectory);
        var bytes = File.ReadAllBytes(iconPath);

        Assert.True(bytes.Length >= 30);
        Assert.Equal(0, BitConverter.ToUInt16(bytes, 0));
        Assert.Equal(1, BitConverter.ToUInt16(bytes, 2));

        var imageOffset = BitConverter.ToUInt32(bytes, 18);
        Assert.True(imageOffset + 8 <= bytes.Length);
        Assert.Equal(
            new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A },
            bytes[(int)imageOffset..((int)imageOffset + 8)]);
    }
}
