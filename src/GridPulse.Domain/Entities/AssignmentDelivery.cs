using System.Collections.Generic;
using GridPulse.Domain.Enums;

namespace GridPulse.Domain.Entities;

/// <summary>
/// Represents a queued dispatcher assignment payload that will be delivered to
/// a crew device. The payload is stored so future mobile clients can poll for
/// work even when delivery occurs asynchronously.
/// </summary>
public sealed class AssignmentDelivery
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public Ticket? Ticket { get; set; }
    public Guid CrewId { get; set; }
    public Crew? Crew { get; set; }
    public string TrackingId { get; set; } = string.Empty;
    public AssignmentDeliveryStatus Status { get; set; } = AssignmentDeliveryStatus.Queued;
    public int AttemptCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }
    public IDictionary<string, string> Payload { get; set; } = new Dictionary<string, string>();
    public string? LastError { get; set; }
}