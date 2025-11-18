using GridPulse.Application.Abstractions;
using GridPulse.Application.Models;

namespace GridPulse.Application.Services;

internal sealed class OutageReadService(IOutageRepository repository) : IOutageReadService
{
    public async Task<IReadOnlyCollection<OutageSummary>> GetRecentAsync(CancellationToken cancellationToken = default)
    {
        var outages = await repository.GetRecentAsync(25, cancellationToken).ConfigureAwait(false);
        return outages.Select(MapToSummary).ToArray();
    }

    public async Task<OutageSummary?> GetByIdAsync(Guid outageId, CancellationToken cancellationToken = default)
    {
        var outage = await repository.GetByIdAsync(outageId, cancellationToken).ConfigureAwait(false);
        return outage is null ? null : MapToSummary(outage);
    }

    private static OutageSummary MapToSummary(Outage outage)
    {
        return new OutageSummary(
            outage.Id,
            outage.ServiceLocationId,
            outage.ServiceAddress,
            outage.Status,
            outage.ReportedAt,
            outage.LastUpdatedAt,
            outage.EstimatedRestoration,
            outage.Cause,
            outage.Events.Count
        );
    }
}
