using WindowPlacementManager.Services;

namespace WindowPlacementManager.Models;

public class AppSettingsData
{
    public List<Profile> Profiles { get; set; } = new List<Profile>();
    public string ActiveProfileName { get; set; } = string.Empty;
    public StartupType StartupOption { get; set; } = StartupType.None;
    public bool DisableProgramActivity { get; set; } = true;
    public int MonitorIntervalMs { get; internal set; }
    public int DelayBetweenActionsMs { get; internal set; }
    public bool BringToForegroundOnTest { get; internal set; }
}