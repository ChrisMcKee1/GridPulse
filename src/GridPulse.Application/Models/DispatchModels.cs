namespace GridPulse.Application.Models;

// ===== DTOs =====

public sealed record DispatchRecommendationDto(
    Guid Id,
    Guid TicketId,
    CrewStatusDto Crew,
    double CompositeScore,
    IReadOnlyDictionary<string, double> ScoreComponents,
    int EtaMinutes,
    bool IsAutoSelected,
    bool IsOverride,
    string? OverrideReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ExpiresAt
);

public sealed record DispatchRecommendationsEnvelope(
    TicketSummaryDto Ticket,
    IReadOnlyCollection<DispatchRecommendationDto> Recommendations
);

// ===== Requests =====

public sealed record DispatchAssignmentRequest
{
    public Guid TicketId { get; init; }
    public Guid CrewId { get; init; }
    public string? OverrideReason { get; init; }
    public IReadOnlyCollection<string> NotifyCrewChannels { get; init; } = Array.Empty<string>();
}
