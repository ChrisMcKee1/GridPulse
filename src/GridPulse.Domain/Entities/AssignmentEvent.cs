using System.Collections.Generic;

namespace GridPulse.Domain.Entities;

public sealed class AssignmentEvent
{
    public Guid Id { get; init; }
    public Guid TicketId { get; init; }
    public Ticket? Ticket { get; init; }
    public Guid? CrewId { get; init; }
    public Crew? Crew { get; init; }
    public AssignmentEventType EventType { get; init; }
    public IDictionary<string, string> Details { get; init; } = new Dictionary<string, string>();
    public string Actor { get; init; } = string.Empty;
    public DateTimeOffset OccurredAt { get; init; }
}
