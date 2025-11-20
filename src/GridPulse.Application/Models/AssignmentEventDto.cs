namespace GridPulse.Application.Models;

public sealed record AssignmentEventDto(
    Guid Id,
    AssignmentEventType EventType,
    string Actor,
    DateTimeOffset OccurredAt,
    Guid? CrewId,
    IReadOnlyDictionary<string, string> Details
);
