using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WindowPlacementManager.Models;

public class WindowConfig : INotifyPropertyChanged
{
    bool _isEnabled = true;
    string _processName = string.Empty;
    string _executablePath = string.Empty;
    string _windowTitleHint = string.Empty;
    bool _controlPosition = true;
    int _targetX;
    int _targetY;
    bool _controlSize = true;
    int _targetWidth;
    int _targetHeight;
    bool _launchAsAdmin = false;
    bool _autoRelaunchEnabled = false;

    public bool LaunchAsAdmin { get => _launchAsAdmin; set => SetField(ref _launchAsAdmin, value); }
    public bool IsEnabled { get => _isEnabled; set => SetField(ref _isEnabled, value); }
    public string ProcessName { get => _processName; set => SetField(ref _processName, value); }
    public string ExecutablePath { get => _executablePath; set => SetField(ref _executablePath, value); }
    public string WindowTitleHint { get => _windowTitleHint; set => SetField(ref _windowTitleHint, value); }
    public bool ControlPosition { get => _controlPosition; set => SetField(ref _controlPosition, value); }
    public int TargetX { get => _targetX; set => SetField(ref _targetX, value); }
    public int TargetY { get => _targetY; set => SetField(ref _targetY, value); }
    public bool ControlSize { get => _controlSize; set => SetField(ref _controlSize, value); }
    public int TargetWidth { get => _targetWidth; set => SetField(ref _targetWidth, value); }
    public int TargetHeight { get => _targetHeight; set => SetField(ref _targetHeight, value); }
    public bool AutoRelaunchEnabled { get => _autoRelaunchEnabled; set => SetField(ref _autoRelaunchEnabled, value); }

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