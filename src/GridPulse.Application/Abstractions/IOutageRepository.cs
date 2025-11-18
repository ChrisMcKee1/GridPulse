namespace GridPulse.Application.Abstractions;

public interface IOutageRepository
{
    Task<IReadOnlyCollection<Outage>> GetRecentAsync(int take, CancellationToken cancellationToken = default);
    Task<Outage?> GetByIdAsync(Guid outageId, CancellationToken cancellationToken = default);
}
