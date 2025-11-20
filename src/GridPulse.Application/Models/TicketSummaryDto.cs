namespace GridPulse.Application.Models;

public sealed record TicketSummaryDto(
    Guid Id,
    string Title,
    TicketPriority Priority,
    TicketStatus Status,
    DateTimeOffset OpenedAt,
    int CustomerImpact,
    Guid? AssignedCrewId,
    string? AssignedCrewName
);
