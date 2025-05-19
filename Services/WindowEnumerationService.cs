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

        Process process;
        public Process GetProcess()
        {
            if(process == null || process.HasExited)
                try { process = Process.GetProcessById(ProcessId); }
                catch { process = null; }
            return process;
        }
    }

    public static List<FoundWindowInfo> FindAllWindowsByProcessNameAndTitle(string processNameFilter, string titleHintFilter)
    {
        var foundWindows = new List<FoundWindowInfo>();
        if(string.IsNullOrWhiteSpace(processNameFilter))
            return foundWindows;

        Native.EnumWindows((hWnd, lParam) => EnumWindowsCallback(hWnd, processNameFilter, titleHintFilter, foundWindows), IntPtr.Zero);
        return foundWindows;
    }

    static bool EnumWindowsCallback(IntPtr hWnd, string processNameFilter, string titleHintFilter, List<FoundWindowInfo> foundWindows)
    {
        if(!Native.IsWindowVisible(hWnd))
            return true;

        Native.GetWindowThreadProcessId(hWnd, out uint pid);
        if(pid == 0)
            return true;

        TryAddWindowIfMatching(hWnd, pid, processNameFilter, titleHintFilter, foundWindows);
        return true;
    }

    static void TryAddWindowIfMatching(IntPtr hWnd, uint pid, string processNameFilter, string titleHintFilter, List<FoundWindowInfo> foundWindows)
    {
        try
        {
            Process p = Process.GetProcessById((int)pid);
            using(p)
                if(p.ProcessName.Equals(processNameFilter, StringComparison.OrdinalIgnoreCase))
                {
                    string windowTitle = Native.GetWindowTitle(hWnd);
                    if(IsTitleMatch(windowTitle, titleHintFilter))
                        foundWindows.Add(CreateFoundWindowInfo(hWnd, (int)pid, p.ProcessName, windowTitle));
                }
        }
        catch(ArgumentException) { }
        catch(Exception ex)
        {
            Debug.WriteLine($"Error processing window (PID: {pid}): {ex.Message}");
        }
    }

    static bool IsTitleMatch(string windowTitle, string titleHintFilter) =>
        string.IsNullOrWhiteSpace(titleHintFilter) ||
        (!string.IsNullOrWhiteSpace(windowTitle) && windowTitle.ToLower().Contains(titleHintFilter.ToLower()));

    static FoundWindowInfo CreateFoundWindowInfo(IntPtr hWnd, int processId, string processName, string title) =>
        new FoundWindowInfo
        {
            HWnd = hWnd,
            ProcessId = processId,
            ProcessNameCache = processName,
            Title = title
        };

    public static FoundWindowInfo FindMostSuitableWindow(WindowConfig config)
    {
        if(config == null || string.IsNullOrWhiteSpace(config.ProcessName)) return null;

        List<FoundWindowInfo> candidates = FindAllWindowsByProcessNameAndTitle(config.ProcessName, config.WindowTitleHint);
        if(!candidates.Any()) return null;

        if(!string.IsNullOrWhiteSpace(config.WindowTitleHint))
        {
            FoundWindowInfo exactTitleMatch = candidates.FirstOrDefault(fw =>
                fw.Title != null && fw.Title.Equals(config.WindowTitleHint, StringComparison.OrdinalIgnoreCase));
            if(exactTitleMatch != null) return exactTitleMatch;
        }
        return candidates.FirstOrDefault();
    }
}