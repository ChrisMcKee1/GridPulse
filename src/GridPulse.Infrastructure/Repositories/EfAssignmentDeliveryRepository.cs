using System;
using System.Linq;
using GridPulse.Application.Abstractions;
using GridPulse.Domain.Entities;
using GridPulse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GridPulse.Infrastructure.Repositories;

internal sealed class EfAssignmentDeliveryRepository : IAssignmentDeliveryRepository
{
    private readonly GridPulseDbContext _dbContext;

    private DbSet<AssignmentDelivery> AssignmentDeliveries => _dbContext.Set<AssignmentDelivery>();

    public EfAssignmentDeliveryRepository(GridPulseDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<AssignmentDelivery?> GetLatestAsync(Guid ticketId, Guid crewId, CancellationToken cancellationToken = default)
    {
        return await AssignmentDeliveries
            .AsSplitQuery()
            .Include(delivery => delivery.Crew)
                .ThenInclude(crew => crew!.LocationHistory)
            .Where(delivery => delivery.TicketId == ticketId && delivery.CrewId == crewId)
            .OrderByDescending(delivery => delivery.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(AssignmentDelivery delivery, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(delivery);
        await AssignmentDeliveries.AddAsync(delivery, cancellationToken).ConfigureAwait(false);
    }

    public Task UpdateAsync(AssignmentDelivery delivery, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(delivery);
        AssignmentDeliveries.Update(delivery);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

}