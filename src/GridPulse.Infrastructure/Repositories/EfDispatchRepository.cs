using GridPulse.Application.Abstractions;
using GridPulse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GridPulse.Infrastructure.Repositories;

internal sealed class EfDispatchRepository(GridPulseDbContext dbContext) : IDispatchRepository
{
    public async Task<IReadOnlyCollection<Crew>> GetCrewsAsync(CancellationToken cancellationToken = default)
    {
        var crews = await dbContext.Crews
            .AsNoTracking()
            .OrderBy(crew => crew.DisplayName)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return crews;
    }

    public async Task<IReadOnlyCollection<Crew>> GetCrewsByIdsAsync(IEnumerable<Guid> crewIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(crewIds);
        var crewIdSet = crewIds.ToHashSet();
        
        var crews = await dbContext.Crews
            .AsNoTracking()
            .Where(c => crewIdSet.Contains(c.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return crews;
    }

    public async Task<Crew?> GetCrewByIdAsync(Guid crewId, CancellationToken cancellationToken = default)
    {
        var crew = await dbContext.Crews
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == crewId, cancellationToken)
            .ConfigureAwait(false);

        return crew;
    }

    public async Task AddCrewAsync(Crew crew, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(crew);
        await dbContext.Crews.AddAsync(crew, cancellationToken).ConfigureAwait(false);
    }

    public async Task AddCrewsAsync(IEnumerable<Crew> crews, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(crews);
        await dbContext.Crews.AddRangeAsync(crews, cancellationToken).ConfigureAwait(false);
    }

    public Task UpdateCrewAsync(Crew crew)
    {
        ArgumentNullException.ThrowIfNull(crew);
        
        var existingEntry = dbContext.ChangeTracker.Entries<Crew>()
            .FirstOrDefault(e => e.Entity.Id == crew.Id);
        
        if (existingEntry is not null)
        {
            existingEntry.CurrentValues.SetValues(crew);
        }
        else
        {
            dbContext.Crews.Update(crew);
        }
        
        return Task.CompletedTask;
    }

    public Task UpdateCrewsAsync(IEnumerable<Crew> crews)
    {
        ArgumentNullException.ThrowIfNull(crews);
        dbContext.Crews.UpdateRange(crews);
        return Task.CompletedTask;
    }

    public async Task DeleteCrewAsync(Guid crewId, CancellationToken cancellationToken = default)
    {
        var crew = await dbContext.Crews
            .FirstOrDefaultAsync(c => c.Id == crewId, cancellationToken)
            .ConfigureAwait(false);

        if (crew is not null)
        {
            dbContext.Crews.Remove(crew);
        }
    }

    public async Task DeleteCrewsAsync(IEnumerable<Guid> crewIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(crewIds);
        var crewIdSet = crewIds.ToHashSet();
        
        var crews = await dbContext.Crews
            .Where(c => crewIdSet.Contains(c.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (crews.Count > 0)
        {
            dbContext.Crews.RemoveRange(crews);
        }
    }

    public async Task<CrewLocationSnapshot?> GetLatestLocationAsync(Guid crewId, CancellationToken cancellationToken = default)
    {
        var snapshot = await dbContext.CrewLocationSnapshots
            .AsNoTracking()
            .Where(snapshot => snapshot.CrewId == crewId)
            .OrderByDescending(snapshot => snapshot.CapturedAt)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return snapshot;
    }

    public async Task AddLocationSnapshotsAsync(IEnumerable<CrewLocationSnapshot> snapshots, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshots);
        await dbContext.CrewLocationSnapshots.AddRangeAsync(snapshots, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyCollection<DispatchRecommendation>> GetRecommendationsForTicketAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        var recommendations = await dbContext.DispatchRecommendations
            .AsNoTracking()
            .AsSplitQuery()
            .Include(recommendation => recommendation.Crew!)
            .Where(recommendation => recommendation.TicketId == ticketId)
            .OrderByDescending(recommendation => recommendation.CompositeScore)
            .ThenByDescending(recommendation => recommendation.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return recommendations;
    }

    public async Task ReplaceRecommendationsForTicketAsync(
        Guid ticketId,
        IEnumerable<DispatchRecommendation> recommendations,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(recommendations);

        var existing = await dbContext.DispatchRecommendations
            .Where(recommendation => recommendation.TicketId == ticketId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (existing.Count > 0)
        {
            dbContext.DispatchRecommendations.RemoveRange(existing);
        }

        await dbContext.DispatchRecommendations
            .AddRangeAsync(recommendations, cancellationToken)
            .ConfigureAwait(false);
    }

    public Task UpdateRecommendationsAsync(IEnumerable<DispatchRecommendation> recommendations)
    {
        ArgumentNullException.ThrowIfNull(recommendations);
        dbContext.DispatchRecommendations.UpdateRange(recommendations);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}