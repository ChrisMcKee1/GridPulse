namespace GridPulse.Application.Models;

public sealed record TicketCreateRequest
{
    public string Title { get; init; } = string.Empty;
    public string OutageReferenceId { get; init; } = string.Empty;
    public string? Description { get; init; }
    public TicketPriority Priority { get; init; } = TicketPriority.Medium;
    public ICollection<string> AffectedAssets { get; init; } = new List<string>();
    public int CustomerImpact { get; init; }
}
