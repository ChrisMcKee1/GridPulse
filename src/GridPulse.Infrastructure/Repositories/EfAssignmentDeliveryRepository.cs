using System.Linq;
using GridPulse.Application.Abstractions;
using GridPulse.Domain.Entities;
using GridPulse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GridPulse.Infrastructure.Repositories;

internal sealed class EfAssignmentDeliveryRepository(GridPulseDbContext dbContext) : IAssignmentDeliveryRepository
{
    public async Task<AssignmentDelivery?> GetLatestAsync(Guid ticketId, Guid crewId, CancellationToken cancellationToken = default)
    {
        return await dbContext.AssignmentDeliveries
            .AsSplitQuery()
            .Include(delivery => delivery.Crew)
                .ThenInclude(crew => crew.LocationHistory)
            .Where(delivery => delivery.TicketId == ticketId && delivery.CrewId == crewId)
            .OrderByDescending(delivery => delivery.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(AssignmentDelivery delivery, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(delivery);
        await dbContext.AssignmentDeliveries.AddAsync(delivery, cancellationToken).ConfigureAwait(false);
    }

    public Task UpdateAsync(AssignmentDelivery delivery, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(delivery);
        dbContext.AssignmentDeliveries.Update(delivery);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}