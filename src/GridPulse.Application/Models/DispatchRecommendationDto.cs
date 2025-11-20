namespace GridPulse.Application.Models;

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
