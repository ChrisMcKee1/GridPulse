namespace GridPulse.Domain.Entities;

public sealed class OutageEvent
{
    public Guid Id { get; init; }
    public Guid OutageId { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public OutageEventType Type { get; init; }
    public OutageStatus? StatusFrom { get; init; }
    public OutageStatus? StatusTo { get; init; }
    public string? Message { get; init; }
    public string CreatedBy { get; init; } = "system";
}
