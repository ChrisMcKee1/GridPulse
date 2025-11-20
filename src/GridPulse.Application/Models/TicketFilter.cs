using System.Collections.Generic;

namespace GridPulse.Application.Models;

public sealed record TicketFilter
{
    public IReadOnlyCollection<TicketStatus>? Statuses { get; init; }
    public TicketPriority? MinPriority { get; init; }
    public bool IncludeRecommendations { get; init; }
    public bool IncludeTimeline { get; init; }
}
