using System.Diagnostics;
using WindowPlacementManager;
using WindowPlacementManager.Models;
using WindowPlacementManager.Services;

public class WindowActionService
{
    public static event Action<WindowConfig> AppLaunchedForPositioning;

    public WindowEnumerationService.FoundWindowInfo FindManagedWindow(WindowConfig config) =>
        (config == null || (string.IsNullOrWhiteSpace(config.ProcessName) && string.IsNullOrWhiteSpace(config.ExecutablePath)))
            ? null
            : WindowEnumerationService.FindMostSuitableWindow(config);

    string GetLaunchPath(WindowConfig config) =>
        !string.IsNullOrWhiteSpace(config.ExecutablePath) ? config.ExecutablePath : config.ProcessName;

    ProcessStartInfo CreateProcessStartInfo(string pathToLaunch, bool launchAsAdmin)
    {
        var startInfo = new ProcessStartInfo(pathToLaunch) { UseShellExecute = true };
        if(launchAsAdmin) startInfo.Verb = "runas";
        return startInfo;
    }


    public LaunchAttemptResult LaunchApp(WindowConfig config, bool supressErrorDialogs = false)
    {
        if(config == null)
        {
            Debug.WriteLine("LaunchApp: WindowConfig is null.");
            if(!supressErrorDialogs) MessageBox.Show("Cannot launch application: Configuration data is missing.", "Launch Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return LaunchAttemptResult.ConfigError;
        }

        string pathToLaunch = GetLaunchPath(config);
        if(string.IsNullOrWhiteSpace(pathToLaunch))
        {
            Debug.WriteLine($"Cannot launch '{config.ProcessName ?? "Unknown"}': No ExecutablePath/ProcessName.");
            if(!supressErrorDialogs)
                MessageBox.Show($"Cannot launch application for '{config.ProcessName ?? "Unknown"}'.\nNo executable path or process name is configured.", "Launch Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return LaunchAttemptResult.ConfigError;
        }

        try
        {
            ProcessStartInfo startInfo = CreateProcessStartInfo(pathToLaunch, config.LaunchAsAdmin);
            using Process p = Process.Start(startInfo);

            Debug.WriteLine($"Launch attempt for '{config.ProcessName ?? "Unknown"}' (Path: '{pathToLaunch}', Admin: {config.LaunchAsAdmin}). Initial Process.Start returned PID: {p?.Id.ToString() ?? "N/A"}");

            AppLaunchedForPositioning?.Invoke(config);
            Debug.WriteLine($"LaunchApp: Raised AppLaunchedForPositioning event for '{config.ProcessName ?? "Unknown"}'.");

            return config.LaunchAsAdmin ? LaunchAttemptResult.Success : LaunchAttemptResult.SuccessNoAdminNeeded;
        }
        catch(System.ComponentModel.Win32Exception ex) when(ex.NativeErrorCode == 1223 && config.LaunchAsAdmin)
        {
            Debug.WriteLine($"Launch of '{pathToLaunch}' as Admin cancelled by user (UAC).");
            if(!supressErrorDialogs)
                MessageBox.Show($"Launching '{pathToLaunch}' as Administrator was cancelled.", "Launch Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return LaunchAttemptResult.UacCancelled;
        }
        catch(System.ComponentModel.Win32Exception ex) when(ex.NativeErrorCode == 740 && !config.LaunchAsAdmin)
        {
            Debug.WriteLine($"Launch of '{pathToLaunch}' failed - Elevation required but 'LaunchAsAdmin' not checked.");
            if(!supressErrorDialogs)
                MessageBox.Show($"Failed to launch '{pathToLaunch}'.\nThe application requires administrator privileges to start, but 'Launch as Admin' was not checked for this configuration.", "Launch Error - Elevation Required", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return LaunchAttemptResult.ElevationRequiredButNotRequested;
        }
        catch(Exception ex)
        {
            Debug.WriteLine($"Failed to launch '{pathToLaunch}' (Dialogs suppressed: {supressErrorDialogs}). Error: {ex.Message}");
            if(!supressErrorDialogs)
                MessageBox.Show($"Failed to launch '{pathToLaunch}'.\nError: {ex.Message}", "Launch Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return LaunchAttemptResult.Failed;
        }
    }

    public bool BringWindowToForeground(IntPtr hWnd)
    {
        if(hWnd == IntPtr.Zero) return false;
        try
        {
            if(Native.IsIconic(hWnd)) Native.ShowWindow(hWnd, Native.SW_RESTORE);
            else Native.ShowWindow(hWnd, Native.SW_SHOWNORMAL);
            return Native.SetForegroundWindow(hWnd);
        }
        catch(Exception ex)
        {
            Debug.WriteLine($"Error bringing window {hWnd} to foreground: {ex.Message}");
            return false;
        }
    }

    public bool BringWindowToBackground(IntPtr hWnd)
    {
        if(hWnd == IntPtr.Zero) return false;
        try
        {
            if(!Native.IsIconic(hWnd)) 
                Native.ShowWindow(hWnd, Native.SW_MINIMIZE);
            return Native.SetForegroundWindow(hWnd);
        }
        catch(Exception ex)
        {
            Debug.WriteLine($"Error bringing window {hWnd} to foreground: {ex.Message}");
            return false;
        }
    }

    public bool ActivateOrLaunchApp(WindowConfig config)
    {
        if(config == null) return false;
        if(!config.IsEnabled)
        {
            Debug.WriteLine($"Skipping disabled config: '{config.ProcessName ?? "Unknown"}'");
            return true;
        }

        var windowInfo = FindManagedWindow(config);
        if(windowInfo != null && windowInfo.HWnd != IntPtr.Zero)
        {
            Debug.WriteLine($"App '{config.ProcessName ?? "Unknown"}' (hWnd:{windowInfo.HWnd}) found. Focusing.");
            return BringWindowToForeground(windowInfo.HWnd);
        }
        else
        {
            Debug.WriteLine($"App '{config.ProcessName ?? "Unknown"}' not found. Launching.");
            LaunchAttemptResult result = LaunchApp(config, supressErrorDialogs: false);
            return result == LaunchAttemptResult.Success || result == LaunchAttemptResult.SuccessNoAdminNeeded;
        }
    }

    bool AttemptGracefulShutdown(Process processToClose, int gracePeriodMs, string appIdentifier)
    {
        Debug.WriteLine($"CloseApp: Attempting graceful shutdown for '{appIdentifier}'.");
        bool closeSignalSent = false;
        try
        {
            if(processToClose.HasExited) return true;
            closeSignalSent = processToClose.CloseMainWindow();
        }
        catch(InvalidOperationException) { return true; }

        if(!closeSignalSent && !processToClose.HasExited)
            Debug.WriteLine($"CloseApp: CloseMainWindow returned false or failed for '{appIdentifier}'. Process might not have a standard window or is unresponsive.");

        if(processToClose.WaitForExit(gracePeriodMs))
        {
            Debug.WriteLine($"CloseApp: '{appIdentifier}' exited (grace period check).");
            return true;
        }

        if(!processToClose.HasExited)
            Debug.WriteLine($"CloseApp: '{appIdentifier}' did not exit within grace period ({gracePeriodMs}ms).");
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
        if(config == null) return false;
        if(!config.IsEnabled)
        {
            Debug.WriteLine($"CloseApp: Skipping disabled config '{config.ProcessName ?? "Unknown"}'.");
            return true;
        }

        var windowInfo = FindManagedWindow(config);
        Process processToClose = null;
        try
        {
            processToClose = windowInfo?.GetProcess();

            if(processToClose == null || processToClose.HasExited)
            {
                Debug.WriteLine($"CloseApp: Process for '{config.ProcessName ?? "Unknown"}' not found or already exited.");
                return true;
            }

            string appIdentifier = $"{config.ProcessName ?? "Unknown"} (PID: {processToClose.Id})";

            if(processToClose.HasExited) return true;
            if(AttemptGracefulShutdown(processToClose, gracePeriodMs, appIdentifier)) return true;

            if(forceKillIfNotClosed)
            {
                if(processToClose.HasExited) return true;
                ForceKillProcess(processToClose, appIdentifier);
                return processToClose.WaitForExit(500) || processToClose.HasExited;
            }
            return processToClose.HasExited;
        }
        catch(InvalidOperationException ex)
        {
            Debug.WriteLine($"CloseApp: Process '{config.ProcessName ?? "Unknown"}' (PID: {processToClose?.Id.ToString() ?? "N/A"}) likely already exited (InvalidOperationException). {ex.Message}");
            return true;
        }
        catch(Exception ex)
        {
            Debug.WriteLine($"CloseApp: General error during close attempt for '{config.ProcessName ?? "Unknown"}' (PID: {processToClose?.Id.ToString() ?? "N/A"}): {ex.Message}");
            if(forceKillIfNotClosed && processToClose != null && !processToClose.HasExited)
            {
                try
                {
                    ForceKillProcess(processToClose, $"{config.ProcessName ?? "Unknown"} (PID: {processToClose.Id})");
                    return processToClose.WaitForExit(500) || processToClose.HasExited;
                }
                catch(Exception killEx) { Debug.WriteLine($"CloseApp: Error during final kill attempt: {killEx.Message}"); }
            }
            return false;
        }
        finally { processToClose?.Dispose(); }
    }

    public async Task ProcessAllAppsInProfile(
       Profile profile,
       bool launchIfNotRunning,
       bool bringToForegroundIfRunning,
       bool closeIfRunning,
       bool supressErrorDialogsForBatch = true,
       bool forceKillIfNotClosed = false,
       bool minimizeIfFound = false,
       int closeGracePeriodMs = 2000,
       int defaultDelayMs = 250,
       int adminLaunchDelayMs = 750)
    {
        if(profile == null)
        {
            Debug.WriteLine("ProcessAllAppsInProfile: Profile is null. Aborting.");
            return;
        }
        if(!profile.WindowConfigs.Any(c => c.IsEnabled))
        {
            Debug.WriteLine($"ProcessAllAppsInProfile: Profile '{profile.Name}' has no enabled configurations. Aborting.");
            return;
        }

        Debug.WriteLine($"ProcessAllAppsInProfile: Starting for profile '{profile.Name}'. LaunchIfNotRunning={launchIfNotRunning}, BringToForeground={bringToForegroundIfRunning}, CloseIfRunning={closeIfRunning}, SuppressDialogsForBatch={supressErrorDialogsForBatch}");

        List<WindowConfig> configsToProcess = profile.WindowConfigs.Where(c => c.IsEnabled).ToList();
        int launchedCount = 0;
        int alreadyRunningOrFocusedCount = 0;
        int closeAttemptedCount = 0;
        int errorCount = 0;

        foreach(var config in configsToProcess)
        {
            string configIdentifier = config.ProcessName ?? config.ExecutablePath ?? "Unknown Config";
            if(string.IsNullOrWhiteSpace(configIdentifier) || configIdentifier == "Unknown Config")
            {
                Debug.WriteLine($"ProcessAllAppsInProfile: Skipping a config with no ProcessName or ExecutablePath.");
                continue;
            }
            Debug.WriteLine($"ProcessAllAppsInProfile: ----- Processing config for '{configIdentifier}' -----");

            WindowEnumerationService.FoundWindowInfo windowInfo = null;
            Process process = null;

            try
            {
                windowInfo = FindManagedWindow(config);

                string processIdStr = "N/A";
                string processHasExitedStr = "N/A";
                IntPtr foundHWnd = IntPtr.Zero;

                if(windowInfo != null)
                {
                    foundHWnd = windowInfo.HWnd;
                    if(windowInfo.HWnd != IntPtr.Zero)
                    {
                        try
                        {
                            var tempProcess = windowInfo.GetProcess();
                            if(tempProcess != null)
                            {
                                processIdStr = tempProcess.Id.ToString();
                                bool tempProcessHasExited = false;
                                try
                                {
                                    tempProcessHasExited = tempProcess.HasExited;
                                    processHasExitedStr = tempProcessHasExited.ToString();
                                }
                                catch(Exception ex)
                                {
                                    processHasExitedStr = $"Unknown ({ex.GetType().Name})";
                                    Debug.WriteLine($"ProcessAllAppsInProfile: Exception checking HasExited for '{configIdentifier}' PID {processIdStr}: {ex.Message}");
                                    tempProcessHasExited = true;
                                }

                                if(!tempProcessHasExited)
                                {
                                    process = tempProcess;
                                }
                                else
                                {
                                    Debug.WriteLine($"ProcessAllAppsInProfile: Found process for '{configIdentifier}' (PID {processIdStr}) but it has already exited.");
                                    tempProcess.Dispose();
                                }
                            }
                            else
                            {
                                Debug.WriteLine($"ProcessAllAppsInProfile: windowInfo.GetProcess() returned null for '{configIdentifier}' (HWnd: {windowInfo.HWnd}).");
                            }
                        }
                        catch(Exception pEx)
                        {
                            Debug.WriteLine($"ProcessAllAppsInProfile: Exception during windowInfo.GetProcess() for '{configIdentifier}' (HWnd: {windowInfo.HWnd}): {pEx.Message}");
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"ProcessAllAppsInProfile: FindManagedWindow for '{configIdentifier}' returned non-null windowInfo but HWnd was Zero.");
                    }
                }
                else
                {
                    Debug.WriteLine($"ProcessAllAppsInProfile: FindManagedWindow for '{configIdentifier}' returned NULL.");
                }
                Debug.WriteLine($"ProcessAllAppsInProfile: Status for '{configIdentifier}': Found HWnd: {foundHWnd.ToString("X")}, Live PID: {process?.Id.ToString() ?? "N/A"} (Log details: PID '{processIdStr}', HasExited '{processHasExitedStr}')");


                if(process != null)
                {
                    alreadyRunningOrFocusedCount++;
                    Debug.WriteLine($"ProcessAllAppsInProfile: App '{configIdentifier}' (PID: {process.Id}) is considered RUNNING and ACCESSIBLE.");


                    if(minimizeIfFound)
                    {
                        if(!BringWindowToBackground(foundHWnd))
                        {
                            Debug.WriteLine($"ProcessAllAppsInProfile: Failed to focus '{configIdentifier}'.");
                        }
                    }
                    if(closeIfRunning)
                    {
                        closeAttemptedCount++;
                        Debug.WriteLine($"ProcessAllAppsInProfile: Attempting to CLOSE '{configIdentifier}'.");
                        if(!CloseApp(config, forceKillIfNotClosed, closeGracePeriodMs))
                        {
                            Debug.WriteLine($"ProcessAllAppsInProfile: Failed to close '{configIdentifier}'.");
                        }
                        int delayAfterCloseAttempt = defaultDelayMs / 2;
                        if(delayAfterCloseAttempt > 0) await Task.Delay(delayAfterCloseAttempt);
                    }
                    else if(bringToForegroundIfRunning)
                    {
                        Debug.WriteLine($"ProcessAllAppsInProfile: Attempting to FOCUS '{configIdentifier}' (HWnd: {foundHWnd.ToString("X")}).");
                        if(!BringWindowToForeground(foundHWnd))
                        {
                            Debug.WriteLine($"ProcessAllAppsInProfile: Failed to focus '{configIdentifier}'.");
                        }
                    }
                }
                else
                {
                    Debug.WriteLine($"ProcessAllAppsInProfile: App '{configIdentifier}' is considered NOT RUNNING or INACCESSIBLE.");
                    if(launchIfNotRunning)
                    {
                        Debug.WriteLine($"ProcessAllAppsInProfile: LaunchIfNotRunning is true. Attempting to LAUNCH '{configIdentifier}'.");

                        LaunchAttemptResult launchResult = LaunchApp(config, supressErrorDialogs: supressErrorDialogsForBatch);
                        Debug.WriteLine($"ProcessAllAppsInProfile: Launch attempt for '{configIdentifier}' completed with result: {launchResult}");

                        if(launchResult == LaunchAttemptResult.Success || launchResult == LaunchAttemptResult.SuccessNoAdminNeeded)
                        {
                            launchedCount++;
                            int currentDelay = config.LaunchAsAdmin ? adminLaunchDelayMs : defaultDelayMs;
                            if(currentDelay > 0)
                            {
                                Debug.WriteLine($"ProcessAllAppsInProfile: Delaying {currentDelay}ms after successfully initiating launch for '{configIdentifier}'.");
                                await Task.Delay(currentDelay);
                            }
                        }
                        else
                        {
                            Debug.WriteLine($"ProcessAllAppsInProfile: Launch of '{configIdentifier}' failed, was UAC cancelled, or other issue. Result: {launchResult}. No post-launch delay for this item.");
                            errorCount++;
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"ProcessAllAppsInProfile: LaunchIfNotRunning is false for '{configIdentifier}'. No launch action taken.");
                    }
                }
            }
            catch(Exception ex)
            {
                errorCount++;
                Debug.WriteLine($"ProcessAllAppsInProfile: UNEXPECTED Top-Level ERROR processing config '{configIdentifier}': {ex.Message}\nStack Trace: {ex.StackTrace}");
            }
            finally
            {
                process?.Dispose();
            }
        }

        Debug.WriteLine($"ProcessAllAppsInProfile: Finished processing profile '{profile.Name}'. Summary: Launched={launchedCount}, AlreadyRunning/FocusAttempts={alreadyRunningOrFocusedCount}, CloseAttempts={closeAttemptedCount}, Errors/Issues={errorCount}");
    }
}