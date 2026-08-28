namespace CodexUsageWidget.Tests;

public sealed class SettingsStoreTests
{
    [Fact]
    public void LoadsDefaultsWhenSettingsFileDoesNotExist()
    {
        using var temp = new TemporaryDirectory();
        var settings = new SettingsStore(temp.Path).Load();

        Assert.Equal(0, settings.WeeklyBudgetTokens);
        Assert.Equal(120, settings.RefreshSeconds);
        Assert.Equal(0.92, settings.Opacity, precision: 6);
        Assert.True(settings.Topmost);
        Assert.False(settings.AutoStart);
        Assert.True(double.IsNaN(settings.Left));
        Assert.True(double.IsNaN(settings.Top));
        Assert.Equal("#58B7E8", settings.WeeklyRingColor);
        Assert.Equal("#8BDCF5", settings.WeeklyRingGradientColor);
        Assert.False(settings.WeeklyRingGradientEnabled);
    }

    [Fact]
    public void SavesAndLoadsValidSettings()
    {
        using var temp = new TemporaryDirectory();
        var store = new SettingsStore(temp.Path);
        var expected = new WidgetSettings(250_000_000, 45, 0.75, false, true, 123, 456)
        {
            WeeklyBudgetConfigured = true,
            WeeklyRingColor = "#123456",
            WeeklyRingGradientColor = "#ABCDEF",
            WeeklyRingGradientEnabled = true
        };

        store.Save(expected);

        Assert.Equal(expected, store.Load());
        Assert.True(File.Exists(Path.Combine(temp.Path, "settings.json")));
    }

    [Fact]
    public void NormalizesInvalidValuesWithoutThrowing()
    {
        using var temp = new TemporaryDirectory();
        var store = new SettingsStore(temp.Path);

        store.Save(new WidgetSettings(0, 5, 2, true, false, double.PositiveInfinity, double.NaN));

        var loaded = store.Load();
        Assert.Equal(0, loaded.WeeklyBudgetTokens);
        Assert.Equal(120, loaded.RefreshSeconds);
        Assert.Equal(1.0, loaded.Opacity, precision: 6);
        Assert.True(double.IsNaN(loaded.Left));
        Assert.True(double.IsNaN(loaded.Top));
        Assert.Equal("#58B7E8", loaded.WeeklyRingColor);
        Assert.Equal("#8BDCF5", loaded.WeeklyRingGradientColor);
    }

    [Fact]
    public void NormalizesRingColorsToCanonicalHex()
    {
        var settings = SettingsStore.Normalize(new WidgetSettings
        {
            WeeklyRingColor = " 58b7e8 ",
            WeeklyRingGradientColor = "#abcdef",
            WeeklyRingGradientEnabled = true
        });

        Assert.Equal("#58B7E8", settings.WeeklyRingColor);
        Assert.Equal("#ABCDEF", settings.WeeklyRingGradientColor);
        Assert.True(settings.WeeklyRingGradientEnabled);
    }

    [Fact]
    public void RecoversDefaultsFromMalformedSettingsFile()
    {
        using var temp = new TemporaryDirectory();
        File.WriteAllText(Path.Combine(temp.Path, "settings.json"), "{not-json");

        var settings = new SettingsStore(temp.Path).Load();

        Assert.Equal(new WidgetSettings(), settings);
    }

    [Fact]
    public void TreatsLegacyImplicitBudgetAsUnconfigured()
    {
        using var temp = new TemporaryDirectory();
        File.WriteAllText(
            Path.Combine(temp.Path, "settings.json"),
            "{\"WeeklyBudgetTokens\":100000000,\"RefreshSeconds\":30,\"Opacity\":0.92,\"Topmost\":true,\"AutoStart\":false}");

        var settings = new SettingsStore(temp.Path).Load();

        Assert.False(settings.WeeklyBudgetConfigured);
        Assert.Equal(0, settings.WeeklyBudgetTokens);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory() => Path = Directory.CreateTempSubdirectory("CodexUsageWidgetSettingsTests").FullName;

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
