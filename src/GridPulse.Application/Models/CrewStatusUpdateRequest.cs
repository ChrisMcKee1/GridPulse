using GridPulse.Domain.Enums;

namespace GridPulse.Application.Models;

public sealed record CrewStatusUpdateRequest
{
    public CrewAssignmentStatus Status { get; init; }
    public string? Note { get; init; }
    public CrewLocationSnapshotDto? Location { get; init; }
}
