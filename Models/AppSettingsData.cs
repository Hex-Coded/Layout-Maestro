using WindowPositioner.Services;

namespace WindowPositioner.Models
{
    public class AppSettingsData
    {
        public List<Profile> Profiles { get; set; } = new List<Profile>();
        public string ActiveProfileName { get; set; } = string.Empty;
        public StartupType StartupOption { get; set; } = StartupType.None;
    }
}
