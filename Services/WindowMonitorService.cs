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

    readonly Dictionary<string, int> _autoRelaunchLastKnownPids = new();
    readonly Dictionary<string, DateTime> _autoRelaunchLastAttemptTimestamps = new();
    const int RelaunchCooldownSeconds = 10;


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
        if(_isProgramActivityDisabled)
        {
            _autoRelaunchLastKnownPids.Clear();
            _autoRelaunchLastAttemptTimestamps.Clear();
        }
    }

    public string GetActiveProfileNameFromSettings() => _settingsManager.LoadSettings().ActiveProfileName;

    public void UpdateActiveProfileReference(Profile liveProfile)
    {
        bool profileEffectivelyChanged = _activeProfile?.Name != liveProfile?.Name || _activeProfile == null || liveProfile == null;

        _activeProfile = liveProfile;
        Debug.WriteLine($"WindowMonitorService: Active profile reference updated to '{liveProfile?.Name ?? "null"}'.");

        if(profileEffectivelyChanged || _isProgramActivityDisabled)
        {
            _autoRelaunchLastKnownPids.Clear();
            _autoRelaunchLastAttemptTimestamps.Clear();
        }
    }

    public void SetPositioningActive(bool isActive)
    {
        _isProgramActivityDisabled = !isActive;
        Debug.WriteLine($"WindowMonitorService: PositioningActive set to {isActive}. IsProgramActivityDisabled: {_isProgramActivityDisabled}");
        if(_isProgramActivityDisabled)
        {
            _autoRelaunchLastKnownPids.Clear();
            _autoRelaunchLastAttemptTimestamps.Clear();
        }
    }

    public void InitializeTimer()
    {
        if(_timer != null) { Debug.WriteLine("WindowMonitorService Timer already initialized."); return; }
        _timer = new System.Windows.Forms.Timer { Interval = 1000 };
        _timer.Tick += Timer_Tick;
        _timer.Start();
        Debug.WriteLine("WindowMonitorService Timer Initialized and Started (1s interval).");
    }

    void Timer_Tick(object sender, EventArgs e)
    {
        if(_isProgramActivityDisabled || _activeProfile == null) return;

        ProactiveAutoRelaunchCheckAndLaunch();
        ProcessWindowPositioningLogic();
    }

    void ProactiveAutoRelaunchCheckAndLaunch()
    {
        if(_activeProfile == null || !_activeProfile.WindowConfigs.Any()) return;


        foreach(var config in _activeProfile.WindowConfigs.Where(c => c.IsEnabled && c.AutoRelaunchEnabled))
        {
            string configKey = GetConfigKey(config);

            WindowEnumerationService.FoundWindowInfo foundWindow = WindowEnumerationService.FindMostSuitableWindow(config);
            Process currentProcess = foundWindow?.GetProcess();

            if(currentProcess != null && !currentProcess.HasExited)
            {
                _autoRelaunchLastKnownPids[configKey] = currentProcess.Id;
                if(_autoRelaunchLastAttemptTimestamps.ContainsKey(configKey))
                    _autoRelaunchLastAttemptTimestamps.Remove(configKey);
                currentProcess.Dispose();
            }
            else
            {
                bool specificInstanceExited = false;
                if(_autoRelaunchLastKnownPids.TryGetValue(configKey, out int lastPid))
                {
                    try
                    {
                        Process p = Process.GetProcessById(lastPid);
                        if(p.HasExited) specificInstanceExited = true;
                        p.Dispose();
                    }
                    catch(ArgumentException) { specificInstanceExited = true; }
                    catch(InvalidOperationException) { specificInstanceExited = true; }
                }

                bool needsLaunch = !_autoRelaunchLastKnownPids.ContainsKey(configKey) || specificInstanceExited || (currentProcess == null || currentProcess.HasExited);

                if(needsLaunch)
                {
                    if(_autoRelaunchLastAttemptTimestamps.TryGetValue(configKey, out DateTime lastAttempt) &&
                        (DateTime.UtcNow - lastAttempt).TotalSeconds < RelaunchCooldownSeconds)
                    {
                        Debug.WriteLine($"AutoRelaunch: Skipping launch for '{config.ProcessName}' due to cooldown.");
                        continue;
                    }

                    Debug.WriteLine($"AutoRelaunch: Process for '{config.ProcessName}' (key: {configKey}) not running or exited. Attempting launch.");
                    bool launched = _windowActionService.LaunchApp(config);
                    _autoRelaunchLastAttemptTimestamps[configKey] = DateTime.UtcNow;

                    if(launched)
                    {
                        Debug.WriteLine($"AutoRelaunch: Launch initiated for '{config.ProcessName}'. Will find new PID on next tick.");
                        _autoRelaunchLastKnownPids.Remove(configKey);
                    }
                    else
                    {
                        Debug.WriteLine($"AutoRelaunch: Launch FAILED for '{config.ProcessName}'. Will retry after cooldown.");
                    }
                }
            }
        }
    }

    string GetConfigKey(WindowConfig config)
    {
        return config.ProcessName.ToLowerInvariant();
    }


    void ProcessWindowPositioningLogic()
    {
        if(_activeProfile == null || _isProgramActivityDisabled) return;
        if(!_activeProfile.WindowConfigs.Any(c => c.IsEnabled && (c.ControlPosition || c.ControlSize))) return;

        _handledWindowHandlesThisCycle.Clear();
        var configsForPositioning = new List<WindowConfig>(_activeProfile.WindowConfigs.Where(c => c.IsEnabled));
        ProcessWindowConfigurations(configsForPositioning, _handledWindowHandlesThisCycle, false, "ProcessWindows");
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
            Debug.WriteLine($"{logContext}: Positioned/Resized '{config.ProcessName}' (hWnd:{hWnd}) X:{targetDimensions.X} Y:{targetDimensions.Y} W:{targetDimensions.Width} H:{targetDimensions.Height}");
        }
        if(logContext != "TestProfileLayout") handledHandles.Add(hWnd);
    }
    (int X, int Y, int Width, int Height) CalculateTargetDimensions(WindowConfig config, RECT currentRect) => (config.ControlPosition ? config.TargetX : currentRect.Left, config.ControlPosition ? config.TargetY : currentRect.Top, config.ControlSize ? config.TargetWidth : currentRect.Width, config.ControlSize ? config.TargetHeight : currentRect.Height);
    bool ShouldApplyWindowChanges(WindowConfig config, RECT currentRect, (int X, int Y, int Width, int Height) targetDimensions) => (config.ControlPosition && (currentRect.Left != targetDimensions.X || currentRect.Top != targetDimensions.Y)) || (config.ControlSize && (currentRect.Width != targetDimensions.Width || currentRect.Height != targetDimensions.Height));
    public void Dispose() { _timer?.Stop(); _timer?.Dispose(); _timer = null; Debug.WriteLine("WindowMonitorService Timer Disposed."); }
}