using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WindowPlacementManager.Models;

public class WindowConfig : INotifyPropertyChanged
{
    bool isEnabled = true;
    string processName = string.Empty;
    string executablePath = string.Empty;
    string windowTitleHint = string.Empty;
    bool controlPosition = true;
    int targetX;
    int targetY;
    bool controlSize = true;
    int targetWidth;
    int targetHeight;
    bool launchAsAdmin = false;
    bool autoRelaunchEnabled = false;

    public bool LaunchAsAdmin { get => launchAsAdmin; set => SetField(ref launchAsAdmin, value); }
    public bool IsEnabled { get => isEnabled; set => SetField(ref isEnabled, value); }
    public string ProcessName { get => processName; set => SetField(ref processName, value); }
    public string ExecutablePath { get => executablePath; set => SetField(ref executablePath, value); }
    public string WindowTitleHint { get => windowTitleHint; set => SetField(ref windowTitleHint, value); }
    public bool ControlPosition { get => controlPosition; set => SetField(ref controlPosition, value); }
    public int TargetX { get => targetX; set => SetField(ref targetX, value); }
    public int TargetY { get => targetY; set => SetField(ref targetY, value); }
    public bool ControlSize { get => controlSize; set => SetField(ref controlSize, value); }
    public int TargetWidth { get => targetWidth; set => SetField(ref targetWidth, value); }
    public int TargetHeight { get => targetHeight; set => SetField(ref targetHeight, value); }
    public bool AutoRelaunchEnabled { get => autoRelaunchEnabled; set => SetField(ref autoRelaunchEnabled, value); }

    public WindowConfig() { }

    public WindowConfig Clone() => (WindowConfig)this.MemberwiseClone();

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    protected bool SetField<TField>(ref TField field, TField value, [CallerMemberName] string propertyName = null)
    {
        if(EqualityComparer<TField>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
