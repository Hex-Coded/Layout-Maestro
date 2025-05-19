using System.Diagnostics;

namespace WindowPlacementManager.Services;

public static partial class WindowEnumerationService
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
}