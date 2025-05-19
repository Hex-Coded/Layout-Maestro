
namespace WindowPlacementManager.Models;

public class Profile
{
    public string Name { get; set; } = "Default Profile";
    public List<WindowConfig> WindowConfigs { get; set; } = new List<WindowConfig>();

    public Profile() { }

    public Profile(string name) => Name = name;

    public WindowConfig AddWindowConfig(WindowConfig config = null)
    {
        var newConfig = config ?? new WindowConfig();
        WindowConfigs.Add(newConfig);
        return newConfig;
    }

    public void RemoveWindowConfig(WindowConfig config) => WindowConfigs.Remove(config);
}
