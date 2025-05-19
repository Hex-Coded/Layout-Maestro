using System.Diagnostics;
using Microsoft.Win32;

namespace WindowPositioner.Services
{
    public enum StartupType
    {
        None,
        Normal,
        Admin
    }

    public class StartupManager
    {
        private const string AppName = "WindowPositioner";
        private const string ScheduledTaskName = "WindowPositionerAutoStartAdmin"; // Unique name for the scheduled task
        private static readonly RegistryKey RkAppRun = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

        public StartupType GetCurrentStartupType()
        {
            if(IsScheduledTaskForAdminStartupSet())
            {
                return StartupType.Admin;
            }
            if(RkAppRun.GetValue(AppName) != null)
            {
                return StartupType.Normal;
            }
            return StartupType.None;
        }

        public void SetStartup(StartupType startupType)
        {
            // First, clear any existing startup methods to avoid conflicts
            ClearNormalStartup();
            ClearAdminStartupTask();

            switch(startupType)
            {
                case StartupType.None:
                    // Already cleared
                    break;
                case StartupType.Normal:
                    SetNormalStartup();
                    break;
                case StartupType.Admin:
                    // Check if current process is admin. If not, cannot create admin task without UAC.
                    // For simplicity, we'll attempt it. If it fails, user might need to run app as admin once to set this.
                    SetAdminStartupTask();
                    break;
            }
        }

        private void SetNormalStartup()
        {
            try
            {
                RkAppRun.SetValue(AppName, Application.ExecutablePath);
            }
            catch(Exception ex)
            {
                Debug.WriteLine($"Error setting normal startup: {ex.Message}");
                // Optionally inform user
            }
        }

        private void ClearNormalStartup()
        {
            try
            {
                if(RkAppRun.GetValue(AppName) != null)
                {
                    RkAppRun.DeleteValue(AppName, false);
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine($"Error clearing normal startup: {ex.Message}");
            }
        }

        private bool IsScheduledTaskForAdminStartupSet()
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("schtasks", $"/Query /TN \"{ScheduledTaskName}\"")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using(Process process = Process.Start(psi))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    // If the task is found, schtasks doesn't return error code but output contains task name
                    // If not found, it might return an error or specific message.
                    // A simple check: if output contains the task name, assume it exists.
                    return output.Contains(ScheduledTaskName) && process.ExitCode == 0;
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine($"Error checking scheduled task: {ex.Message}");
                return false;
            }
        }

        private void SetAdminStartupTask()
        {
            try
            {
                string exePath = Application.ExecutablePath;
                // Command to create a scheduled task that runs at logon with highest privileges
                // /SC ONLOGON: Runs when any user logs on.
                // /RL HIGHEST: Runs with highest privileges.
                // /F: Force create task if it exists.
                // /TR \"'{exePath}'\": The command to run (note quotes for paths with spaces)
                string arguments = $"/Create /SC ONLOGON /TN \"{ScheduledTaskName}\" /TR \"\\\"{exePath}\\\"\" /RL HIGHEST /F";

                ProcessStartInfo psi = new ProcessStartInfo("schtasks", arguments)
                {
                    Verb = "runas", // Request elevation for schtasks itself if current process isn't admin
                    UseShellExecute = true, // Required for verb runas
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                try
                {
                    using(Process process = Process.Start(psi))
                    {
                        process.WaitForExit();
                        if(process.ExitCode != 0)
                        {
                            Debug.WriteLine($"schtasks.exe exited with code {process.ExitCode} while setting admin startup.");
                            // Consider notifying the user that setting admin startup might have failed,
                            // possibly due to UAC denial or insufficient permissions.
                            MessageBox.Show("Failed to set 'Boot with Windows as Admin'.\nThis might be due to UAC prompt denial or insufficient permissions.\nTry running Window Positioner as Administrator once to set this option.",
                                            "Admin Startup Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }
                catch(System.ComponentModel.Win32Exception ex) when(ex.NativeErrorCode == 1223) // 1223: The operation was canceled by the user (UAC)
                {
                    Debug.WriteLine("Admin startup task creation was cancelled by UAC.");
                    MessageBox.Show("Setting 'Boot with Windows as Admin' was cancelled (UAC prompt).",
                                    "Admin Startup Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine($"Error setting admin startup task: {ex.Message}");
                MessageBox.Show($"An error occurred while trying to set 'Boot with Windows as Admin':\n{ex.Message}",
                                "Admin Startup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ClearAdminStartupTask()
        {
            if(!IsScheduledTaskForAdminStartupSet()) return; // No need to delete if not set

            try
            {
                // Command to delete the scheduled task
                // /F: Force delete
                string arguments = $"/Delete /TN \"{ScheduledTaskName}\" /F";
                ProcessStartInfo psi = new ProcessStartInfo("schtasks", arguments)
                {
                    Verb = "runas", // May need elevation to delete task created by admin
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                try
                {
                    using(Process process = Process.Start(psi))
                    {
                        process.WaitForExit();
                        if(process.ExitCode != 0)
                        {
                            Debug.WriteLine($"schtasks.exe exited with code {process.ExitCode} while clearing admin startup.");
                        }
                    }
                }
                catch(System.ComponentModel.Win32Exception ex) when(ex.NativeErrorCode == 1223)
                {
                    Debug.WriteLine("Admin startup task deletion was cancelled by UAC.");
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine($"Error clearing admin startup task: {ex.Message}");
            }
        }
    }
}
