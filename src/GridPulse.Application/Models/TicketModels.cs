namespace GridPulse.Application.Models;

// ===== DTOs =====

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

// ===== Requests =====

public sealed record TicketCreateRequest
{
    public string Title { get; init; } = string.Empty;
    public string OutageReferenceId { get; init; } = string.Empty;
    public string? Description { get; init; }
    public TicketPriority Priority { get; init; } = TicketPriority.Medium;
    public ICollection<string> AffectedAssets { get; init; } = new List<string>();
    public int CustomerImpact { get; init; }
}

public sealed record TicketStatusUpdateRequest
{
    public TicketStatus Status { get; init; }
    public string? Reason { get; init; }
}

public sealed record TicketBatchDeleteRequest
{
    public IEnumerable<Guid> TicketIds { get; init; } = Array.Empty<Guid>();
}

// ===== Query & Filter =====

public sealed record TicketFilter
{
    public IReadOnlyCollection<TicketStatus>? Statuses { get; init; }
    public TicketPriority? MinPriority { get; init; }
    public bool IncludeRecommendations { get; init; }
    public bool IncludeTimeline { get; init; }
}

public sealed record TicketQueryOptions
{
    public IReadOnlyCollection<TicketStatus>? Statuses { get; init; }
    public TicketPriority? MinPriority { get; init; }
    public bool IncludeRecommendations { get; init; }
    public bool IncludeTimeline { get; init; }
    public int? Take { get; init; }
}

// ===== Seed Data =====

public sealed record TicketSeedData(
    string OutageReferenceId,
    string Title,
    TicketPriority Priority,
    string? Description,
    IReadOnlyCollection<string> AffectedAssets,
    int CustomerImpact,
    DateTimeOffset OpenedAt
);
