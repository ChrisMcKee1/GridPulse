namespace GridPulse.Application.Models;

public sealed record AssignmentReceiptDto(
    Guid TicketId,
    Guid CrewId,
    string DeliveryStatus,
    string TrackingId
);
