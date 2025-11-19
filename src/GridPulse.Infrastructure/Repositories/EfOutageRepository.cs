using GridPulse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GridPulse.Infrastructure.Repositories;

internal sealed class EfOutageRepository(GridPulseDbContext dbContext) : IOutageRepository
{
    public async Task<IReadOnlyCollection<Outage>> GetRecentAsync(int take, CancellationToken cancellationToken = default)
    {
        var outages = await dbContext.Outages
            .AsNoTracking()
            .Include(o => o.Events)
            .OrderByDescending(o => o.ReportedAt)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return outages;
    }

    public async Task<Outage?> GetByIdAsync(Guid outageId, CancellationToken cancellationToken = default)
    {
        var outage = await dbContext.Outages
            .AsNoTracking()
            .Include(o => o.Events)
            .FirstOrDefaultAsync(o => o.Id == outageId, cancellationToken)
            .ConfigureAwait(false);

        return outage;
    }
}
