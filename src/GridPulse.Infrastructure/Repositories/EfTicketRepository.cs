using GridPulse.Application.Abstractions;
using GridPulse.Application.Models;
using GridPulse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace GridPulse.Infrastructure.Repositories;

internal sealed class EfTicketRepository(GridPulseDbContext dbContext) : ITicketRepository
{
    public async Task<Ticket?> GetByIdAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        var ticket = await dbContext.Tickets
            .AsSplitQuery()
            .Include(t => t.Events)
            .Include(t => t.Recommendations)
                .ThenInclude(r => r.Crew)
                    .ThenInclude(c => c.LocationHistory)
            .FirstOrDefaultAsync(t => t.Id == ticketId, cancellationToken)
            .ConfigureAwait(false);

        return ticket;
    }

    public async Task<IReadOnlyCollection<Ticket>> GetAsync(TicketQueryOptions options, CancellationToken cancellationToken = default)
    {
        options ??= new TicketQueryOptions();

        var query = dbContext.Tickets.AsQueryable();

        var statuses = options.Statuses?.ToArray();
        if (statuses is { Length: > 0 })
        {
            var statusSet = statuses.ToHashSet();
            query = query.Where(ticket => statusSet.Contains(ticket.Status));
        }

        if (options.MinPriority is { } minPriority)
        {
            query = query.Where(ticket => ticket.Priority <= minPriority);
        }

        var includeRecommendations = options.IncludeRecommendations;
        var includeTimeline = options.IncludeTimeline;

        if (includeRecommendations)
        {
            query = query
                .Include(ticket => ticket.Recommendations)
                    .ThenInclude(recommendation => recommendation.Crew)
                        .ThenInclude(crew => crew.LocationHistory);
        }

        if (includeTimeline)
        {
            query = query.Include(ticket => ticket.Events);
        }

        if (includeRecommendations || includeTimeline)
        {
            query = query.AsSplitQuery();
        }

        query = query.OrderByDescending(ticket => ticket.OpenedAt);

        if (options.Take is { } take and > 0)
        {
            query = query.Take(take);
        }

        var tickets = await query
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return tickets;
    }

    public async Task<Ticket?> GetByAssignedCrewIdAsync(Guid crewId, CancellationToken cancellationToken = default)
    {
        if (crewId == Guid.Empty)
        {
            throw new ArgumentException("Crew identifier is required.", nameof(crewId));
        }

        var ticket = await dbContext.Tickets
            .AsSplitQuery()
            .Include(ticket => ticket.Events)
            .Include(ticket => ticket.Recommendations)
                .ThenInclude(recommendation => recommendation.Crew)
            .AsNoTracking()
            .Where(ticket => ticket.AssignedCrewId == crewId)
            .OrderByDescending(ticket => ticket.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return ticket;
    }

    public async Task AddAsync(Ticket ticket, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        await dbContext.Tickets.AddAsync(ticket, cancellationToken).ConfigureAwait(false);
    }

    public Task UpdateAsync(Ticket ticket, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ticket);

        var local = dbContext.Tickets.Local.FirstOrDefault(t => t.Id == ticket.Id);
        if (local is not null)
        {
            dbContext.Entry(local).State = EntityState.Detached;
        }

        dbContext.Tickets.Update(ticket);
        ApplyAuditVersionOriginalValue(ticket);

        return Task.CompletedTask;
    }

    private void ApplyAuditVersionOriginalValue(Ticket ticket)
    {
        var entry = dbContext.Entry(ticket);
        var auditProperty = entry.Property(t => t.AuditVersion);
        if (!auditProperty.Metadata.IsConcurrencyToken)
        {
            return;
        }

        var currentVersion = ticket.AuditVersion;
        if (currentVersion <= 0)
        {
            auditProperty.OriginalValue = currentVersion;
            return;
        }

        // Immutable ticket clones always bump the audit version by 1; expose the prior value so EF's concurrency check succeeds.
        auditProperty.OriginalValue = currentVersion - 1;
    }

    public async Task AddEventsAsync(IEnumerable<AssignmentEvent> events, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(events);
        await dbContext.AssignmentEvents.AddRangeAsync(events, cancellationToken).ConfigureAwait(false);
    }

    public async Task AddRecommendationsAsync(IEnumerable<DispatchRecommendation> recommendations, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(recommendations);
        await dbContext.DispatchRecommendations.AddRangeAsync(recommendations, cancellationToken).ConfigureAwait(false);
    }

    public Task<bool> ExistsByOutageReferenceAsync(string outageReferenceId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(outageReferenceId))
        {
            throw new ArgumentException("Outage reference identifier is required.", nameof(outageReferenceId));
        }

        return dbContext.Tickets.AnyAsync(
            ticket => ticket.OutageReferenceId == outageReferenceId,
            cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}