using System.Diagnostics;
using WindowPlacementManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowPlacementManager.Services;

public class WindowMonitorService : IDisposable
{
    readonly SettingsManager settingsManager;
    readonly WindowActionService windowActionService;
    AppSettingsData currentSettings;
    Profile activeProfile;
    System.Windows.Forms.Timer timer;
    bool isProgramActivityDisabled = true;

    readonly Dictionary<string, int> autoRelaunchLastKnownPids = new();
    readonly Dictionary<string, DateTime> autoRelaunchLastLaunchAttemptTime = new();
    const int MinSecondsBetweenLaunchAttempts = 15;

    readonly Dictionary<string, WindowConfig> pendingPositioningTasks = new();
    readonly object pendingPositioningLock = new object();

    public WindowMonitorService(SettingsManager settingsManager, WindowActionService windowActionService)
    {
        this.settingsManager = settingsManager;
        this.windowActionService = windowActionService;
    }

    public void LoadAndApplySettings() { currentSettings = settingsManager.LoadSettings(); isProgramActivityDisabled = currentSettings.DisableProgramActivity; if(isProgramActivityDisabled) ClearAllMonitoringState(); else if(activeProfile != null) ProcessAutoRelaunchAndPositioningState(initialSetup: true); }
    public string GetActiveProfileNameFromSettings() => settingsManager.LoadSettings().ActiveProfileName;

    public void UpdateActiveProfileReference(Profile liveProfile)
    {
        bool profileChanged = activeProfile?.Name != liveProfile?.Name || (activeProfile == null) != (liveProfile == null);
        activeProfile = liveProfile;
        Debug.WriteLine($"WMS: Active profile updated to '{liveProfile?.Name ?? "null"}'. Profile effectively changed: {profileChanged}");
        if(profileChanged) ClearAllMonitoringState();
        ProcessAutoRelaunchAndPositioningState(initialSetup: profileChanged);
    }

    public void SetPositioningActive(bool isActive)
    {
        bool wasProgramDisabled = isProgramActivityDisabled;
        isProgramActivityDisabled = !isActive;
        Debug.WriteLine($"WMS: Program activity active: {isActive}. Was disabled: {wasProgramDisabled}");
        if(isProgramActivityDisabled) ClearAllMonitoringState();
        else if(wasProgramDisabled && activeProfile != null)
            ProcessAutoRelaunchAndPositioningState(initialSetup: true);
    }

    void ClearAllMonitoringState()
    {
        Debug.WriteLine("WMS: Clearing all monitoring state.");
        autoRelaunchLastKnownPids.Clear();
        autoRelaunchLastLaunchAttemptTime.Clear();
        lock(pendingPositioningLock) pendingPositioningTasks.Clear();
    }

    public void InitializeTimer() { if(timer != null) return; timer = new System.Windows.Forms.Timer { Interval = 1500 }; timer.Tick += Timer_Tick; timer.Start(); Debug.WriteLine("WMS Timer Initialized (1.5s)."); }

    public void NotifyAppLaunched(WindowConfig config) { if(config == null || isProgramActivityDisabled) return; if(!config.ControlPosition && !config.ControlSize) return; string configKey = GetConfigKeyForPositioning(config); lock(pendingPositioningLock) pendingPositioningTasks[configKey] = config; Debug.WriteLine($"WMS: Queued '{config.ProcessName}' for positioning."); }
    string GetConfigKeyForPositioning(WindowConfig config) => config.ProcessName.ToLowerInvariant() + (config.ExecutablePath ?? "");
    string GetConfigKeyForAutoRL(WindowConfig config) => config.ProcessName.ToLowerInvariant() + (config.ExecutablePath ?? "");


    void Timer_Tick(object sender, EventArgs e)
    {
        if(isProgramActivityDisabled || activeProfile == null) return;
        ProcessAutoRelaunchAndPositioningState(initialSetup: false);
    }

    void ProcessAutoRelaunchAndPositioningState(bool initialSetup)
    {
        if(isProgramActivityDisabled || activeProfile == null) return;

        var configsForAutoRL = activeProfile.WindowConfigs.Where(c => c.IsEnabled && c.AutoRelaunchEnabled).ToList();
        if(configsForAutoRL.Any() || autoRelaunchLastKnownPids.Any())
        {
            foreach(var config in configsForAutoRL)
            {
                string configKey = GetConfigKeyForAutoRL(config);
                WindowEnumerationService.FoundWindowInfo foundWindow = WindowEnumerationService.FindMostSuitableWindow(config);
                Process currentProcess = foundWindow?.GetProcess();

                bool isCurrentlyRunning = (currentProcess != null && !currentProcess.HasExited);

                if(isCurrentlyRunning)
                {
                    autoRelaunchLastKnownPids[configKey] = currentProcess.Id;
                    autoRelaunchLastLaunchAttemptTime.Remove(configKey);
                    Debug.WriteLineIf(initialSetup, $"WMS: AutoRL: '{config.ProcessName}' (PID: {currentProcess.Id}) is running. Monitoring.");
                }
                else
                {
                    if(autoRelaunchLastLaunchAttemptTime.TryGetValue(configKey, out DateTime lastAttemptTime) &&
                        (DateTime.UtcNow - lastAttemptTime).TotalSeconds < MinSecondsBetweenLaunchAttempts)
                    {
                        Debug.WriteLine($"WMS: AutoRL: '{config.ProcessName}' launch attempt is recent. Waiting for it to settle or UAC. Cooldown active.");
                        continue;
                    }

                    bool specificInstanceExited = false;
                    if(autoRelaunchLastKnownPids.TryGetValue(configKey, out int lastPid))
                    {
                        try { using Process p = Process.GetProcessById(lastPid); { if(p.HasExited) specificInstanceExited = true; } }
                        catch { specificInstanceExited = true; }
                    }

                    if(initialSetup || specificInstanceExited || !autoRelaunchLastKnownPids.ContainsKey(configKey))
                    {
                        Debug.WriteLine($"WMS: AutoRL: '{config.ProcessName}' (key: {configKey}) needs launch (Initial: {initialSetup}, Exited: {specificInstanceExited}, Not known: {!autoRelaunchLastKnownPids.ContainsKey(configKey)}).");

                        autoRelaunchLastLaunchAttemptTime[configKey] = DateTime.UtcNow;

                        if(windowActionService.LaunchApp(config, supressErrorDialogs: true))
                        {
                            Debug.WriteLine($"WMS: AutoRL: Launch initiated for '{config.ProcessName}'.");
                            autoRelaunchLastKnownPids.Remove(configKey);
                        }
                        else
                        {
                            Debug.WriteLine($"WMS: AutoRL: Launch FAILED for '{config.ProcessName}'. Cooldown will apply.");
                        }
                    }
                }
                currentProcess?.Dispose();
            }

            var activeRLConfigKeys = new HashSet<string>(configsForAutoRL.Select(GetConfigKeyForAutoRL));
            var pidsToRemove = autoRelaunchLastKnownPids.Keys.Where(k => !activeRLConfigKeys.Contains(k)).ToList();
            foreach(var keyToRemove in pidsToRemove)
            {
                autoRelaunchLastKnownPids.Remove(keyToRemove);
                autoRelaunchLastLaunchAttemptTime.Remove(keyToRemove);
                Debug.WriteLine($"WMS: AutoRL: Stopped tracking PID for config key '{keyToRemove}' (no longer AutoRL enabled).");
            }
        }


        ProcessPendingPositioningTasks();
    }


    void ProcessPendingPositioningTasks() { if(!pendingPositioningTasks.Any()) return; List<string> completedConfigKeys = new List<string>(); Dictionary<string, WindowConfig> currentPendingTasksSnapshot; lock(pendingPositioningLock) { currentPendingTasksSnapshot = new Dictionary<string, WindowConfig>(pendingPositioningTasks); } if(!currentPendingTasksSnapshot.Any()) return; Debug.WriteLine($"WMS: Processing {currentPendingTasksSnapshot.Count} pending positioning tasks."); foreach(var entry in currentPendingTasksSnapshot) { string configKey = entry.Key; WindowConfig config = entry.Value; if(config == null) { Debug.WriteLine($"WMS: ERROR - Null config in pending tasks for key '{configKey}'."); completedConfigKeys.Add(configKey); continue; } Debug.WriteLine($"WMS: Positioning: Checking '{config.ProcessName}' (key: {configKey})"); WindowEnumerationService.FoundWindowInfo foundWindow = null; try { foundWindow = WindowEnumerationService.FindMostSuitableWindow(config); } catch(Exception ex) { Debug.WriteLine($"WMS: Positioning: Exception FindMostSuitableWindow for '{config.ProcessName}': {ex.Message}"); continue; } if(foundWindow != null && foundWindow.HWnd != IntPtr.Zero) { Debug.WriteLine($"WMS: Positioning: Found '{config.ProcessName}'. Applying settings ONCE."); if(ApplySettingsToWindow(config, foundWindow.HWnd)) { completedConfigKeys.Add(configKey); } } else { Debug.WriteLine($"WMS: Positioning: Window for '{config.ProcessName}' not yet found."); } } if(completedConfigKeys.Any()) { lock(pendingPositioningLock) { foreach(string key in completedConfigKeys) pendingPositioningTasks.Remove(key); } Debug.WriteLine($"WMS: Positioning: Removed {completedConfigKeys.Count} tasks."); } }
    bool ApplySettingsToWindow(WindowConfig config, IntPtr hWnd) { if(config == null || hWnd == IntPtr.Zero) return true; if(!config.ControlPosition && !config.ControlSize) return true; if(!Native.GetWindowRect(hWnd, out RECT currentRect)) { Debug.WriteLine($"WMS: Pos: Failed GetWindowRect for {config.ProcessName}"); return false; } int targetX = config.ControlPosition ? config.TargetX : currentRect.Left; int targetY = config.ControlPosition ? config.TargetY : currentRect.Top; int targetWidth = config.ControlSize ? config.TargetWidth : currentRect.Width; int targetHeight = config.ControlSize ? config.TargetHeight : currentRect.Height; if(targetWidth <= 0 || targetHeight <= 0) { Debug.WriteLine($"WMS: Pos: Invalid target dims for {config.ProcessName}"); return true; } Native.MoveWindow(hWnd, targetX, targetY, targetWidth, targetHeight, true); Debug.WriteLine($"WMS: Pos: Applied ONCE to {config.ProcessName}"); return true; }
    public void TestProfileLayout(Profile profileToTest) { if(profileToTest == null || !profileToTest.WindowConfigs.Any() || isProgramActivityDisabled) return; Debug.WriteLine($"WMS: TestProfileLayout for '{profileToTest.Name}'"); foreach(var config in profileToTest.WindowConfigs.Where(c => c.IsEnabled)) { var foundWindow = WindowEnumerationService.FindMostSuitableWindow(config); if(foundWindow?.HWnd != IntPtr.Zero) ApplySettingsToWindow(config, foundWindow.HWnd); else Debug.WriteLine($"WMS: Test: Win not found for '{config.ProcessName}'."); } Debug.WriteLine("WMS: TestProfileLayout completed."); }
    public void Dispose() { timer?.Stop(); timer?.Dispose(); timer = null; Debug.WriteLine("WMS Timer Disposed."); }
}