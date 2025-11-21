namespace GridPulse.Application.Abstractions;

public interface IDispatchRepository
{
    Task<IReadOnlyCollection<Crew>> GetCrewsAsync(CancellationToken cancellationToken = default);
    Task<Crew?> GetCrewByIdAsync(Guid crewId, CancellationToken cancellationToken = default);
    Task AddCrewAsync(Crew crew, CancellationToken cancellationToken = default);
    Task UpdateCrewAsync(Crew crew);
    Task<CrewLocationSnapshot?> GetLatestLocationAsync(Guid crewId, CancellationToken cancellationToken = default);
    Task AddLocationSnapshotsAsync(IEnumerable<CrewLocationSnapshot> snapshots, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<DispatchRecommendation>> GetRecommendationsForTicketAsync(Guid ticketId, CancellationToken cancellationToken = default);
    Task ReplaceRecommendationsForTicketAsync(Guid ticketId, IEnumerable<DispatchRecommendation> recommendations, CancellationToken cancellationToken = default);
    Task UpdateRecommendationsAsync(IEnumerable<DispatchRecommendation> recommendations);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}