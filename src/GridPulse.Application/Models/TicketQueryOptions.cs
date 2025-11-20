namespace GridPulse.Application.Models;

public sealed record TicketQueryOptions
{
    public IReadOnlyCollection<TicketStatus>? Statuses { get; init; }
    public TicketPriority? MinPriority { get; init; }
    public bool IncludeRecommendations { get; init; }
    public bool IncludeTimeline { get; init; }
    public int? Take { get; init; }
}
