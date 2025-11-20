namespace GridPulse.Domain.Entities;

public sealed class CrewLocationSnapshot
{
    public Guid Id { get; init; }
    public Guid CrewId { get; init; }
    public Crew? Crew { get; init; }
    public decimal Latitude { get; init; }
    public decimal Longitude { get; init; }
    public DateTimeOffset CapturedAt { get; init; }
    public int SignalAgeSeconds { get; init; }
    public bool IsStale { get; init; }
    public double? SpeedMph { get; init; }
}
