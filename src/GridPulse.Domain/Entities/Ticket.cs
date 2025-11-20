using System.Collections.Generic;

namespace GridPulse.Domain.Entities;

public sealed class Ticket
{
    public Guid Id { get; init; }
    public string OutageReferenceId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public TicketPriority Priority { get; init; } = TicketPriority.Medium;
    public TicketStatus Status { get; init; } = TicketStatus.Open;
    public ICollection<string> AffectedAssets { get; init; } = new List<string>();
    public int CustomerImpact { get; init; }
    public int? EtaMinutes { get; init; }
    public Guid? AssignedCrewId { get; init; }
    public string AutomationSource { get; init; } = "ingestion";
    public int AuditVersion { get; init; }
    public DateTimeOffset OpenedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public DateTimeOffset? ClosedAt { get; init; }
    public ICollection<AssignmentEvent> Events { get; init; } = new List<AssignmentEvent>();
    public ICollection<DispatchRecommendation> Recommendations { get; init; } = new List<DispatchRecommendation>();
}
