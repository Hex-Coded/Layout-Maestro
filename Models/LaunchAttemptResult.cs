namespace WindowPlacementManager.Models;

public enum LaunchAttemptResult
{
    Success,
    SuccessNoAdminNeeded,
    Failed,
    UacCancelled,
    ConfigError,
    ElevationRequiredButNotRequested
}