using System.Diagnostics;
using WindowPlacementManager.Models;

namespace WindowPlacementManager.Services;

public class WindowActionService
{
    public WindowEnumerationService.FoundWindowInfo FindManagedWindow(WindowConfig config)
    {
        if(config == null || string.IsNullOrWhiteSpace(config.ProcessName)) return null;
        return WindowEnumerationService.FindMostSuitableWindow(config);
    }

    public bool LaunchApp(WindowConfig config)
    {
        string pathToLaunch = !string.IsNullOrWhiteSpace(config.ExecutablePath) ? config.ExecutablePath : config.ProcessName;
        if(string.IsNullOrWhiteSpace(pathToLaunch))
        {
            Debug.WriteLine($"Cannot launch '{config.ProcessName}': No ExecutablePath/ProcessName.");
            MessageBox.Show($"Cannot launch application for '{config.ProcessName}'.\nNo executable path or process name is configured.", "Launch Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(pathToLaunch) { UseShellExecute = true };
            if(config.LaunchAsAdmin) { startInfo.Verb = "runas"; }
            Process.Start(startInfo);
            Debug.WriteLine($"Launched '{pathToLaunch}' (Admin: {config.LaunchAsAdmin}).");
            return true;
        }
        catch(System.ComponentModel.Win32Exception ex)
        {
            if(ex.NativeErrorCode == 1223 && config.LaunchAsAdmin) { MessageBox.Show($"Launching '{pathToLaunch}' as Administrator was cancelled.", "Launch Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information); return false; }
            if(ex.NativeErrorCode == 740 && !config.LaunchAsAdmin) { MessageBox.Show($"Failed to launch '{pathToLaunch}'.\nThe application requires administrator privileges to start, but 'Launch as Admin' was not checked for this configuration.", "Launch Error - Elevation Required", MessageBoxButtons.OK, MessageBoxIcon.Error); return false; }
            MessageBox.Show($"Failed to launch '{pathToLaunch}'.\nError: {ex.Message}", "Launch Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return false;
        }
        catch(Exception ex) { MessageBox.Show($"Failed to launch '{pathToLaunch}'.\nError: {ex.Message}", "Launch Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return false; }
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
        if(windowInfo != null && windowInfo.HWnd != IntPtr.Zero) { Debug.WriteLine($"App '{config.ProcessName}' (hWnd:{windowInfo.HWnd}) found. Focusing."); return BringWindowToForeground(windowInfo.HWnd); }
        else { Debug.WriteLine($"App '{config.ProcessName}' not found. Launching."); return LaunchApp(config); }
    }


    public bool CloseApp(WindowConfig config, bool forceKillIfNotClosed = false, int gracePeriodMs = 2000)
    {
        if(!config.IsEnabled)
        {
            Debug.WriteLine($"CloseApp: Skipping disabled config '{config.ProcessName}'.");
            return true;
        }

        var windowInfo = FindManagedWindow(config);
        if(windowInfo?.GetProcess() == null || windowInfo.GetProcess().HasExited)
        {
            Debug.WriteLine($"CloseApp: Process for '{config.ProcessName}' not found or already exited.");
            return true;
        }

        Process processToClose = windowInfo.GetProcess();
        string appIdentifier = $"{config.ProcessName} (PID: {processToClose.Id})";
        Debug.WriteLine($"CloseApp: Attempting to close '{appIdentifier}'.");

        try
        {
            if(!processToClose.CloseMainWindow())
            {
                Debug.WriteLine($"CloseApp: CloseMainWindow failed for '{appIdentifier}'. Process might not have a standard window or is unresponsive.");
                if(forceKillIfNotClosed)
                {
                    if(!processToClose.WaitForExit(500))
                    {
                        Debug.WriteLine($"CloseApp: Forcing kill for '{appIdentifier}' after CloseMainWindow failed and short wait.");
                        processToClose.Kill();
                        return true;
                    }
                    return true;
                }
                return false;
            }

            if(processToClose.WaitForExit(gracePeriodMs))
            {
                Debug.WriteLine($"CloseApp: '{appIdentifier}' exited gracefully.");
                return true;
            }
            else
            {
                Debug.WriteLine($"CloseApp: '{appIdentifier}' did not exit within grace period.");
                if(forceKillIfNotClosed)
                {
                    Debug.WriteLine($"CloseApp: Forcing kill for '{appIdentifier}'.");
                    processToClose.Kill();
                    return true;
                }
                return false;
            }
        }
        catch(InvalidOperationException ex)
        {
            Debug.WriteLine($"CloseApp: Process '{appIdentifier}' already exited. {ex.Message}");
            return true;
        }
        catch(Exception ex)
        {
            Debug.WriteLine($"CloseApp: Error closing '{appIdentifier}': {ex.Message}");
            if(forceKillIfNotClosed)
            {
                try
                {
                    if(!processToClose.HasExited) { processToClose.Kill(); return true; }
                }
                catch(Exception killEx) { Debug.WriteLine($"CloseApp: Error during final kill attempt for '{appIdentifier}': {killEx.Message}"); }
            }
            return false;
        }
        finally
        {
            processToClose?.Dispose();
        }
    }

    public void ProcessAllAppsInProfile(Profile profile,
                                        bool launchIfNotRunning,
                                        bool bringToForegroundIfRunning,
                                        bool closeIfRunning,
                                        bool forceKillIfNotClosed = false,
                                        int closeGracePeriodMs = 2000)
    {
        if(profile == null)
        {
            Debug.WriteLine("ProcessAllAppsInProfile: Profile is null.");
            return;
        }
        if(!profile.WindowConfigs.Any(c => c.IsEnabled))
        {
            Debug.WriteLine($"ProcessAllAppsInProfile: Profile '{profile.Name}' has no enabled configs.");
            return;
        }

        Debug.WriteLine($"Processing profile '{profile.Name}': Launch={launchIfNotRunning}, Focus={bringToForegroundIfRunning}, Close={closeIfRunning}");

        List<WindowConfig> configsToProcess = profile.WindowConfigs.Where(c => c.IsEnabled).ToList();

        foreach(var config in configsToProcess)
        {
            var windowInfo = FindManagedWindow(config);
            Process process = windowInfo?.GetProcess();

            if(process != null && !process.HasExited)
            {
                if(closeIfRunning)
                {
                    Debug.WriteLine($"Closing running app as part of profile action: '{config.ProcessName}'");
                    if(!CloseApp(config, forceKillIfNotClosed, closeGracePeriodMs))
                    {
                        Debug.WriteLine($"Failed to close '{config.ProcessName}' during profile action.");
                    }
                }
                else if(bringToForegroundIfRunning)
                {
                    Debug.WriteLine($"Focusing running app: '{config.ProcessName}' (hWnd:{windowInfo.HWnd})");
                    if(!BringWindowToForeground(windowInfo.HWnd))
                    {
                        Debug.WriteLine($"Failed to focus '{config.ProcessName}' (hWnd:{windowInfo.HWnd}).");
                    }
                }
                else
                {
                    Debug.WriteLine($"App '{config.ProcessName}' (PID: {process.Id}) is running. No focus/close action specified.");
                }
            }
            else
            {
                if(launchIfNotRunning)
                {
                    Debug.WriteLine($"Launching missing app: '{config.ProcessName}'");
                    if(!LaunchApp(config))
                    {
                        Debug.WriteLine($"Failed to launch '{config.ProcessName}'.");
                    }
                }
                else
                {
                    Debug.WriteLine($"App '{config.ProcessName}' is not running. No launch action specified.");
                }
            }
            process?.Dispose();
        }
        Debug.WriteLine($"Finished processing profile '{profile.Name}'.");
    }
}