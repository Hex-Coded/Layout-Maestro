using System.Diagnostics;
using WindowPlacementManager.Models;

namespace WindowPlacementManager.Services;

public class WindowMonitorService : IDisposable
{
    readonly SettingsManager _settingsManager;
    readonly WindowActionService _windowActionService; 
    AppSettingsData _currentSettings;
    Profile _activeProfile;
    readonly HashSet<IntPtr> _handledWindowHandlesThisCycle;
    System.Windows.Forms.Timer _timer;
    bool _isProgramActivityDisabled = true;

    readonly Dictionary<int, WindowConfig> _monitoredPidsForRelaunch = new(); 

    public WindowMonitorService(SettingsManager settingsManager, WindowActionService windowActionService) 
    {
        _settingsManager = settingsManager;
        _windowActionService = windowActionService; 
        _handledWindowHandlesThisCycle = new HashSet<IntPtr>();
        LoadAndApplySettings();
        InitializeTimer();
    }

    public void LoadAndApplySettings()
    {
        _currentSettings = _settingsManager.LoadSettings();
        _activeProfile = _currentSettings.Profiles.FirstOrDefault(p => p.Name == _currentSettings.ActiveProfileName);
        _isProgramActivityDisabled = _currentSettings.DisableProgramActivity;
        _monitoredPidsForRelaunch.Clear(); 
    }

    public void SetPositioningActive(bool isActive)
    {
        _isProgramActivityDisabled = !isActive;
        if (_isProgramActivityDisabled) _monitoredPidsForRelaunch.Clear(); 
    }


    void InitializeTimer()
    {
        _timer = new System.Windows.Forms.Timer { Interval = 300 }; 
        _timer.Tick += Timer_Tick;
        _timer.Start();
    }

    void Timer_Tick(object sender, EventArgs e)
    {
        ProcessAutoRelaunch();
        ProcessWindowPositioning();
    }

    void ProcessAutoRelaunch()
    {
        if (_isProgramActivityDisabled || _activeProfile == null)
        {
            if (_monitoredPidsForRelaunch.Any()) _monitoredPidsForRelaunch.Clear();
            return;
        }

        List<int> pidsToRelaunchAndRemove = new List<int>();
        foreach (var monitoredEntry in _monitoredPidsForRelaunch)
        {
            try
            {
                Process process = Process.GetProcessById(monitoredEntry.Key);
                if (process.HasExited) pidsToRelaunchAndRemove.Add(monitoredEntry.Key);
            }
            catch (ArgumentException) { pidsToRelaunchAndRemove.Add(monitoredEntry.Key); } 
            catch (InvalidOperationException) { pidsToRelaunchAndRemove.Add(monitoredEntry.Key); } 
        }

        foreach (int pid in pidsToRelaunchAndRemove)
            if (_monitoredPidsForRelaunch.TryGetValue(pid, out WindowConfig configToRelaunch))
            {
                Debug.WriteLine($"AutoRelaunch: Process '{configToRelaunch.ProcessName}' (Former PID: {pid}) exited. Relaunching.");
                _windowActionService.LaunchApp(configToRelaunch);
                _monitoredPidsForRelaunch.Remove(pid);
            }

        foreach (var config in _activeProfile.WindowConfigs.Where(c => c.IsEnabled && c.AutoRelaunchEnabled))
        {
            var foundWindow = WindowEnumerationService.FindMostSuitableWindow(config);
            Process process = foundWindow?.GetProcess();
            if (process != null && !process.HasExited && !_monitoredPidsForRelaunch.ContainsKey(process.Id))
            {
                Debug.WriteLine($"AutoRelaunch: Now monitoring '{config.ProcessName}' (PID: {process.Id}).");
                _monitoredPidsForRelaunch[process.Id] = config;
            }
        }
    }

    void ProcessWindowPositioning() 
    {
        if (_isProgramActivityDisabled || _activeProfile == null || !_activeProfile.WindowConfigs.Any()) return;
        _handledWindowHandlesThisCycle.Clear();
        ProcessWindowConfigurations(_activeProfile.WindowConfigs.Where(c => c.IsEnabled), _handledWindowHandlesThisCycle, false, "ProcessWindows");
    }


    public void TestProfileLayout(Profile profileToTest) 
    {
        if (profileToTest == null || !profileToTest.WindowConfigs.Any()) { Debug.WriteLine("TestProfileLayout: Profile null or no configs."); return; }
        HashSet<IntPtr> handledHWndsThisTest = new HashSet<IntPtr>();
        ProcessWindowConfigurations(profileToTest.WindowConfigs.Where(c => c.IsEnabled), handledHWndsThisTest, true, "TestProfileLayout");
        Debug.WriteLine("TestProfileLayout: Completed.");
    }

    void ProcessWindowConfigurations(IEnumerable<WindowConfig> configs, HashSet<IntPtr> handledHandles, bool alwaysApply, string logContext)
    {
        foreach (var config in configs) ApplyConfigurationToSingleWindow(config, handledHandles, alwaysApply, logContext);
    }

    void ApplyConfigurationToSingleWindow(WindowConfig config, HashSet<IntPtr> handledHandles, bool alwaysApply, string logContext)
    {
        if (string.IsNullOrWhiteSpace(config.ProcessName)) return;
        var foundWindow = WindowEnumerationService.FindMostSuitableWindow(config);
        if (foundWindow == null || foundWindow.HWnd == IntPtr.Zero)
        {
            if (logContext == "TestProfileLayout") Debug.WriteLine($"{logContext}: Window not found for '{config.ProcessName}' Hint:'{config.WindowTitleHint}'.");
            return;
        }
        IntPtr hWnd = foundWindow.HWnd;
        if (handledHandles.Contains(hWnd)) return;
        if (!Native.GetWindowRect(hWnd, out RECT currentRect)) return;
        var targetDimensions = CalculateTargetDimensions(config, currentRect);
        if (targetDimensions.Width <= 0 || targetDimensions.Height <= 0) { Debug.WriteLine($"{logContext}: Invalid target dims for {config.ProcessName} (hWnd:{hWnd}) W:{targetDimensions.Width} H:{targetDimensions.Height}"); return; }
        bool needsChange = ShouldApplyWindowChanges(config, currentRect, targetDimensions);
        if (alwaysApply || needsChange)
        {
            Native.MoveWindow(hWnd, targetDimensions.X, targetDimensions.Y, targetDimensions.Width, targetDimensions.Height, true);
            Debug.WriteLine($"{logContext}: Applied to {config.ProcessName} (hWnd:{hWnd}) X:{targetDimensions.X} Y:{targetDimensions.Y} W:{targetDimensions.Width} H:{targetDimensions.Height}");
        }
        handledHandles.Add(hWnd);
    }

    (int X, int Y, int Width, int Height) CalculateTargetDimensions(WindowConfig config, RECT currentRect) =>
        (config.ControlPosition ? config.TargetX : currentRect.Left,
         config.ControlPosition ? config.TargetY : currentRect.Top,
         config.ControlSize ? config.TargetWidth : currentRect.Width,
         config.ControlSize ? config.TargetHeight : currentRect.Height);

    bool ShouldApplyWindowChanges(WindowConfig config, RECT currentRect, (int X, int Y, int Width, int Height) targetDimensions) =>
        (config.ControlPosition && (currentRect.Left != targetDimensions.X || currentRect.Top != targetDimensions.Y)) ||
        (config.ControlSize && (currentRect.Width != targetDimensions.Width || currentRect.Height != targetDimensions.Height));

    public void Dispose()
    {
        _timer?.Stop();
        _timer?.Dispose();
        _timer = null;
    }
}