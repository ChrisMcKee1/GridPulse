using System.Collections.Generic;

namespace GridPulse.Domain.Entities;

public sealed class Crew
{
    public Guid Id { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string Region { get; init; } = string.Empty;
    public ICollection<CrewSkill> Skills { get; init; } = new List<CrewSkill>();
    public int CurrentTicketCount { get; init; }
    public CrewStatus Status { get; init; } = CrewStatus.Available;
    public DateTimeOffset LastStatusUpdate { get; init; }
    public TimeSpan PreferredShiftEnd { get; init; }
    public string? DeviceEndpoint { get; init; }
    public ICollection<CrewLocationSnapshot> LocationHistory { get; init; } = new List<CrewLocationSnapshot>();
    public ICollection<DispatchRecommendation> Recommendations { get; init; } = new List<DispatchRecommendation>();
    public ICollection<AssignmentEvent> Events { get; init; } = new List<AssignmentEvent>();
}
