namespace GridPulse.Application.Models;

public sealed record DispatchAssignmentRequest
{
    public Guid TicketId { get; init; }
    public Guid CrewId { get; init; }
    public string? OverrideReason { get; init; }
    public IReadOnlyCollection<string> NotifyCrewChannels { get; init; } = Array.Empty<string>();
}
