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

    public StartupType GetCurrentStartupType() => IsScheduledTaskForAdminStartupSet() ? StartupType.Admin : (RkAppRun.GetValue(AppName) != null ? StartupType.Normal : StartupType.None);

    public void SetStartup(StartupType startupType)
    {
        StartupType currentSystemType = GetCurrentStartupType();
        if(currentSystemType == startupType) return;

        if(currentSystemType == StartupType.Normal && startupType != StartupType.Normal)
            ClearNormalStartup();
        if(currentSystemType == StartupType.Admin && startupType != StartupType.Admin)
            ClearAdminStartupTask();

        switch(startupType)
        {
            case StartupType.None:
                break;
            case StartupType.Normal:
                if(currentSystemType != StartupType.Normal) SetNormalStartup();
                break;
            case StartupType.Admin:
                if(currentSystemType != StartupType.Admin) SetAdminStartupTask();
                break;
        }
    }

    void SetNormalStartup()
    {
        try { RkAppRun.SetValue(AppName, Application.ExecutablePath); }
        catch(Exception ex) { Debug.WriteLine($"Error setting normal startup: {ex.Message}"); }
    }

    void ClearNormalStartup()
    {
        try { if(RkAppRun.GetValue(AppName) != null) RkAppRun.DeleteValue(AppName, false); }
        catch(Exception ex) { Debug.WriteLine($"Error clearing normal startup: {ex.Message}"); }
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

    (bool success, bool uacCancelled) ExecuteAdminSchTasksOperation(string arguments)
    {
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
            if(process.ExitCode == 0) return (true, false);
            Debug.WriteLine($"schtasks.exe exited with code {process.ExitCode} for args: {arguments}.");
            return (false, false);
        }
        catch(System.ComponentModel.Win32Exception ex) when(ex.NativeErrorCode == 1223)
        {
            Debug.WriteLine($"schtasks command '{arguments}' was cancelled by UAC.");
            return (false, true);
        }
        catch(Exception ex)
        {
            Debug.WriteLine($"Generic error for schtasks args '{arguments}': {ex.Message}");
            return (false, false);
        }
    }

    void SetAdminStartupTask()
    {
        try
        {
            string exePath = Application.ExecutablePath;
            string arguments = $"/Create /SC ONLOGON /TN \"{ScheduledTaskName}\" /TR \"\\\"{exePath}\\\"\" /RL HIGHEST /F";
            var (success, uacCancelled) = ExecuteAdminSchTasksOperation(arguments);

            if(success) return;

            if(uacCancelled)
                MessageBox.Show("Setting 'Boot with Windows as Admin' was cancelled (UAC prompt).",
                                "Admin Startup Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);
            else
                MessageBox.Show("Failed to set 'Boot with Windows as Admin'.\nThis might be due to UAC prompt denial or insufficient permissions.\nTry running Window Positioner as Administrator once to set this option.",
                                "Admin Startup Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch(Exception ex)
        {
            Debug.WriteLine($"Error setting admin startup task (outer handler): {ex.Message}");
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
            var (success, uacCancelled) = ExecuteAdminSchTasksOperation(arguments);

            if(success) return;

            if(uacCancelled)
                Debug.WriteLine("Admin startup task deletion was cancelled by UAC.");
            else
                Debug.WriteLine($"Failed to clear admin startup task (schtasks non-zero exit or other Process.Start error).");
        }
        catch(Exception ex)
        {
            Debug.WriteLine($"Error clearing admin startup task (outer handler): {ex.Message}");
        }
    }
}