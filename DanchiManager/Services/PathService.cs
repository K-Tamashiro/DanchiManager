using System.IO;
using System.Text.Json;
using DanchiManager.Models;

namespace DanchiManager.Services;

public static class PathService
{
    public static string AppDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "KINRIN");

    public static string DefaultDbPath => Path.Combine(AppDir, "data", "danchi.db");

    public static string SettingsPath => Path.Combine(AppDir, "danchi.settings.json");

    public static AppSettings LoadSettings()
    {
        Directory.CreateDirectory(AppDir);
        if (!File.Exists(SettingsPath))
            return new AppSettings { DatabasePath = DefaultDbPath };

        try
        {
            var json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings { DatabasePath = DefaultDbPath };
        }
        catch
        {
            return new AppSettings { DatabasePath = DefaultDbPath };
        }
    }

    public static void SaveSettings(AppSettings settings)
    {
        Directory.CreateDirectory(AppDir);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
    }

    public static string ResolveDbPath(AppSettings settings)
    {
        if (settings.UseCustomPath && !string.IsNullOrWhiteSpace(settings.DatabasePath))
            return settings.DatabasePath;
        return DefaultDbPath;
    }
}
