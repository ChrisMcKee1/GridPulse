namespace GridPulse.Application.Models;

public sealed record TicketStatusUpdateRequest
{
    public TicketStatus Status { get; init; }
    public string? Reason { get; init; }
}
