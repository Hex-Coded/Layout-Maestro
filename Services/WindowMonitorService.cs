using System.Diagnostics;
using WindowPlacementManager.Models;
using WindowPlacementManager.Services;
using WindowPlacementManager;

public class WindowMonitorService : IDisposable
{
    readonly SettingsManager _settingsManager;
    AppSettingsData _currentSettings;
    Profile _activeProfile;
    readonly HashSet<int> _handledProcessIdsThisSession;
    System.Windows.Forms.Timer _timer;
    bool _isProgramActivityDisabled = true;


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
        _isProgramActivityDisabled = _currentSettings.DisableProgramActivity;
        _handledProcessIdsThisSession.Clear();
    }

    public void SetPositioningActive(bool isActive)
    {
        _isProgramActivityDisabled = !isActive;
        if(isActive)
        {
        }
    }

    void InitializeTimer()
    {
        _timer = new System.Windows.Forms.Timer();
        _timer.Interval = 200;
        _timer.Tick += Timer_Tick;
        _timer.Start();
    }

    void Timer_Tick(object sender, EventArgs e) => ProcessWindows();

    public void ProcessWindows()
    {
        if(_isProgramActivityDisabled)
        {
            return;
        }

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

    public void TestProfileLayout(Profile profileToTest)
    {
        if(profileToTest == null || !profileToTest.WindowConfigs.Any())
        {
            Debug.WriteLine("TestProfileLayout: Profile to test is null or has no configs.");
            return;
        }

        HashSet<int> handledProcessIdsThisTestRun = new HashSet<int>();

        foreach(var config in profileToTest.WindowConfigs.Where(c => c.IsEnabled))
        {
            if(string.IsNullOrWhiteSpace(config.ProcessName)) continue;

            try
            {
                Process[] processes = Process.GetProcessesByName(config.ProcessName);
                foreach(var process in processes)
                {
                    if(handledProcessIdsThisTestRun.Contains(process.Id))
                    {
                        try { Process.GetProcessById(process.Id); }
                        catch(ArgumentException) { handledProcessIdsThisTestRun.Remove(process.Id); continue; }
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

                    if(targetWidth <= 0 || targetHeight <= 0)
                    {
                        Debug.WriteLine($"TestProfileLayout: Skipping {config.ProcessName} (PID:{process.Id}) due to invalid target/current dimensions W:{targetWidth} H:{targetHeight}");
                        continue;
                    }

                    NativeMethods.MoveWindow(hWnd, targetX, targetY, targetWidth, targetHeight, true);
                    handledProcessIdsThisTestRun.Add(process.Id);
                    Debug.WriteLine($"TestProfileLayout: Moved {config.ProcessName} (PID: {process.Id}, hWnd: {hWnd}) to X:{targetX} Y:{targetY} W:{targetWidth} H:{targetHeight}");
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine($"TestProfileLayout: Error processing window for '{config.ProcessName}': {ex.Message}");
            }
        }
        Debug.WriteLine("TestProfileLayout: Test run completed.");
    }


    public void Dispose()
    {
        _timer?.Stop();
        _timer?.Dispose();
        _timer = null;
    }
}
