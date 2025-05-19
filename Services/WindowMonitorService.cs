using System.Diagnostics;
using WindowPlacementManager.Models;

namespace WindowPlacementManager.Services;

public class WindowMonitorService : IDisposable
{
    private readonly SettingsManager _settingsManager;
    private AppSettingsData _currentSettings;
    private Profile _activeProfile;
    private readonly HashSet<IntPtr> _handledWindowHandlesThisCycle;
    private System.Windows.Forms.Timer _timer;
    private bool _isProgramActivityDisabled = true;

    public WindowMonitorService(SettingsManager settingsManager)
    {
        _settingsManager = settingsManager;
        _handledWindowHandlesThisCycle = new HashSet<IntPtr>();
        LoadAndApplySettings();
        InitializeTimer();
    }

    public void LoadAndApplySettings()
    {
        _currentSettings = _settingsManager.LoadSettings();
        _activeProfile = _currentSettings.Profiles.FirstOrDefault(p => p.Name == _currentSettings.ActiveProfileName);
        _isProgramActivityDisabled = _currentSettings.DisableProgramActivity;
    }

    public void SetPositioningActive(bool isActive) => _isProgramActivityDisabled = !isActive;

    private void InitializeTimer()
    {
        _timer = new System.Windows.Forms.Timer { Interval = 300 };
        _timer.Tick += Timer_Tick;
        _timer.Start();
    }

    private void Timer_Tick(object sender, EventArgs e) => ProcessWindows();

    public void ProcessWindows()
    {
        if(_isProgramActivityDisabled || _activeProfile == null || !_activeProfile.WindowConfigs.Any()) return;

        _handledWindowHandlesThisCycle.Clear();

        foreach(var config in _activeProfile.WindowConfigs.Where(c => c.IsEnabled))
        {
            if(string.IsNullOrWhiteSpace(config.ProcessName)) continue;

            var foundWindow = WindowEnumerationService.FindMostSuitableWindow(config);
            if(foundWindow == null || foundWindow?.HWnd == IntPtr.Zero) continue;

            IntPtr hWnd = foundWindow.HWnd;
            if(_handledWindowHandlesThisCycle.Contains(hWnd)) continue;

            if(!Native.GetWindowRect(hWnd, out RECT currentRect)) continue;

            int targetX = config.ControlPosition ? config.TargetX : currentRect.Left;
            int targetY = config.ControlPosition ? config.TargetY : currentRect.Top;
            int targetWidth = config.ControlSize ? config.TargetWidth : currentRect.Width;
            int targetHeight = config.ControlSize ? config.TargetHeight : currentRect.Height;

            if(targetWidth <= 0 || targetHeight <= 0)
            {
                Debug.WriteLine($"ProcessWindows: Invalid target dims for {config.ProcessName} (hWnd:{hWnd}) W:{targetWidth} H:{targetHeight}");
                continue;
            }

            bool needsMove = config.ControlPosition && (currentRect.Left != targetX || currentRect.Top != targetY);
            bool needsResize = config.ControlSize && (currentRect.Width != targetWidth || currentRect.Height != targetHeight);

            if(needsMove || needsResize)
            {
                Native.MoveWindow(hWnd, targetX, targetY, targetWidth, targetHeight, true);
                Debug.WriteLine($"ProcessWindows: Applied to {config.ProcessName} (hWnd:{hWnd}) X:{targetX} Y:{targetY} W:{targetWidth} H:{targetHeight}");
            }
            _handledWindowHandlesThisCycle.Add(hWnd);
        }
    }

    public void TestProfileLayout(Profile profileToTest)
    {
        if(profileToTest == null || !profileToTest.WindowConfigs.Any())
        {
            Debug.WriteLine("TestProfileLayout: Profile null or no configs.");
            return;
        }

        HashSet<IntPtr> handledHWndsThisTest = new HashSet<IntPtr>();
        foreach(var config in profileToTest.WindowConfigs.Where(c => c.IsEnabled))
        {
            if(string.IsNullOrWhiteSpace(config.ProcessName)) continue;

            var foundWindow = WindowEnumerationService.FindMostSuitableWindow(config);
            if(foundWindow?.HWnd == IntPtr.Zero)
            {
                Debug.WriteLine($"TestProfileLayout: Window not found for '{config.ProcessName}' Hint:'{config.WindowTitleHint}'.");
                continue;
            }

            IntPtr hWnd = foundWindow.HWnd;
            if(handledHWndsThisTest.Contains(hWnd)) continue;

            if(!Native.GetWindowRect(hWnd, out RECT currentRect)) continue;

            int targetX = config.ControlPosition ? config.TargetX : currentRect.Left;
            int targetY = config.ControlPosition ? config.TargetY : currentRect.Top;
            int targetWidth = config.ControlSize ? config.TargetWidth : currentRect.Width;
            int targetHeight = config.ControlSize ? config.TargetHeight : currentRect.Height;

            if(targetWidth <= 0 || targetHeight <= 0)
            {
                Debug.WriteLine($"TestProfileLayout: Invalid target dims for {config.ProcessName} (hWnd:{hWnd}) W:{targetWidth} H:{targetHeight}");
                continue;
            }

            Native.MoveWindow(hWnd, targetX, targetY, targetWidth, targetHeight, true);
            handledHWndsThisTest.Add(hWnd);
            Debug.WriteLine($"TestProfileLayout: Applied to {config.ProcessName} (hWnd:{hWnd}) X:{targetX} Y:{targetY} W:{targetWidth} H:{targetHeight}");
        }
        Debug.WriteLine("TestProfileLayout: Completed.");
    }

    public void Dispose()
    {
        _timer?.Stop();
        _timer?.Dispose();
        _timer = null;
    }
}