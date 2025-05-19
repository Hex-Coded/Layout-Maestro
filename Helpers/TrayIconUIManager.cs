using System.Diagnostics;

namespace WindowPlacementManager.Helpers;

public static class TrayIconUIManager
{
    const int WM_SYSCOMMAND = 0x0112;
    const int SC_MINIMIZE = 0xF020;
    public static void InitializeNotifyIcon(NotifyIcon notifyIcon)
    {
        if(notifyIcon.Icon == null)
            try { notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location); }
            catch(Exception exIcon)
            {
                Debug.WriteLine($"Failed to extract associated icon: {exIcon.Message}");
                try { notifyIcon.Icon = System.Drawing.SystemIcons.Application; }
                catch(Exception exSysIcon) { Debug.WriteLine($"Failed to set system icon: {exSysIcon.Message}"); }
            }
    }

    public static void HideFormAndShowTrayIcon(Form form, NotifyIcon notifyIcon)
    {
        form.Hide();
        if(notifyIcon != null)
            notifyIcon.Visible = true;
    }

    public static void ShowFormFromTrayIcon(Form form, NotifyIcon notifyIcon)
    {
        if(notifyIcon != null) notifyIcon.Visible = false;
        form.Show();
        form.WindowState = FormWindowState.Normal;
        form.Activate();
        form.BringToFront();
    }

    public static bool HandleMinimizeToTray(ref Message m, Action hideAction)
    {
        if(m.Msg == WM_SYSCOMMAND && m.WParam.ToInt32() == SC_MINIMIZE)
        {
            hideAction?.Invoke();
            return true;
        }
        return false;
    }

    public static void DisposeNotifyIcon(NotifyIcon notifyIcon)
    {
        if(notifyIcon != null)
        {
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
        }
    }
}
