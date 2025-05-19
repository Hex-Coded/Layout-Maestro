using System.Diagnostics;
using WindowPlacementManager.Models;

namespace WindowPlacementManager.Services;

public class MonitoredProcessState
{
    public WindowConfig Config { get; set; }
    public bool HasBeenObservedRunning { get; set; } = false;
    public int? LastSeenProcessId { get; set; }
    public bool HasBeenPositionedThisInstance { get; set; } = false;

    public bool IsUacLaunchPending { get; set; } = false;
    public DateTime? LastUacLaunchAttemptTime { get; set; } = null;
    public DateTime? LastUacUserCancelTime { get; set; } = null;

    public MonitoredProcessState(WindowConfig config) => Config = config;
}

public class WindowMonitorService : IDisposable
{
    readonly SettingsManager _settingsManager;
    readonly WindowActionService _windowActionService;
    AppSettingsData _appSettings;
    Profile _activeProfile;
    System.Windows.Forms.Timer _monitorTimer;
    bool _isPositioningActive = true;
    Dictionary<string, MonitoredProcessState> _activeMonitoredStates = new Dictionary<string, MonitoredProcessState>();
    readonly object _lockMonitoredStates = new object();

    private const int UAC_PENDING_TIMEOUT_SECONDS = 60;
    private const int UAC_CANCEL_COOLDOWN_SECONDS = int.MaxValue;

    public WindowMonitorService(SettingsManager settingsManager, WindowActionService windowActionService)
    {
        _settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
        _windowActionService = windowActionService ?? throw new ArgumentNullException(nameof(windowActionService));
    }

    string GetConfigKey(WindowConfig config)
    {
        if(config == null) return string.Empty;
        return $"{config.ProcessName?.ToLowerInvariant() ?? ""}|{config.ExecutablePath?.ToLowerInvariant() ?? ""}|{config.WindowTitleHint?.ToLowerInvariant() ?? ""}";
    }

    public void LoadAndApplySettings()
    {
        _appSettings = _settingsManager.LoadSettings();
        if(_appSettings == null)
        {
            Debug.WriteLine("WindowMonitorService: Failed to load settings.");
            _appSettings = new AppSettingsData();
        }

        if(_monitorTimer != null)
        {
            _monitorTimer.Interval = _appSettings.MonitorIntervalMs > 0 ? _appSettings.MonitorIntervalMs : 1000;
        }

        SetPositioningActive(!_appSettings.DisableProgramActivity);

        Profile loadedProfile = null;
        if(!string.IsNullOrEmpty(_appSettings.ActiveProfileName) && _appSettings.Profiles.Any())
        {
            loadedProfile = _appSettings.Profiles.FirstOrDefault(p => p.Name == _appSettings.ActiveProfileName);
        }
        if(loadedProfile == null && _appSettings.Profiles.Any())
        {
            loadedProfile = _appSettings.Profiles.First();
        }
        UpdateActiveProfileReference(loadedProfile);
    }

    public string GetActiveProfileNameFromSettings() => _appSettings?.ActiveProfileName ?? string.Empty;

    public void UpdateActiveProfileReference(Profile newProfile)
    {
        lock(_lockMonitoredStates)
        {
            _activeProfile = newProfile;
            var newStatesCache = new Dictionary<string, MonitoredProcessState>();

            if(_activeProfile != null)
            {
                var configsInNewProfile = _activeProfile.WindowConfigs
                                                 .Where(c => c.IsEnabled && (!string.IsNullOrEmpty(c.ProcessName) || !string.IsNullOrEmpty(c.ExecutablePath)))
                                                 .ToList();

                foreach(var config in configsInNewProfile)
                {
                    string key = GetConfigKey(config);
                    if(_activeMonitoredStates.TryGetValue(key, out var existingState))
                    {
                        existingState.Config = config;
                        newStatesCache[key] = existingState;
                    }
                    else
                    {
                        newStatesCache[key] = new MonitoredProcessState(config);
                    }
                }
            }
            _activeMonitoredStates = newStatesCache;
            Debug.WriteLine($"WindowMonitorService: Active profile updated to '{_activeProfile?.Name ?? "None"}'. Monitoring {_activeMonitoredStates.Count} configs.");

            if(_monitorTimer != null)
            {
                if(_isPositioningActive && _activeProfile != null && _activeMonitoredStates.Any())
                {
                    if(!_monitorTimer.Enabled) _monitorTimer.Start();
                }
                else
                {
                    if(_monitorTimer.Enabled) _monitorTimer.Stop();
                }
            }
        }
    }

    public void NotifyAppLaunched(WindowConfig launchedConfig)
    {
        lock(_lockMonitoredStates)
        {
            string key = GetConfigKey(launchedConfig);
            if(string.IsNullOrEmpty(key))
            {
                Debug.WriteLine("NotifyAppLaunched: Received null or empty key for launchedConfig.");
                return;
            }

            if(_activeMonitoredStates.TryGetValue(key, out var monitoredState))
            {
                string appName = launchedConfig.ProcessName ?? launchedConfig.ExecutablePath ?? "UnknownApp";
                Debug.WriteLine($"WindowMonitorService: Notified of launch attempt for '{appName}' (Key: {key}).");

                if(launchedConfig.LaunchAsAdmin)
                {
                    if(!monitoredState.IsUacLaunchPending)
                    {
                        Debug.WriteLine($"---> NotifyAppLaunched: Setting IsUacLaunchPending=true for '{appName}' (admin app) because it wasn't already pending.");
                        monitoredState.IsUacLaunchPending = true;
                        monitoredState.LastUacLaunchAttemptTime = DateTime.UtcNow;
                        monitoredState.LastUacUserCancelTime = null;
                    }
                    else
                    {
                        Debug.WriteLine($"---> NotifyAppLaunched: IsUacLaunchPending for '{appName}' was ALREADY true. Not re-setting pending state from Notify.");
                    }
                }
            }
            else
            {
                Debug.WriteLine($"NotifyAppLaunched: Could not find MonitoredProcessState for key '{key}'. This might happen if profile changed rapidly.");
            }
        }
    }

    private void HandleProcessNotRunning(MonitoredProcessState state)
    {
        string appName = state.Config.ProcessName ?? state.Config.ExecutablePath ?? "UnknownApp";

        Debug.WriteLine($"--- HPNR Pre-Check for {appName}: IsUacLaunchPending={state.IsUacLaunchPending}, LastUacLaunchAttemptTime={state.LastUacLaunchAttemptTime?.ToString("O") ?? "N/A"}, LastUacUserCancelTime={state.LastUacUserCancelTime?.ToString("O") ?? "N/A"}, HasBeenObservedRunning={state.HasBeenObservedRunning}");

        if(state.IsUacLaunchPending)
        {
            if(state.LastUacLaunchAttemptTime.HasValue &&
                (DateTime.UtcNow - state.LastUacLaunchAttemptTime.Value).TotalSeconds < UAC_PENDING_TIMEOUT_SECONDS)
            {
                double remainingTime = UAC_PENDING_TIMEOUT_SECONDS - (DateTime.UtcNow - state.LastUacLaunchAttemptTime.Value).TotalSeconds;
                Debug.WriteLine($"HandleProcessNotRunning: '{appName}' has a PENDING UAC launch (within {UAC_PENDING_TIMEOUT_SECONDS}s timeout). Timer WILL NOT attempt relaunch NOW. Time remaining: {remainingTime:F0}s");
                return;
            }
            else
            {
                Debug.WriteLine($"HandleProcessNotRunning: Pending UAC for '{appName}' has TIMED OUT. Resetting IsUacLaunchPending flag.");
                state.IsUacLaunchPending = false;
                state.LastUacLaunchAttemptTime = null;
                if(!state.LastUacUserCancelTime.HasValue || (state.LastUacLaunchAttemptTime.HasValue && state.LastUacUserCancelTime < state.LastUacLaunchAttemptTime.Value))
                {
                    Debug.WriteLine($"HandleProcessNotRunning: Treating UAC timeout for '{appName}' as an implicit cancel for cooldown purposes.");
                    state.LastUacUserCancelTime = DateTime.UtcNow;
                }
            }
        }

        if(state.LastUacUserCancelTime.HasValue)
        {
            if((DateTime.UtcNow - state.LastUacUserCancelTime.Value).TotalSeconds < UAC_CANCEL_COOLDOWN_SECONDS)
            {
                double remainingCooldown = UAC_CANCEL_COOLDOWN_SECONDS - (DateTime.UtcNow - state.LastUacUserCancelTime.Value).TotalSeconds;
                Debug.WriteLine($"HandleProcessNotRunning: '{appName}' UAC was RECENTLY CANCELLED or TIMED OUT (within {UAC_CANCEL_COOLDOWN_SECONDS}s cooldown). Timer WILL NOT attempt relaunch NOW. Cooldown remaining: {remainingCooldown:F0}s");
                return;
            }
            else
            {
                Debug.WriteLine($"HandleProcessNotRunning: UAC cancel/timeout cooldown for '{appName}' has EXPIRED. Clearing LastUacUserCancelTime.");
                state.LastUacUserCancelTime = null;
            }
        }

        if(state.Config.AutoRelaunchEnabled && state.HasBeenObservedRunning)
        {
            if(state.Config.LaunchAsAdmin)
            {
                Debug.WriteLine($"---> HandleProcessNotRunning: Preparing for ADMIN auto-relaunch of '{appName}'. Setting IsUacLaunchPending=true BEFORE calling LaunchApp.");
                state.IsUacLaunchPending = true;
                state.LastUacLaunchAttemptTime = DateTime.UtcNow;
                state.LastUacUserCancelTime = null;
            }

            Debug.WriteLine($"HandleProcessNotRunning: '{appName}' not running. AutoRelaunch=true & HasBeenObservedRunning=true. Timer WILL attempt relaunch NOW. (IsUacLaunchPending was just set if admin: {state.IsUacLaunchPending})");

            var launchResult = _windowActionService.LaunchApp(state.Config, supressErrorDialogs: true);

            switch(launchResult)
            {
                case LaunchAttemptResult.Success:
                    Debug.WriteLine($"---> HandleProcessNotRunning: Admin relaunch of '{appName}' initiated by timer (UAC should be visible). IsUacLaunchPending is {state.IsUacLaunchPending}.");
                    break;

                case LaunchAttemptResult.SuccessNoAdminNeeded:
                    Debug.WriteLine($"---> HandleProcessNotRunning: Non-admin relaunch of '{appName}' initiated by timer.");
                    state.IsUacLaunchPending = false;
                    state.LastUacLaunchAttemptTime = null;
                    break;

                case LaunchAttemptResult.UacCancelled:
                    Debug.WriteLine($"---> HandleProcessNotRunning: Relaunch of '{appName}' (initiated by timer) was CANCELLED by UAC. Setting LastUacUserCancelTime for cooldown.");
                    state.LastUacUserCancelTime = DateTime.UtcNow;
                    state.IsUacLaunchPending = false;
                    state.LastUacLaunchAttemptTime = null;
                    break;

                case LaunchAttemptResult.Failed:
                case LaunchAttemptResult.ConfigError:
                case LaunchAttemptResult.ElevationRequiredButNotRequested:
                default:
                    Debug.WriteLine($"HandleProcessNotRunning: Relaunch of '{appName}' (initiated by timer) failed or was not applicable. Result: {launchResult}.");
                    if(state.Config.LaunchAsAdmin)
                    {
                        Debug.WriteLine($"---> HandleProcessNotRunning: Clearing IsUacLaunchPending for '{appName}' due to launch failure (Result: {launchResult}).");
                    }
                    state.IsUacLaunchPending = false;
                    state.LastUacLaunchAttemptTime = null;
                    break;
            }
        }
        else if(state.Config.AutoRelaunchEnabled && !state.HasBeenObservedRunning)
        {
            Debug.WriteLine($"HandleProcessNotRunning: '{appName}' not running. AutoRelaunch=true, but HasBeenObservedRunning=false. Timer will NOT attempt auto-relaunch.");
        }
        else if(!state.Config.AutoRelaunchEnabled)
        {
            Debug.WriteLine($"HandleProcessNotRunning: '{appName}' not running, but AutoRelaunch is disabled. No action.");
        }
    }



    public void InitializeTimer()
    {
        if(_monitorTimer == null)
        {
            _monitorTimer = new System.Windows.Forms.Timer();
            _monitorTimer.Interval = _appSettings?.MonitorIntervalMs > 0 ? _appSettings.MonitorIntervalMs : 1000;
            _monitorTimer.Tick += MonitorWindowsTimer_Tick;

            if(_isPositioningActive && _activeProfile != null && _activeMonitoredStates.Any())
            {
                _monitorTimer.Start();
            }
            Debug.WriteLine($"WindowMonitorService: Timer initialized. Interval: {_monitorTimer.Interval}ms. Running: {_monitorTimer.Enabled}");
        }
    }

    void MonitorWindowsTimer_Tick(object sender, EventArgs e)
    {
        if(!_isPositioningActive || _activeProfile == null) return;

        List<MonitoredProcessState> currentStatesToProcess;
        lock(_lockMonitoredStates)
        {
            currentStatesToProcess = _activeMonitoredStates.Values
                .Where(s => s.Config != null && (!string.IsNullOrEmpty(s.Config.ProcessName) || !string.IsNullOrEmpty(s.Config.ExecutablePath)))
                .ToList();
        }

        if(!currentStatesToProcess.Any()) return;

        foreach(var state in currentStatesToProcess)
        {
            if(!state.Config.IsEnabled) continue;

            WindowEnumerationService.FoundWindowInfo windowInfo = null;
            Process process = null;

            try
            {
                windowInfo = _windowActionService.FindManagedWindow(state.Config);
                if(windowInfo != null && windowInfo.HWnd != IntPtr.Zero)
                {
                    process = windowInfo.GetProcess();
                }

                if(process != null && !process.HasExited)
                {
                    if(state.IsUacLaunchPending || state.LastUacUserCancelTime.HasValue)
                    {
                        Debug.WriteLine($"WindowMonitorService: '{state.Config.ProcessName}' (PID: {process.Id}) confirmed running. Clearing UAC state flags.");
                        state.IsUacLaunchPending = false;
                        state.LastUacLaunchAttemptTime = null;
                        state.LastUacUserCancelTime = null;
                    }

                    bool needsPositioning = false;
                    if(state.LastSeenProcessId != process.Id)
                    {
                        needsPositioning = true;
                        Debug.WriteLine($"WindowMonitorService: New instance detected for '{state.Config.ProcessName}' (PID: {process.Id}, Old PID: {state.LastSeenProcessId?.ToString() ?? "N/A"}). Marking for positioning.");
                    }
                    else if(!state.HasBeenPositionedThisInstance)
                    {
                        needsPositioning = true;
                        Debug.WriteLine($"WindowMonitorService: Existing instance for '{state.Config.ProcessName}' (PID: {process.Id}) not yet positioned. Marking for positioning.");
                    }

                    if(needsPositioning)
                    {
                        ApplyWindowLayout(state.Config, windowInfo.HWnd, false);
                        state.HasBeenPositionedThisInstance = true;
                    }

                    if(!state.HasBeenObservedRunning)
                    {
                        state.HasBeenObservedRunning = true;
                        Debug.WriteLine($"WindowMonitorService: Successfully observed '{state.Config.ProcessName}' (PID: {process.Id}) running for the first time by the monitor. Auto-relaunch will now be considered if it closes.");
                    }

                    state.LastSeenProcessId = process.Id;
                }
                else
                {
                    if(state.LastSeenProcessId != null)
                    {
                        Debug.WriteLine($"WindowMonitorService: Process '{state.Config.ProcessName}' (Last PID: {state.LastSeenProcessId}) is no longer running or its window not found. Resetting instance-specific state.");
                    }
                    state.HasBeenPositionedThisInstance = false;
                    state.LastSeenProcessId = null;

                    HandleProcessNotRunning(state);
                }
            }
            catch(InvalidOperationException ex)
            {
                Debug.WriteLine($"WindowMonitorService: Process '{state.Config.ProcessName}' likely exited or inaccessible. Error: {ex.Message}. Resetting instance-specific state.");
                state.HasBeenPositionedThisInstance = false;
                state.LastSeenProcessId = null;
                HandleProcessNotRunning(state);
            }
            catch(Exception ex)
            {
                Debug.WriteLine($"WindowMonitorService: Error processing '{state.Config.ProcessName}': {ex.Message}");
            }
            finally
            {
                process?.Dispose();
            }
        }
    }

    void ApplyWindowLayout(WindowConfig config, IntPtr hWnd, bool forceApply)
    {
        if(hWnd == IntPtr.Zero || (!config.ControlPosition && !config.ControlSize && !forceApply)) return;

        if(Native.IsIconic(hWnd) && !forceApply) return;

        bool shouldSetPos = config.ControlPosition || forceApply;
        bool shouldSetSize = config.ControlSize || forceApply;

        if(!shouldSetPos && !shouldSetSize && !forceApply) return;

        Native.GetWindowRect(hWnd, out RECT currentRect);

        int x = currentRect.Left;
        int y = currentRect.Top;
        int width = currentRect.Width;
        int height = currentRect.Height;
        bool changed = false;

        if(shouldSetPos)
        {
            if(currentRect.Left != config.TargetX || currentRect.Top != config.TargetY)
            {
                x = config.TargetX;
                y = config.TargetY;
                changed = true;
            }
        }
        if(shouldSetSize)
        {
            if(currentRect.Width != config.TargetWidth || currentRect.Height != config.TargetHeight)
            {
                width = config.TargetWidth;
                height = config.TargetHeight;
                changed = true;
            }
        }

        if(changed || forceApply)
        {
            Debug.WriteLine($"WindowMonitorService: Applying layout to '{config.ProcessName ?? "Unknown"}' (hWnd {hWnd}). New Pos: ({x},{y}), New Size: ({width}x{height})");
            Native.SetWindowPos(hWnd, IntPtr.Zero, x, y, width, height, Native.SWP_NOZORDER | Native.SWP_NOACTIVATE | Native.SWP_ASYNCWINDOWPOS);
        }
    }

    public void SetPositioningActive(bool isActive)
    {
        _isPositioningActive = isActive;
        if(_monitorTimer != null)
        {
            if(_isPositioningActive && _activeProfile != null && _activeMonitoredStates.Any())
            {
                if(!_monitorTimer.Enabled) _monitorTimer.Start();
            }
            else
            {
                if(_monitorTimer.Enabled) _monitorTimer.Stop();
            }
        }
        Debug.WriteLine($"WindowMonitorService: Positioning active set to {_isPositioningActive}. Timer running: {_monitorTimer?.Enabled ?? false}");
    }

    public async Task TestProfileLayout(Profile profileToTest)
    {
        if(profileToTest == null || !profileToTest.WindowConfigs.Any(wc => wc.IsEnabled))
        {
            Debug.WriteLine("WindowMonitorService: TestProfileLayout - No profile or enabled configs to test.");
            System.Windows.Forms.MessageBox.Show("No enabled configurations in the selected profile to test.", "Test Layout", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
            return;
        }

        Debug.WriteLine($"WindowMonitorService: Testing layout for profile '{profileToTest.Name}'.");
        int appliedCount = 0;
        int notFoundCount = 0;

        foreach(var config in profileToTest.WindowConfigs.Where(wc => wc.IsEnabled))
        {
            WindowEnumerationService.FoundWindowInfo windowInfo = _windowActionService.FindManagedWindow(config);
            if(windowInfo != null && windowInfo.HWnd != IntPtr.Zero)
            {
                ApplyWindowLayout(config, windowInfo.HWnd, true);
                if(_appSettings.BringToForegroundOnTest)
                {
                    await Task.Delay(50);
                    _windowActionService.BringWindowToForeground(windowInfo.HWnd);
                }
                appliedCount++;
            }
            else
            {
                Debug.WriteLine($"WindowMonitorService: TestProfileLayout - Window for '{config.ProcessName ?? "Unknown"}' not found.");
                notFoundCount++;
            }
            await Task.Delay(_appSettings.DelayBetweenActionsMs > 0 ? _appSettings.DelayBetweenActionsMs : 100);
        }
        System.Windows.Forms.MessageBox.Show($"Layout test complete for profile '{profileToTest.Name}'.\n\nApplied to: {appliedCount} window(s)\nNot found: {notFoundCount} window(s)", "Test Layout Complete", System.Windows.Forms.MessageBoxButtons.OK);
    }

    public void Dispose()
    {
        if(_monitorTimer != null)
        {
            _monitorTimer.Stop();
            _monitorTimer.Tick -= MonitorWindowsTimer_Tick;
            _monitorTimer.Dispose();
            _monitorTimer = null;
        }
        Debug.WriteLine("WindowMonitorService: Disposed.");
    }
}
