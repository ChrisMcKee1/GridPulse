using System.Collections.Generic;

namespace GridPulse.Domain.Entities;

public sealed class DispatchRecommendation
{
    public Guid Id { get; init; }
    public Guid TicketId { get; init; }
    public Ticket? Ticket { get; init; }
    public Guid CrewId { get; init; }
    public Crew? Crew { get; init; }
    public double CompositeScore { get; init; }
    public IDictionary<string, double> ScoreComponents { get; init; } = new Dictionary<string, double>();
    public int RecommendedRouteEtaMinutes { get; init; }
    public bool IsAutoSelected { get; init; }
    public bool IsOverride { get; init; }
    public string? OverrideReason { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
}
