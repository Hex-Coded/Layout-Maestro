using System.Diagnostics;
using WindowPlacementManager.Models;

namespace WindowPlacementManager.Services;

public class WindowActionService
{
    public WindowEnumerationService.FoundWindowInfo FindManagedWindow(WindowConfig config) => (config == null || string.IsNullOrWhiteSpace(config.ProcessName)) ? null : WindowEnumerationService.FindMostSuitableWindow(config);

    string GetLaunchPath(WindowConfig config) => !string.IsNullOrWhiteSpace(config.ExecutablePath) ? config.ExecutablePath : config.ProcessName;

    ProcessStartInfo CreateProcessStartInfo(string pathToLaunch, bool launchAsAdmin)
    {
        var startInfo = new ProcessStartInfo(pathToLaunch) { UseShellExecute = true };
        if(launchAsAdmin) startInfo.Verb = "runas";
        return startInfo;
    }

    bool HandleLaunchException(Exception ex, string pathToLaunch, bool wasAdminLaunchAttempt)
    {
        if(ex is System.ComponentModel.Win32Exception win32Ex)
        {
            if(win32Ex.NativeErrorCode == 1223 && wasAdminLaunchAttempt)
            {
                MessageBox.Show($"Launching '{pathToLaunch}' as Administrator was cancelled.", "Launch Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            if(win32Ex.NativeErrorCode == 740 && !wasAdminLaunchAttempt)
            {
                MessageBox.Show($"Failed to launch '{pathToLaunch}'.\nThe application requires administrator privileges to start, but 'Launch as Admin' was not checked for this configuration.", "Launch Error - Elevation Required", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
        MessageBox.Show($"Failed to launch '{pathToLaunch}'.\nError: {ex.Message}", "Launch Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        return false;
    }
    public bool LaunchApp(WindowConfig config, bool supressErrorDialogs = false)
    {
        string pathToLaunch = GetLaunchPath(config);
        if(string.IsNullOrWhiteSpace(pathToLaunch))
        {
            Debug.WriteLine($"Cannot launch '{config.ProcessName}': No ExecutablePath/ProcessName.");
            if(!supressErrorDialogs)
                MessageBox.Show($"Cannot launch application for '{config.ProcessName}'.\nNo executable path or process name is configured.", "Launch Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }

        try
        {
            ProcessStartInfo startInfo = CreateProcessStartInfo(pathToLaunch, config.LaunchAsAdmin);
            Process p = Process.Start(startInfo);
            Debug.WriteLine($"Launched '{pathToLaunch}' (Admin: {config.LaunchAsAdmin}). PID: {p?.Id}");
            p?.Dispose();
            return true;
        }
        catch(System.ComponentModel.Win32Exception ex) when(ex.NativeErrorCode == 1223 && config.LaunchAsAdmin)
        {
            Debug.WriteLine($"Launch of '{pathToLaunch}' as Admin cancelled by user (UAC).");
            if(!supressErrorDialogs)
                MessageBox.Show($"Launching '{pathToLaunch}' as Administrator was cancelled.", "Launch Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }
        catch(Exception ex)
        {
            return HandleLaunchException(ex, pathToLaunch, config.LaunchAsAdmin, supressErrorDialogs);
        }
    }

    bool HandleLaunchException(Exception ex, string pathToLaunch, bool wasAdminLaunchAttempt, bool supressErrorDialogs = false)
    {
        if(ex is System.ComponentModel.Win32Exception win32Ex)
        {
            if(win32Ex.NativeErrorCode == 1223 && wasAdminLaunchAttempt)
            {
                if(!supressErrorDialogs) MessageBox.Show($"Launching '{pathToLaunch}' as Administrator was cancelled.", "Launch Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            if(win32Ex.NativeErrorCode == 740 && !wasAdminLaunchAttempt)
            {
                if(!supressErrorDialogs) MessageBox.Show($"Failed to launch '{pathToLaunch}'.\nThe application requires administrator privileges to start, but 'Launch as Admin' was not checked for this configuration.", "Launch Error - Elevation Required", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
        if(!supressErrorDialogs) MessageBox.Show($"Failed to launch '{pathToLaunch}'.\nError: {ex.Message}", "Launch Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        else Debug.WriteLine($"Failed to launch '{pathToLaunch}' (dialogs suppressed). Error: {ex.Message}");
        return false;
    }


    public async Task ProcessAllAppsInProfile(Profile profile, bool launchIfNotRunning, bool bringToForegroundIfRunning, bool closeIfRunning, bool forceKillIfNotClosed = false, int closeGracePeriodMs = 2000, int delayBetweenLaunchesMs = 1000)
    {
        if(profile == null) { Debug.WriteLine("ProcessAllAppsInProfile: Profile is null."); return; }
        if(!profile.WindowConfigs.Any(c => c.IsEnabled)) { Debug.WriteLine($"ProcessAllAppsInProfile: Profile '{profile.Name}' has no enabled configs."); return; }

        Debug.WriteLine($"Processing profile '{profile.Name}': Launch={launchIfNotRunning}, Focus={bringToForegroundIfRunning}, Close={closeIfRunning}");

        List<WindowConfig> configsToProcess = profile.WindowConfigs.Where(c => c.IsEnabled).ToList();

        foreach(var config in configsToProcess)
        {
            Debug.WriteLine($"ProcessAllAppsInProfile: Processing config for '{config.ProcessName}'");
            var windowInfo = FindManagedWindow(config);
            Process process = windowInfo?.GetProcess();

            try
            {
                if(process != null && !process.HasExited)
                {
                    if(closeIfRunning)
                    {
                        Debug.WriteLine($"ProcessAllAppsInProfile: Closing running app: '{config.ProcessName}'");
                        if(!CloseApp(config, forceKillIfNotClosed, closeGracePeriodMs))
                            Debug.WriteLine($"ProcessAllAppsInProfile: Failed to close '{config.ProcessName}'.");
                        if(delayBetweenLaunchesMs > 0) await Task.Delay(delayBetweenLaunchesMs / 2);
                    }
                    else if(bringToForegroundIfRunning)
                    {
                        Debug.WriteLine($"ProcessAllAppsInProfile: Focusing running app: '{config.ProcessName}' (hWnd:{windowInfo.HWnd})");
                        if(!BringWindowToForeground(windowInfo.HWnd))
                            Debug.WriteLine($"ProcessAllAppsInProfile: Failed to focus '{config.ProcessName}'.");
                    }
                    else
                        Debug.WriteLine($"ProcessAllAppsInProfile: App '{config.ProcessName}' (PID: {process.Id}) is running. No launch/focus/close specified.");
                }
                else
                {
                    if(launchIfNotRunning)
                    {
                        Debug.WriteLine($"ProcessAllAppsInProfile: Launching missing app: '{config.ProcessName}'");
                        if(!LaunchApp(config, supressErrorDialogs: false))
                            Debug.WriteLine($"ProcessAllAppsInProfile: Failed to launch '{config.ProcessName}'.");
                        else
                        if(delayBetweenLaunchesMs > 0)
                        {
                            Debug.WriteLine($"ProcessAllAppsInProfile: Delaying {delayBetweenLaunchesMs}ms after launching '{config.ProcessName}'.");
                            await Task.Delay(delayBetweenLaunchesMs);
                        }
                    }
                    else
                        Debug.WriteLine($"ProcessAllAppsInProfile: App '{config.ProcessName}' is not running. No launch action specified.");
                }
            }
            finally { process?.Dispose(); }
        }
        Debug.WriteLine($"Finished processing profile '{profile.Name}'.");
    }

    public bool BringWindowToForeground(IntPtr hWnd)
    {
        if(hWnd == IntPtr.Zero) return false;
        try
        {
            if(Native.IsIconic(hWnd)) Native.ShowWindow(hWnd, Native.SW_RESTORE); else Native.ShowWindow(hWnd, Native.SW_SHOWNORMAL);
            return Native.SetForegroundWindow(hWnd);
        }
        catch(Exception ex) { Debug.WriteLine($"Error bringing window {hWnd} to foreground: {ex.Message}"); return false; }
    }

    public bool ActivateOrLaunchApp(WindowConfig config)
    {
        if(!config.IsEnabled) { Debug.WriteLine($"Skipping disabled config: '{config.ProcessName}'"); return true; }
        var windowInfo = FindManagedWindow(config);
        if(windowInfo != null && windowInfo.HWnd != IntPtr.Zero)
        {
            Debug.WriteLine($"App '{config.ProcessName}' (hWnd:{windowInfo.HWnd}) found. Focusing.");
            return BringWindowToForeground(windowInfo.HWnd);
        }
        else
        {
            Debug.WriteLine($"App '{config.ProcessName}' not found. Launching.");
            return LaunchApp(config);
        }
    }

    bool AttemptGracefulShutdown(Process processToClose, int gracePeriodMs, string appIdentifier)
    {
        Debug.WriteLine($"CloseApp: Attempting graceful shutdown for '{appIdentifier}'.");
        bool closeSignalSent = false;
        try { if(processToClose.HasExited) return true; closeSignalSent = processToClose.CloseMainWindow(); }
        catch(InvalidOperationException) { return true; }

        if(!closeSignalSent && !processToClose.HasExited)
            Debug.WriteLine($"CloseApp: CloseMainWindow returned false or failed for '{appIdentifier}'. The process might not have a main window or is unresponsive.");

        if(processToClose.WaitForExit(gracePeriodMs))
        {
            Debug.WriteLine($"CloseApp: '{appIdentifier}' exited (grace period check).");
            return true;
        }
        if(!processToClose.HasExited) Debug.WriteLine($"CloseApp: '{appIdentifier}' did not exit within grace period ({gracePeriodMs}ms).");
        return processToClose.HasExited;
    }

    void ForceKillProcess(Process processToClose, string appIdentifier)
    {
        try
        {
            if(!processToClose.HasExited)
            {
                Debug.WriteLine($"CloseApp: Forcing kill for '{appIdentifier}'.");
                processToClose.Kill();
            }
        }
        catch(Exception killEx) { Debug.WriteLine($"CloseApp: Error during force kill for '{appIdentifier}': {killEx.Message}"); }
    }

    public bool CloseApp(WindowConfig config, bool forceKillIfNotClosed = false, int gracePeriodMs = 2000)
    {
        if(!config.IsEnabled)
        {
            Debug.WriteLine($"CloseApp: Skipping disabled config '{config.ProcessName}'.");
            return true;
        }

        var windowInfo = FindManagedWindow(config);
        Process processToClose = windowInfo?.GetProcess();

        if(processToClose == null || processToClose.HasExited)
        {
            Debug.WriteLine($"CloseApp: Process for '{config.ProcessName}' not found or already exited.");
            return true;
        }

        string appIdentifier = $"{config.ProcessName} (PID: {processToClose.Id})";
        try
        {
            if(processToClose.HasExited) return true;
            if(AttemptGracefulShutdown(processToClose, gracePeriodMs, appIdentifier)) return true;

            if(forceKillIfNotClosed)
            {
                if(processToClose.HasExited) return true;
                ForceKillProcess(processToClose, appIdentifier);
                return processToClose.WaitForExit(200) || processToClose.HasExited;
            }
            return processToClose.HasExited;
        }
        catch(InvalidOperationException ex)
        {
            Debug.WriteLine($"CloseApp: Process '{appIdentifier}' already exited before or during close attempt. {ex.Message}");
            return true;
        }
        catch(Exception ex)
        {
            Debug.WriteLine($"CloseApp: General error during close attempt for '{appIdentifier}': {ex.Message}");
            if(forceKillIfNotClosed && !processToClose.HasExited)
            {
                ForceKillProcess(processToClose, appIdentifier);
                return processToClose.WaitForExit(200) || processToClose.HasExited;
            }
            return processToClose.HasExited;
        }
        finally { processToClose?.Dispose(); }
    }

    void HandleRunningApp(WindowConfig config, WindowEnumerationService.FoundWindowInfo windowInfo, Process process, bool bringToForegroundIfRunning, bool closeIfRunning, bool forceKillIfNotClosed, int closeGracePeriodMs)
    {
        if(closeIfRunning)
        {
            Debug.WriteLine($"Closing running app as part of profile action: '{config.ProcessName}'");
            if(!CloseApp(config, forceKillIfNotClosed, closeGracePeriodMs))
                Debug.WriteLine($"Failed to close '{config.ProcessName}' during profile action.");
        }
        else if(bringToForegroundIfRunning && windowInfo != null && windowInfo.HWnd != IntPtr.Zero)
        {
            Debug.WriteLine($"Focusing running app: '{config.ProcessName}' (hWnd:{windowInfo.HWnd})");
            if(!BringWindowToForeground(windowInfo.HWnd))
                Debug.WriteLine($"Failed to focus '{config.ProcessName}' (hWnd:{windowInfo.HWnd}).");
        }
        else
            Debug.WriteLine($"App '{config.ProcessName}' (PID: {process.Id}) is running. No focus/close action specified by profile settings.");
    }

    void HandleNotRunningApp(WindowConfig config, bool launchIfNotRunning)
    {
        if(launchIfNotRunning)
        {
            Debug.WriteLine($"Launching missing app: '{config.ProcessName}'");
            if(!LaunchApp(config))
                Debug.WriteLine($"Failed to launch '{config.ProcessName}'.");
        }
        else
            Debug.WriteLine($"App '{config.ProcessName}' is not running. No launch action specified by profile settings.");
    }

    void ProcessSingleAppInProfile(WindowConfig config, bool launchIfNotRunning, bool bringToForegroundIfRunning, bool closeIfRunning, bool forceKillIfNotClosed, int closeGracePeriodMs)
    {
        var windowInfo = FindManagedWindow(config);
        Process process = windowInfo?.GetProcess();
        try
        {
            if(process != null && !process.HasExited)
                HandleRunningApp(config, windowInfo, process, bringToForegroundIfRunning, closeIfRunning, forceKillIfNotClosed, closeGracePeriodMs);
            else
                HandleNotRunningApp(config, launchIfNotRunning);
        }
        finally { process?.Dispose(); }
    }

    public void ProcessAllAppsInProfile(Profile profile, bool launchIfNotRunning, bool bringToForegroundIfRunning, bool closeIfRunning, bool forceKillIfNotClosed = false, int closeGracePeriodMs = 2000)
    {
        if(profile == null) { Debug.WriteLine("ProcessAllAppsInProfile: Profile is null."); return; }
        if(!profile.WindowConfigs.Any(c => c.IsEnabled)) { Debug.WriteLine($"ProcessAllAppsInProfile: Profile '{profile.Name}' has no enabled configs."); return; }

        Debug.WriteLine($"Processing profile '{profile.Name}': Launch={launchIfNotRunning}, Focus={bringToForegroundIfRunning}, Close={closeIfRunning}");
        foreach(var config in profile.WindowConfigs.Where(c => c.IsEnabled))
            ProcessSingleAppInProfile(config, launchIfNotRunning, bringToForegroundIfRunning, closeIfRunning, forceKillIfNotClosed, closeGracePeriodMs);
        Debug.WriteLine($"Finished processing profile '{profile.Name}'.");
    }
}