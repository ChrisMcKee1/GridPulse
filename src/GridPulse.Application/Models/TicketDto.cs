namespace GridPulse.Application.Models;

public sealed record TicketDto(
    Guid Id,
    string Title,
    string OutageReferenceId,
    TicketPriority Priority,
    TicketStatus Status,
    int CustomerImpact,
    int? EtaMinutes,
    Guid? AssignedCrewId,
    string? AssignedCrewName,
    IReadOnlyCollection<string> AffectedAssets,
    DateTimeOffset OpenedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ClosedAt,
    IReadOnlyCollection<AssignmentEventDto> Events,
    IReadOnlyCollection<DispatchRecommendationDto> Recommendations
);
