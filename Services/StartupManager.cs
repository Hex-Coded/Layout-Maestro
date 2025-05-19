using System.Diagnostics;
using Microsoft.Win32;

namespace WindowPlacementManager.Services;

public enum StartupType
{
    None,
    Normal,
    Admin
}

public class StartupManager
{
    const string AppName = "WindowPlacementManager";
    const string ScheduledTaskName = "WindowPlacementManagerAutoStartAdmin";
    static readonly RegistryKey RkAppRun = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

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
        ClearNormalStartup();
        ClearAdminStartupTask();

        switch(startupType)
        {
            case StartupType.None:
                break;
            case StartupType.Normal:
                SetNormalStartup();
                break;
            case StartupType.Admin:
                SetAdminStartupTask();
                break;
        }
    }

    void SetNormalStartup()
    {
        try
        {
            RkAppRun.SetValue(AppName, Application.ExecutablePath);
        }
        catch(Exception ex)
        {
            Debug.WriteLine($"Error setting normal startup: {ex.Message}");
        }
    }

    void ClearNormalStartup()
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

    bool IsScheduledTaskForAdminStartupSet()
    {
        try
        {
            ProcessStartInfo psi = new ProcessStartInfo("schtasks", $"/Query /TN \"{ScheduledTaskName}\"")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using Process process = Process.Start(psi);
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return output.Contains(ScheduledTaskName) && process.ExitCode == 0;
        }
        catch(Exception ex)
        {
            Debug.WriteLine($"Error checking scheduled task: {ex.Message}");
            return false;
        }
    }

    void SetAdminStartupTask()
    {
        try
        {
            string exePath = Application.ExecutablePath;
            string arguments = $"/Create /SC ONLOGON /TN \"{ScheduledTaskName}\" /TR \"\\\"{exePath}\\\"\" /RL HIGHEST /F";

            ProcessStartInfo psi = new ProcessStartInfo("schtasks", arguments)
            {
                Verb = "runas",
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            try
            {
                using Process process = Process.Start(psi);
                process.WaitForExit();
                if(process.ExitCode != 0)
                {
                    Debug.WriteLine($"schtasks.exe exited with code {process.ExitCode} while setting admin startup.");
                    MessageBox.Show("Failed to set 'Boot with Windows as Admin'.\nThis might be due to UAC prompt denial or insufficient permissions.\nTry running Window Positioner as Administrator once to set this option.",
                                    "Admin Startup Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch(System.ComponentModel.Win32Exception ex) when(ex.NativeErrorCode == 1223)
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

    void ClearAdminStartupTask()
    {
        if(!IsScheduledTaskForAdminStartupSet()) return;

        try
        {
            string arguments = $"/Delete /TN \"{ScheduledTaskName}\" /F";
            ProcessStartInfo psi = new ProcessStartInfo("schtasks", arguments)
            {
                Verb = "runas",
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            try
            {
                using Process process = Process.Start(psi);
                process.WaitForExit();
                if(process.ExitCode != 0)
                {
                    Debug.WriteLine($"schtasks.exe exited with code {process.ExitCode} while clearing admin startup.");
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
