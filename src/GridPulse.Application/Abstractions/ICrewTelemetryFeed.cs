namespace GridPulse.Application.Abstractions;

public interface ICrewTelemetryFeed
{
    Task<IReadOnlyCollection<CrewLocationSnapshot>> GetLatestSnapshotsAsync(CancellationToken cancellationToken = default);
    Task<CrewLocationSnapshot?> GetLatestForCrewAsync(Guid crewId, CancellationToken cancellationToken = default);
    Task RecordSnapshotAsync(CrewLocationSnapshot snapshot, CancellationToken cancellationToken = default);
    Task<DateTimeOffset?> GetLastHeartbeatAsync(CancellationToken cancellationToken = default);
}
