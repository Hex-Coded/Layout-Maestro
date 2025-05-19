using System;
using System.Diagnostics;
using System.Linq;
using WindowPlacementManager.Models;

namespace WindowPlacementManager.Services
{
    public class WindowActionService
    {
        public Process GetRunningProcess(WindowConfig config)
        {
            if(string.IsNullOrWhiteSpace(config.ProcessName))
                return null;

            try
            {
                Process[] processes = Process.GetProcessesByName(config.ProcessName);
                if(!processes.Any()) return null;

                if(!string.IsNullOrWhiteSpace(config.WindowTitleHint))
                {
                    foreach(var proc in processes)
                    {
                        if(proc.MainWindowHandle != IntPtr.Zero)
                        {
                            string currentTitle = NativeMethods.GetWindowTitle(proc.MainWindowHandle);
                            if(currentTitle.ToLower().Contains(config.WindowTitleHint.ToLower()))
                            {
                                try { Process.GetProcessById(proc.Id); return proc; }
                                catch { }
                            }
                        }
                    }
                    return null;
                }
                return processes.FirstOrDefault(p => {
                    try { Process.GetProcessById(p.Id); return p.MainWindowHandle != IntPtr.Zero; }
                    catch { return false; }
                });
            }
            catch(Exception ex)
            {
                Debug.WriteLine($"Error getting running process for '{config.ProcessName}': {ex.Message}");
                return null;
            }
        }

        public bool LaunchApp(WindowConfig config)
        {
            string pathToLaunch = !string.IsNullOrWhiteSpace(config.ExecutablePath)
                                  ? config.ExecutablePath
                                  : config.ProcessName;

            if(string.IsNullOrWhiteSpace(pathToLaunch))
            {
                Debug.WriteLine($"Cannot launch app for config '{config.ProcessName}': No ExecutablePath or ProcessName provided.");
                return false;
            }

            try
            {
                Process.Start(pathToLaunch);
                Debug.WriteLine($"Launched '{pathToLaunch}'.");
                return true;
            }
            catch(Exception ex)
            {
                Debug.WriteLine($"Error launching app '{pathToLaunch}': {ex.Message}");
                return false;
            }
        }

        public bool BringWindowToForeground(IntPtr hWnd)
        {
            if(hWnd == IntPtr.Zero) return false;

            try
            {
                if(NativeMethods.IsIconic(hWnd))
                {
                    NativeMethods.ShowWindow(hWnd, NativeMethods.SW_RESTORE);
                }

                return NativeMethods.SetForegroundWindow(hWnd);
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
                Debug.WriteLine($"Skipping action for disabled config: '{config.ProcessName}'");
                return true;
            }

            Process runningProcess = GetRunningProcess(config);
            if(runningProcess != null && runningProcess.MainWindowHandle != IntPtr.Zero)
            {
                Debug.WriteLine($"App '{config.ProcessName}' (PID: {runningProcess.Id}) already running. Attempting to bring to foreground.");
                return BringWindowToForeground(runningProcess.MainWindowHandle);
            }
            else
            {
                Debug.WriteLine($"App '{config.ProcessName}' not found or no main window. Attempting to launch.");
                bool launched = LaunchApp(config);
                return launched;
            }
        }

        public void ActivateOrLaunchAllAppsInProfile(Profile profile, bool launchIfNotRunning, bool bringToForegroundIfRunning)
        {
            if(profile == null)
            {
                Debug.WriteLine("ActivateOrLaunchAllAppsInProfile: Profile is null.");
                return;
            }

            Debug.WriteLine($"Processing profile '{profile.Name}': LaunchIfNotRunning={launchIfNotRunning}, BringToForegroundIfRunning={bringToForegroundIfRunning}");

            foreach(var config in profile.WindowConfigs.Where(c => c.IsEnabled))
            {
                Process runningProcess = GetRunningProcess(config);

                if(runningProcess != null && runningProcess.MainWindowHandle != IntPtr.Zero)
                {
                    if(bringToForegroundIfRunning)
                    {
                        Debug.WriteLine($"App '{config.ProcessName}' (PID: {runningProcess.Id}) running. Bringing to foreground.");
                        BringWindowToForeground(runningProcess.MainWindowHandle);
                    }
                }
                else
                {
                    if(launchIfNotRunning)
                    {
                        Debug.WriteLine($"App '{config.ProcessName}' not running or no main window. Launching.");
                        LaunchApp(config);
                    }
                }
            }
            Debug.WriteLine($"Finished processing profile '{profile.Name}'.");
        }
    }
}