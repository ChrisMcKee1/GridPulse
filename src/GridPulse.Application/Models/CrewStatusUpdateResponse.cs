namespace GridPulse.Application.Models;

public sealed record CrewStatusUpdateResponse(
    Guid TicketId,
    DateTimeOffset AcceptedAt
);
