using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using CodexUsageWidget.Models;

namespace CodexUsageWidget.Services;

public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
        PropertyNameCaseInsensitive = true
    };

    private readonly string directory;

    public SettingsStore(string? directory = null)
    {
        this.directory = directory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CodexUsageWidget");
    }

    private string SettingsPath => Path.Combine(directory, "settings.json");

    public WidgetSettings Load()
    {
        if (!File.Exists(SettingsPath))
        {
            return new WidgetSettings();
        }

        try
        {
            var json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize<WidgetSettings>(json, SerializerOptions);
            return Normalize(settings ?? new WidgetSettings());
        }
        catch (JsonException)
        {
            return new WidgetSettings();
        }
        catch (IOException)
        {
            return new WidgetSettings();
        }
        catch (UnauthorizedAccessException)
        {
            return new WidgetSettings();
        }
    }

    public void Save(WidgetSettings settings)
    {
        var normalized = Normalize(settings);
        Directory.CreateDirectory(directory);
        var tempPath = Path.Combine(directory, $"settings.{Guid.NewGuid():N}.tmp");

        try
        {
            File.WriteAllText(tempPath, JsonSerializer.Serialize(normalized, SerializerOptions));
            File.Move(tempPath, SettingsPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    public static WidgetSettings Normalize(WidgetSettings settings)
    {
        var defaults = new WidgetSettings();
        var opacity = double.IsFinite(settings.Opacity)
            ? Math.Clamp(settings.Opacity, 0.45, 1.0)
            : defaults.Opacity;

        return settings with
        {
            WeeklyBudgetTokens = settings.WeeklyBudgetConfigured && settings.WeeklyBudgetTokens > 0
                ? settings.WeeklyBudgetTokens
                : 0,
            WeeklyBudgetConfigured = settings.WeeklyBudgetConfigured && settings.WeeklyBudgetTokens > 0,
            RefreshSeconds = settings.RefreshSeconds is >= 10 and <= 600
                ? settings.RefreshSeconds
                : defaults.RefreshSeconds,
            Opacity = opacity,
            Left = double.IsFinite(settings.Left) ? settings.Left : double.NaN,
            Top = double.IsFinite(settings.Top) ? settings.Top : double.NaN
        };
    }
}
