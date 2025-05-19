using System.Diagnostics;
using WindowPlacementManager.Models;

namespace WindowPlacementManager.Services;

public static class WindowEnumerationService
{
    public class FoundWindowInfo
    {
        public IntPtr HWnd { get; set; }
        public int ProcessId { get; set; }
        public string ProcessNameCache { get; set; }
        public string Title { get; set; }

        private Process _process;
        public Process GetProcess()
        {
            if(_process == null || _process.HasExited)
            {
                try { _process = Process.GetProcessById(ProcessId); }
                catch { _process = null; }
            }
            return _process;
        }
    }

    public static List<FoundWindowInfo> FindAllWindowsByProcessNameAndTitle(string processNameFilter, string titleHintFilter)
    {
        var foundWindows = new List<FoundWindowInfo>();
        if(string.IsNullOrWhiteSpace(processNameFilter))
        {
            return foundWindows;
        }

        Native.EnumWindows((hWnd, lParam) =>
        {
            if(!Native.IsWindowVisible(hWnd))
                return true;

            Native.GetWindowThreadProcessId(hWnd, out uint pid);
            if(pid == 0)
                return true;

            try
            {
                Process p = Process.GetProcessById((int)pid);
                using(p)
                {
                    if(p.ProcessName.Equals(processNameFilter, StringComparison.OrdinalIgnoreCase))
                    {
                        string windowTitle = Native.GetWindowTitle(hWnd);
                        if(string.IsNullOrWhiteSpace(titleHintFilter) ||
                            (!string.IsNullOrWhiteSpace(windowTitle) && windowTitle.ToLower().Contains(titleHintFilter.ToLower())))
                        {
                            foundWindows.Add(new FoundWindowInfo
                            {
                                HWnd = hWnd,
                                ProcessId = (int)pid,
                                ProcessNameCache = p.ProcessName,
                                Title = windowTitle
                            });
                        }
                    }
                }
            }
            catch(ArgumentException) { }
            catch(Exception ex)
            {
                Debug.WriteLine($"Error in FindAllWindowsByProcessNameAndTitle (PID: {pid}): {ex.Message}");
            }
            return true;
        }, IntPtr.Zero);

        return foundWindows;
    }

    public static FoundWindowInfo FindMostSuitableWindow(WindowConfig config)
    {
        if(config == null || string.IsNullOrWhiteSpace(config.ProcessName))
            return null;

        List<FoundWindowInfo> candidates = FindAllWindowsByProcessNameAndTitle(config.ProcessName, config.WindowTitleHint);

        if(!candidates.Any())
            return null;

        if(!string.IsNullOrWhiteSpace(config.WindowTitleHint))
        {
            var exactTitleMatch = candidates.FirstOrDefault(fw =>
                fw.Title.Equals(config.WindowTitleHint, StringComparison.OrdinalIgnoreCase));
            if(exactTitleMatch != null) return exactTitleMatch;

        }

        return candidates.FirstOrDefault();
    }
}