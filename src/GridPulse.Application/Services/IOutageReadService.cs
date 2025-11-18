namespace GridPulse.Application.Services;

public interface IOutageReadService
{
    Task<IReadOnlyCollection<OutageSummary>> GetRecentAsync(CancellationToken cancellationToken = default);
    Task<OutageSummary?> GetByIdAsync(Guid outageId, CancellationToken cancellationToken = default);
}
