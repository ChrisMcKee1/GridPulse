using GridPulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GridPulse.Infrastructure.Persistence;

public sealed class GridPulseDbContext(DbContextOptions<GridPulseDbContext> options) : DbContext(options)
{
    public DbSet<Outage> Outages => Set<Outage>();
    public DbSet<OutageEvent> OutageEvents => Set<OutageEvent>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<DispatchRecommendation> DispatchRecommendations => Set<DispatchRecommendation>();
    public DbSet<Crew> Crews => Set<Crew>();
    public DbSet<CrewLocationSnapshot> CrewLocationSnapshots => Set<CrewLocationSnapshot>();
    public DbSet<AssignmentEvent> AssignmentEvents => Set<AssignmentEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GridPulseDbContext).Assembly);
    }
}
