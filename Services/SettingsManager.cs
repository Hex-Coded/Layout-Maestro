using System.Text.Json;
using WindowPlacementManager.Models;

namespace WindowPlacementManager.Services;

public class SettingsManager
{
    static readonly string SettingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WindowPlacementManager", "settings.json");

    public AppSettingsData LoadSettings()
    {
        try
        {
            if(File.Exists(SettingsFilePath))
            {
                string json = File.ReadAllText(SettingsFilePath);
                return JsonSerializer.Deserialize<AppSettingsData>(json) ?? CreateDefaultSettings();
            }
        }
        catch(Exception ex)
        {
            Console.WriteLine($"Error loading settings: {ex.Message}");
        }
        return CreateDefaultSettings();
    }

    public void SaveSettings(AppSettingsData settings)
    {
        try
        {
            string directory = Path.GetDirectoryName(SettingsFilePath);
            if(!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFilePath, json);
        }
        catch(Exception ex)
        {
            Console.WriteLine($"Error saving settings: {ex.Message}");
        }
    }

    AppSettingsData CreateDefaultSettings()
    {
        var defaultSettings = new AppSettingsData();
        var defaultProfile = new Profile("Default");
        defaultSettings.Profiles.Add(defaultProfile);
        defaultSettings.ActiveProfileName = defaultProfile.Name;
        return defaultSettings;
    }
}
