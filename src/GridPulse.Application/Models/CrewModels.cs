namespace GridPulse.Application.Models;

// ===== DTOs =====

public sealed record CrewStatusDto(
    Guid CrewId,
    string DisplayName,
    CrewStatus Status,
    int CurrentTicketCount,
    IReadOnlyCollection<CrewSkill> Skills,
    DateTimeOffset LastStatusUpdate,
    CrewLocationSnapshotDto? Location,
    bool IsTelemetryStale
);

public sealed record CrewLocationSnapshotDto(
    decimal Latitude,
    decimal Longitude,
    DateTimeOffset CapturedAt,
    double? SpeedMph
);

// ===== Requests & Responses =====

public sealed record CrewStatusUpdateRequest
{
    public CrewAssignmentStatus Status { get; init; }
    public string? Note { get; init; }
    public CrewLocationSnapshotDto? Location { get; init; }
}

public sealed record CrewStatusUpdateResponse(
    Guid TicketId,
    DateTimeOffset AcceptedAt
);

public sealed record CrewBatchDeleteRequest
{
    public IEnumerable<Guid> CrewIds { get; init; } = Array.Empty<Guid>();
}
