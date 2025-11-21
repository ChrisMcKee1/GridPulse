namespace GridPulse.Domain.Enums;

/// <summary>
/// Represents the lifecycle state of an outbound crew assignment payload as it
/// moves through the simulated delivery pipeline.
/// </summary>
public enum AssignmentDeliveryStatus
{
    Queued = 0,
    Acknowledged = 1,
    EnRoute = 2,
    OnScene = 3,
    Paused = 4,
    Completed = 5,
    Failed = 6
}