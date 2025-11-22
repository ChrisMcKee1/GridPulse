using GridPulse.Application.Abstractions;
using GridPulse.Application.Models;
using GridPulse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace GridPulse.Infrastructure.Repositories;

internal sealed class EfTicketRepository : ITicketRepository
{
    private readonly GridPulseDbContext _dbContext;

    private DbSet<Ticket> Tickets => _dbContext.Set<Ticket>();

    private DbSet<AssignmentEvent> AssignmentEvents => _dbContext.Set<AssignmentEvent>();

    private DbSet<DispatchRecommendation> DispatchRecommendations => _dbContext.Set<DispatchRecommendation>();

    public EfTicketRepository(GridPulseDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<Ticket?> GetByIdAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        var ticket = await Tickets
            .AsSplitQuery()
            .Include(t => t.Events)
            .Include(t => t.Recommendations)
                .ThenInclude(r => r.Crew)
                    .ThenInclude(c => c!.LocationHistory)
            .FirstOrDefaultAsync(t => t.Id == ticketId, cancellationToken)
            .ConfigureAwait(false);

        return ticket;
    }

    public async Task<IReadOnlyCollection<Ticket>> GetAsync(TicketQueryOptions options, CancellationToken cancellationToken = default)
    {
        options ??= new TicketQueryOptions();

        var query = Tickets.AsQueryable();

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
                        .ThenInclude(crew => crew!.LocationHistory);
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

        var ticket = await Tickets
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

    public async Task<IReadOnlyCollection<Ticket>> GetByIdsAsync(IEnumerable<Guid> ticketIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ticketIds);
        var ticketIdSet = ticketIds.ToHashSet();

        var tickets = await Tickets
            .AsNoTracking()
            .Where(t => ticketIdSet.Contains(t.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return tickets;
    }

    public async Task AddAsync(Ticket ticket, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        await Tickets.AddAsync(ticket, cancellationToken).ConfigureAwait(false);
    }

    public async Task AddRangeAsync(IEnumerable<Ticket> tickets, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tickets);
        await Tickets.AddRangeAsync(tickets, cancellationToken).ConfigureAwait(false);
    }

    public Task UpdateAsync(Ticket ticket, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ticket);

        var local = Tickets.Local.FirstOrDefault(t => t.Id == ticket.Id);
        if (local is not null)
        {
            _dbContext.Entry(local).State = EntityState.Detached;
        }

        Tickets.Update(ticket);
        ApplyAuditVersionOriginalValue(ticket);

        return Task.CompletedTask;
    }

    private void ApplyAuditVersionOriginalValue(Ticket ticket)
    {
        var entry = _dbContext.Entry(ticket);
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

    public Task UpdateRangeAsync(IEnumerable<Ticket> tickets, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tickets);

        foreach (var ticket in tickets)
        {
            var local = Tickets.Local.FirstOrDefault(t => t.Id == ticket.Id);
            if (local is not null)
            {
                _dbContext.Entry(local).State = EntityState.Detached;
            }

            Tickets.Update(ticket);
            ApplyAuditVersionOriginalValue(ticket);
        }

        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        var ticket = await Tickets
            .FirstOrDefaultAsync(t => t.Id == ticketId, cancellationToken)
            .ConfigureAwait(false);

        if (ticket is not null)
        {
            Tickets.Remove(ticket);
        }
    }

    public async Task DeleteRangeAsync(IEnumerable<Guid> ticketIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ticketIds);
        var ticketIdSet = ticketIds.ToHashSet();

        var tickets = await Tickets
            .Where(t => ticketIdSet.Contains(t.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (tickets.Count > 0)
        {
            Tickets.RemoveRange(tickets);
        }
    }

    public async Task AddEventsAsync(IEnumerable<AssignmentEvent> events, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(events);
        await AssignmentEvents.AddRangeAsync(events, cancellationToken).ConfigureAwait(false);
    }

    public async Task AddRecommendationsAsync(IEnumerable<DispatchRecommendation> recommendations, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(recommendations);
        await DispatchRecommendations.AddRangeAsync(recommendations, cancellationToken).ConfigureAwait(false);
    }

    public Task<bool> ExistsByOutageReferenceAsync(string outageReferenceId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(outageReferenceId))
        {
            throw new ArgumentException("Outage reference identifier is required.", nameof(outageReferenceId));
        }

        return Tickets.AnyAsync(
            ticket => ticket.OutageReferenceId == outageReferenceId,
            cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

}