namespace GridPulse.Application.Models;

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
