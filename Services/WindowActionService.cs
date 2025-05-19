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
            ProcessStartInfo startInfo = new ProcessStartInfo(pathToLaunch)
            {
                UseShellExecute = true
            };

            if(config.LaunchAsAdmin)
            {
                startInfo.Verb = "runas";
            }

            Process.Start(startInfo);
            Debug.WriteLine($"Launched '{pathToLaunch}' (Admin: {config.LaunchAsAdmin}).");
            return true;
        }
        catch(System.ComponentModel.Win32Exception ex)
        {
            if(ex.NativeErrorCode == 1223 && config.LaunchAsAdmin)
            {
                Debug.WriteLine($"Launch of '{pathToLaunch}' as admin was cancelled by the user (UAC).");
                MessageBox.Show($"Launching '{pathToLaunch}' as Administrator was cancelled.", "Launch Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            if(ex.NativeErrorCode == 740 && !config.LaunchAsAdmin)
            {
                Debug.WriteLine($"Launch Error '{pathToLaunch}': Elevation required but not requested via config. {ex.Message}");
                MessageBox.Show($"Failed to launch '{pathToLaunch}'.\nThe application requires administrator privileges to start, but 'Launch as Admin' was not checked for this configuration.\n\nConsider enabling 'Launch as Admin' for this app or running Window Placement Manager as Administrator.", "Launch Error - Elevation Required", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            Debug.WriteLine($"Win32 Launch Error '{pathToLaunch}': {ex.Message} (Code: {ex.NativeErrorCode})");
            MessageBox.Show($"Failed to launch '{pathToLaunch}'.\nError: {ex.Message}\n\nCheck if the path is correct and the application exists.", "Launch Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
        catch(Exception ex)
        {
            Debug.WriteLine($"General Launch Error '{pathToLaunch}': {ex.Message}");
            MessageBox.Show($"Failed to launch '{pathToLaunch}'.\nError: {ex.Message}\n\nCheck if the path is correct and the application exists.", "Launch Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
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

    public bool ActivateOrLaunchApp(WindowConfig config)
    {
        if(!config.IsEnabled)
        {
            Debug.WriteLine($"Skipping disabled config: '{config.ProcessName}'");
            return true;
        }

        var windowInfo = FindManagedWindow(config);

        if(windowInfo != null && windowInfo.HWnd != IntPtr.Zero)
        {
            Debug.WriteLine($"App '{config.ProcessName}' (hWnd:{windowInfo.HWnd}) found. Focusing.");
            return BringWindowToForeground(windowInfo.HWnd);
        }
        else
        {
            Debug.WriteLine($"App '{config.ProcessName}' not found or its window handle is invalid. Launching.");
            return LaunchApp(config);
        }
    }

    public void ActivateOrLaunchAllAppsInProfile(Profile profile, bool launchIfNotRunning, bool bringToForegroundIfRunning)
    {
        if(profile == null)
        {
            Debug.WriteLine("ActivateOrLaunchAllAppsInProfile: Profile is null.");
            return;
        }
        if(!profile.WindowConfigs.Any(c => c.IsEnabled))
        {
            Debug.WriteLine($"ActivateOrLaunchAllAppsInProfile: Profile '{profile.Name}' has no enabled configs.");
            return;
        }

        Debug.WriteLine($"Processing profile '{profile.Name}': Launch={launchIfNotRunning}, Focus={bringToForegroundIfRunning}");

        foreach(var config in profile.WindowConfigs.Where(c => c.IsEnabled))
        {
            var windowInfo = FindManagedWindow(config);

            if(windowInfo != null && windowInfo.HWnd != IntPtr.Zero)
            {
                if(bringToForegroundIfRunning)
                {
                    Debug.WriteLine($"Focusing running app: '{config.ProcessName}' (hWnd:{windowInfo.HWnd})");
                    if(!BringWindowToForeground(windowInfo.HWnd))
                    {
                        Debug.WriteLine($"Failed to focus '{config.ProcessName}' (hWnd:{windowInfo.HWnd}).");
                    }
                }
                else
                {
                    Debug.WriteLine($"App '{config.ProcessName}' (hWnd:{windowInfo.HWnd}) is running. No action taken as bringToForegroundIfRunning is false.");
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
                    Debug.WriteLine($"App '{config.ProcessName}' is not running. No action taken as launchIfNotRunning is false.");
                }
            }
        }
        Debug.WriteLine($"Finished processing profile '{profile.Name}'.");
    }
}