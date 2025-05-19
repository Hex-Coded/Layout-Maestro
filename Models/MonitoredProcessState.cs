using WindowPlacementManager.Models;

public class MonitoredProcessState
{
    public WindowConfig Config { get; set; }
    public bool HasBeenObservedRunning { get; set; } = false;
    public int? LastSeenProcessId { get; set; }
    public bool HasBeenPositionedThisInstance { get; set; } = false;

    public bool IsUacLaunchPending { get; set; } = false;
    public DateTime? LastUacLaunchAttemptTime { get; set; } = null;
    public DateTime? LastUacUserCancelTime { get; set; } = null;

    public MonitoredProcessState(WindowConfig config) => Config = config;
}