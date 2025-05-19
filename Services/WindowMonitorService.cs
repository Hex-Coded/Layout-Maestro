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
    }

    public void LoadAndApplySettings()
    {
        _currentSettings = _settingsManager.LoadSettings();
        _isProgramActivityDisabled = _currentSettings.DisableProgramActivity;

        if(!_isProgramActivityDisabled && _activeProfile != null)
            RefreshMonitoredPidsForRelaunch();
        else if(_isProgramActivityDisabled)
        {
            if(_monitoredPidsForRelaunch.Any()) Debug.WriteLine($"AutoRelaunch: Clearing all monitored PIDs due to LoadAndApplySettings indicating program disabled.");
            _monitoredPidsForRelaunch.Clear();
        }
    }

    public string GetActiveProfileNameFromSettings() => _settingsManager.LoadSettings().ActiveProfileName;

    public void UpdateActiveProfileReference(Profile liveProfile)
    {
        _activeProfile = liveProfile;
        Debug.WriteLine($"WindowMonitorService: Active profile reference updated to '{liveProfile?.Name ?? "null"}'.");
        RefreshMonitoredPidsForRelaunch();
    }

    void RefreshMonitoredPidsForRelaunch()
    {
        if(_activeProfile == null || _isProgramActivityDisabled)
        {
            if(_monitoredPidsForRelaunch.Any()) Debug.WriteLine($"AutoRelaunch: Clearing all monitored PIDs (Profile: {_activeProfile?.Name ?? "null"}, Disabled: {_isProgramActivityDisabled}).");
            _monitoredPidsForRelaunch.Clear();
            return;
        }

        var pidsThatShouldBeMonitored = new Dictionary<int, WindowConfig>();
        foreach(var config in _activeProfile.WindowConfigs.Where(c => c.IsEnabled && c.AutoRelaunchEnabled))
        {
            var foundWindow = WindowEnumerationService.FindMostSuitableWindow(config);
            Process process = foundWindow?.GetProcess();
            if(process != null && !process.HasExited)
                pidsThatShouldBeMonitored[process.Id] = config;
        }

        var currentMonitoredPidsCopy = new List<int>(_monitoredPidsForRelaunch.Keys);
        foreach(int pidInDict in currentMonitoredPidsCopy)
            if(!pidsThatShouldBeMonitored.ContainsKey(pidInDict))
            {
                _monitoredPidsForRelaunch.Remove(pidInDict);
                Debug.WriteLine($"AutoRelaunch: Stopped monitoring PID {pidInDict} (config changed or process no longer matches).");
            }

        foreach(var entry in pidsThatShouldBeMonitored)
            if(!_monitoredPidsForRelaunch.ContainsKey(entry.Key))
            {
                _monitoredPidsForRelaunch[entry.Key] = entry.Value;
                Debug.WriteLine($"AutoRelaunch: Now monitoring '{entry.Value.ProcessName}' (PID: {entry.Key}).");
            }
    }

    public void SetPositioningActive(bool isActive)
    {
        _isProgramActivityDisabled = !isActive;
        Debug.WriteLine($"WindowMonitorService: PositioningActive set to {isActive}. IsProgramActivityDisabled: {_isProgramActivityDisabled}");
        if(_isProgramActivityDisabled)
        {
            if(_monitoredPidsForRelaunch.Any()) Debug.WriteLine($"AutoRelaunch: Clearing all monitored PIDs because program activity was disabled.");
            _monitoredPidsForRelaunch.Clear();
        }
        else
        {
            RefreshMonitoredPidsForRelaunch();
        }
    }

    public void InitializeTimer()
    {
        if(_timer != null)
        {
            Debug.WriteLine("WindowMonitorService Timer already initialized.");
            return;
        }
        _timer = new System.Windows.Forms.Timer { Interval = 300 };
        _timer.Tick += Timer_Tick;
        _timer.Start();
        Debug.WriteLine("WindowMonitorService Timer Initialized and Started.");
    }

    void Timer_Tick(object sender, EventArgs e)
    {
        if(_isProgramActivityDisabled || _activeProfile == null) return;

        ProcessAutoRelaunchLogic();
        ProcessWindowPositioningLogic();
    }

    void ProcessAutoRelaunchLogic()
    {
        if(!_monitoredPidsForRelaunch.Any()) return;

        List<int> pidsToRelaunchAndRemove = new List<int>();
        var pidsToCheck = new List<int>(_monitoredPidsForRelaunch.Keys);

        foreach(int pid in pidsToCheck)
        {
            if(!_monitoredPidsForRelaunch.ContainsKey(pid)) continue;

            try
            {
                Process process = Process.GetProcessById(pid);
                if(process.HasExited) pidsToRelaunchAndRemove.Add(pid);
            }
            catch(ArgumentException) { pidsToRelaunchAndRemove.Add(pid); }
            catch(InvalidOperationException) { pidsToRelaunchAndRemove.Add(pid); }
        }

        bool relaunchedAny = false;
        foreach(int pid in pidsToRelaunchAndRemove)
            if(_monitoredPidsForRelaunch.TryGetValue(pid, out WindowConfig configToRelaunch))
            {
                Debug.WriteLine($"AutoRelaunch: Process '{configToRelaunch.ProcessName}' (Former PID: {pid}) found exited. Attempting relaunch.");
                _windowActionService.LaunchApp(configToRelaunch);
                _monitoredPidsForRelaunch.Remove(pid);
                relaunchedAny = true;
            }

        if(relaunchedAny)
            RefreshMonitoredPidsForRelaunch();
    }

    void ProcessWindowPositioningLogic()
    {
        if(_activeProfile == null || !_activeProfile.WindowConfigs.Any(c => c.IsEnabled && (c.ControlPosition || c.ControlSize))) return;

        _handledWindowHandlesThisCycle.Clear();
        ProcessWindowConfigurations(_activeProfile.WindowConfigs.Where(c => c.IsEnabled), _handledWindowHandlesThisCycle, false, "ProcessWindows");
    }

    public void TestProfileLayout(Profile profileToTest)
    {
        if(profileToTest == null || !profileToTest.WindowConfigs.Any()) { Debug.WriteLine("TestProfileLayout: Profile null or no configs."); return; }
        HashSet<IntPtr> handledHWndsThisTest = new HashSet<IntPtr>();
        ProcessWindowConfigurations(profileToTest.WindowConfigs.Where(c => c.IsEnabled), handledHWndsThisTest, true, "TestProfileLayout");
        Debug.WriteLine("TestProfileLayout: Completed.");
    }

    void ProcessWindowConfigurations(IEnumerable<WindowConfig> configs, HashSet<IntPtr> handledHandles, bool alwaysApply, string logContext)
    {
        foreach(var config in configs) ApplyConfigurationToSingleWindow(config, handledHandles, alwaysApply, logContext);
    }

    void ApplyConfigurationToSingleWindow(WindowConfig config, HashSet<IntPtr> handledHandles, bool alwaysApply, string logContext)
    {
        if(string.IsNullOrWhiteSpace(config.ProcessName)) return;
        if(!config.ControlPosition && !config.ControlSize && logContext != "TestProfileLayout") return;

        var foundWindow = WindowEnumerationService.FindMostSuitableWindow(config);
        if(foundWindow == null || foundWindow.HWnd == IntPtr.Zero)
        {
            if(logContext == "TestProfileLayout") Debug.WriteLine($"{logContext}: Window not found for '{config.ProcessName}' Hint:'{config.WindowTitleHint}'.");
            return;
        }
        IntPtr hWnd = foundWindow.HWnd;
        if(handledHandles.Contains(hWnd) && logContext != "TestProfileLayout") return;
        if(!Native.GetWindowRect(hWnd, out RECT currentRect)) return;

        var targetDimensions = CalculateTargetDimensions(config, currentRect);
        if(targetDimensions.Width <= 0 || targetDimensions.Height <= 0) { Debug.WriteLine($"{logContext}: Invalid target dims for {config.ProcessName} (hWnd:{hWnd}) W:{targetDimensions.Width} H:{targetDimensions.Height}"); return; }

        bool needsChange = ShouldApplyWindowChanges(config, currentRect, targetDimensions);
        if(alwaysApply || needsChange)
        {
            Native.MoveWindow(hWnd, targetDimensions.X, targetDimensions.Y, targetDimensions.Width, targetDimensions.Height, true);
            Debug.WriteLine($"{logContext}: Applied to {config.ProcessName} (hWnd:{hWnd}) X:{targetDimensions.X} Y:{targetDimensions.Y} W:{targetDimensions.Width} H:{targetDimensions.Height}");
        }
        if(logContext != "TestProfileLayout") handledHandles.Add(hWnd);
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
        Debug.WriteLine("WindowMonitorService Timer Disposed.");
    }
}