using Microsoft.Win32;

namespace WindowPositioner.Services
{
    public class StartupManager
    {
        private const string AppName = "WindowPositioner";
        private static readonly RegistryKey RkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

        public bool IsStartupEnabled()
        {
            return RkApp.GetValue(AppName) != null;
        }

        public void SetStartup(bool enable)
        {
            if(enable)
            {
                if(!IsStartupEnabled())
                {
                    RkApp.SetValue(AppName, Application.ExecutablePath);
                }
            }
            else
            {
                if(IsStartupEnabled())
                {
                    RkApp.DeleteValue(AppName, false);
                }
            }
        }
    }
}
