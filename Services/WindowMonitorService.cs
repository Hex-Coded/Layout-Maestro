using System.Diagnostics;
using WindowPlacementManager.Models;

namespace WindowPlacementManager.Services;

public class WindowMonitorService : IDisposable
{
    readonly SettingsManager settingsManager;
    readonly WindowActionService windowActionService;
    AppSettingsData currentSettings;
    Profile activeProfile;
    readonly HashSet<IntPtr> handledWindowHandlesThisCycle;
    System.Windows.Forms.Timer timer;
    bool isProgramActivityDisabled = true;

    readonly Dictionary<string, int> autoRelaunchLastKnownPids = new();
    readonly Dictionary<string, DateTime> autoRelaunchLastAttemptTimestamps = new();
    const int RelaunchCooldownSeconds = 10;


    public WindowMonitorService(SettingsManager settingsManager, WindowActionService windowActionService)
    {
        this.settingsManager = settingsManager;
        this.windowActionService = windowActionService;
        handledWindowHandlesThisCycle = new HashSet<IntPtr>();
    }

    public void LoadAndApplySettings()
    {
        currentSettings = settingsManager.LoadSettings();
        isProgramActivityDisabled = currentSettings.DisableProgramActivity;
        if(isProgramActivityDisabled)
        {
            autoRelaunchLastKnownPids.Clear();
            autoRelaunchLastAttemptTimestamps.Clear();
        }
    }

    public string GetActiveProfileNameFromSettings() => settingsManager.LoadSettings().ActiveProfileName;

    public void UpdateActiveProfileReference(Profile liveProfile)
    {
        bool profileEffectivelyChanged = activeProfile?.Name != liveProfile?.Name || activeProfile == null || liveProfile == null;

        activeProfile = liveProfile;
        Debug.WriteLine($"WindowMonitorService: Active profile reference updated to '{liveProfile?.Name ?? "null"}'.");

        if(profileEffectivelyChanged || isProgramActivityDisabled)
        {
            autoRelaunchLastKnownPids.Clear();
            autoRelaunchLastAttemptTimestamps.Clear();
        }
    }

    public void SetPositioningActive(bool isActive)
    {
        isProgramActivityDisabled = !isActive;
        Debug.WriteLine($"WindowMonitorService: PositioningActive set to {isActive}. IsProgramActivityDisabled: {isProgramActivityDisabled}");
        if(isProgramActivityDisabled)
        {
            autoRelaunchLastKnownPids.Clear();
            autoRelaunchLastAttemptTimestamps.Clear();
        }
    }

    public void InitializeTimer()
    {
        if(timer != null) { Debug.WriteLine("WindowMonitorService Timer already initialized."); return; }
        timer = new System.Windows.Forms.Timer { Interval = 1000 };
        timer.Tick += Timer_Tick;
        timer.Start();
        Debug.WriteLine("WindowMonitorService Timer Initialized and Started (1s interval).");
    }

    void Timer_Tick(object sender, EventArgs e)
    {
        if(isProgramActivityDisabled || activeProfile == null) return;

        ProactiveAutoRelaunchCheckAndLaunch();
        ProcessWindowPositioningLogic();
    }

    void ProactiveAutoRelaunchCheckAndLaunch()
    {
        if(activeProfile == null || !activeProfile.WindowConfigs.Any()) return;


        foreach(var config in activeProfile.WindowConfigs.Where(c => c.IsEnabled && c.AutoRelaunchEnabled))
        {
            string configKey = GetConfigKey(config);

            WindowEnumerationService.FoundWindowInfo foundWindow = WindowEnumerationService.FindMostSuitableWindow(config);
            Process currentProcess = foundWindow?.GetProcess();

            if(currentProcess != null && !currentProcess.HasExited)
            {
                autoRelaunchLastKnownPids[configKey] = currentProcess.Id;
                if(autoRelaunchLastAttemptTimestamps.ContainsKey(configKey))
                    autoRelaunchLastAttemptTimestamps.Remove(configKey);
                currentProcess.Dispose();
            }
            else
            {
                bool specificInstanceExited = false;
                if(autoRelaunchLastKnownPids.TryGetValue(configKey, out int lastPid))
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

                bool needsLaunch = !autoRelaunchLastKnownPids.ContainsKey(configKey) || specificInstanceExited || (currentProcess == null || currentProcess.HasExited);

                if(needsLaunch)
                {
                    if(autoRelaunchLastAttemptTimestamps.TryGetValue(configKey, out DateTime lastAttempt) &&
                        (DateTime.UtcNow - lastAttempt).TotalSeconds < RelaunchCooldownSeconds)
                    {
                        Debug.WriteLine($"AutoRelaunch: Skipping launch for '{config.ProcessName}' due to cooldown.");
                        continue;
                    }

                    Debug.WriteLine($"AutoRelaunch: Process for '{config.ProcessName}' (key: {configKey}) not running or exited. Attempting launch.");
                    bool launched = windowActionService.LaunchApp(config);
                    autoRelaunchLastAttemptTimestamps[configKey] = DateTime.UtcNow;

                    if(launched)
                    {
                        Debug.WriteLine($"AutoRelaunch: Launch initiated for '{config.ProcessName}'. Will find new PID on next tick.");
                        autoRelaunchLastKnownPids.Remove(configKey);
                    }
                    else
                    {
                        Debug.WriteLine($"AutoRelaunch: Launch FAILED for '{config.ProcessName}'. Will retry after cooldown.");
                    }
                }
            }
        }
    }

    string GetConfigKey(WindowConfig config) => config.ProcessName.ToLowerInvariant();


    void ProcessWindowPositioningLogic()
    {
        if(activeProfile == null || isProgramActivityDisabled) return;
        if(!activeProfile.WindowConfigs.Any(c => c.IsEnabled && (c.ControlPosition || c.ControlSize))) return;

        handledWindowHandlesThisCycle.Clear();
        var configsForPositioning = new List<WindowConfig>(activeProfile.WindowConfigs.Where(c => c.IsEnabled));
        ProcessWindowConfigurations(configsForPositioning, handledWindowHandlesThisCycle, false, "ProcessWindows");
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
    public void Dispose() { timer?.Stop(); timer?.Dispose(); timer = null; Debug.WriteLine("WindowMonitorService Timer Disposed."); }
}