using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using WindowPlacementManager.Models;

namespace WindowPlacementManager.Services
{
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
                Process.Start(new ProcessStartInfo(pathToLaunch) { UseShellExecute = true });
                Debug.WriteLine($"Launched '{pathToLaunch}'.");
                return true;
            }
            catch(System.ComponentModel.Win32Exception ex) when(ex.NativeErrorCode == 740)
            {
                Debug.WriteLine($"Launch Error '{pathToLaunch}': Elevation required. {ex.Message}");
                MessageBox.Show($"Failed to launch '{pathToLaunch}'.\nThe application requires administrator privileges to start.\n\nTry running Window Placement Manager as Administrator or ensure the target app doesn't require elevation.", "Launch Error - Elevation Required", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            catch(Exception ex)
            {
                Debug.WriteLine($"Launch Error '{pathToLaunch}': {ex.Message}");
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
            if(windowInfo?.HWnd != IntPtr.Zero)
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

        public void ActivateOrLaunchAllAppsInProfile(Profile profile, bool launchIfNotRunning, bool bringToForegroundIfRunning)
        {
            if(profile == null) return;
            Debug.WriteLine($"Processing profile '{profile.Name}': Launch={launchIfNotRunning}, Focus={bringToForegroundIfRunning}");

            foreach(var config in profile.WindowConfigs.Where(c => c.IsEnabled))
            {
                var windowInfo = FindManagedWindow(config);
                if(windowInfo?.HWnd != IntPtr.Zero)
                {
                    if(bringToForegroundIfRunning)
                    {
                        if(!BringWindowToForeground(windowInfo.HWnd))
                            Debug.WriteLine($"Failed to focus '{config.ProcessName}' (hWnd:{windowInfo.HWnd}).");
                    }
                }
                else if(launchIfNotRunning)
                {
                    if(!LaunchApp(config))
                        Debug.WriteLine($"Failed to launch '{config.ProcessName}'.");
                }
            }
            Debug.WriteLine($"Finished profile '{profile.Name}'.");
        }
    }
}