namespace GridPulse.Domain.Enums;

/// <summary>
/// Represents the assignment-specific status reported by a crew device while
/// executing a dispatched ticket. These values are distinct from the broader
/// <see cref="CrewStatus"/> roster states so workflow automation can reason
/// about acknowledgements separately from availability.
/// </summary>
public enum CrewAssignmentStatus
{
    Acknowledged = 0,
    EnRoute = 1,
    OnScene = 2,
    Paused = 3,
    Completed = 4
}