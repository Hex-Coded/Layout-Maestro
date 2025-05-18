using System.Diagnostics;
using WindowPositioner;
using WindowPositioner.Models;
using WindowPositioner.Services;
using Timer = System.Windows.Forms.Timer;

public class WindowMonitorService : IDisposable
{
    private readonly SettingsManager _settingsManager;
    private AppSettingsData _currentSettings;
    private Profile _activeProfile;
    private readonly HashSet<int> _handledProcessIdsThisSession;
    private Timer _timer;

    public WindowMonitorService(SettingsManager settingsManager)
    {
        _settingsManager = settingsManager;
        _handledProcessIdsThisSession = new HashSet<int>();
        LoadAndApplySettings();
        InitializeTimer();
    }

    public void LoadAndApplySettings()
    {
        _currentSettings = _settingsManager.LoadSettings();
        _activeProfile = _currentSettings.Profiles.FirstOrDefault(p => p.Name == _currentSettings.ActiveProfileName);
        _handledProcessIdsThisSession.Clear();
    }

    private void InitializeTimer()
    {
        _timer = new Timer();
        _timer.Interval = 200;
        _timer.Tick += Timer_Tick;
        _timer.Start();
    }

    private void Timer_Tick(object sender, EventArgs e)
    {
        ProcessWindows();
    }

    public void ProcessWindows()
    {
        if(_activeProfile == null || !_activeProfile.WindowConfigs.Any())
        {
            return;
        }

        foreach(var config in _activeProfile.WindowConfigs.Where(c => c.IsEnabled))
        {
            if(string.IsNullOrWhiteSpace(config.ProcessName)) continue;

            try
            {
                Process[] processes = Process.GetProcessesByName(config.ProcessName);
                foreach(var process in processes)
                {
                    if(_handledProcessIdsThisSession.Contains(process.Id))
                    {
                        try { Process.GetProcessById(process.Id); }
                        catch(ArgumentException) { _handledProcessIdsThisSession.Remove(process.Id); continue; }
                        continue;
                    }

                    IntPtr hWnd = process.MainWindowHandle;
                    if(hWnd == IntPtr.Zero) continue;

                    if(!string.IsNullOrWhiteSpace(config.WindowTitleHint))
                    {
                        string currentTitle = NativeMethods.GetWindowTitle(hWnd);
                        if(!currentTitle.ToLower().Contains(config.WindowTitleHint.ToLower()))
                        {
                            continue;
                        }
                    }

                    if(!NativeMethods.GetWindowRect(hWnd, out RECT currentRect)) continue;

                    int targetX = currentRect.Left;
                    int targetY = currentRect.Top;
                    int targetWidth = currentRect.Width;
                    int targetHeight = currentRect.Height;

                    if(config.ControlPosition)
                    {
                        targetX = config.TargetX;
                        targetY = config.TargetY;
                    }

                    if(config.ControlSize)
                    {
                        targetWidth = config.TargetWidth;
                        targetHeight = config.TargetHeight;
                    }

                    if(targetWidth <= 0 || targetHeight <= 0) continue;


                    NativeMethods.MoveWindow(hWnd, targetX, targetY, targetWidth, targetHeight, true);
                    _handledProcessIdsThisSession.Add(process.Id);
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine($"Error processing window for '{config.ProcessName}': {ex.Message}");
            }
        }
    }

    public void ForceProfileReload()
    {
        LoadAndApplySettings();
    }

    public void Dispose()
    {
        _timer?.Stop();
        _timer?.Dispose();
    }
}