using System.Collections.Generic;

namespace GridPulse.Domain.Entities;

public sealed class Outage
{
    public Guid Id { get; init; }
    public Guid ServiceLocationId { get; init; }
    public string ServiceAddress { get; init; } = string.Empty;
    public OutageStatus Status { get; init; } = OutageStatus.Reported;
    public DateTimeOffset ReportedAt { get; init; }
    public DateTimeOffset LastUpdatedAt { get; init; }
    public DateTimeOffset? EstimatedRestoration { get; init; }
    public string? Cause { get; init; }
    public ICollection<OutageEvent> Events { get; init; } = new List<OutageEvent>();
}
