using GridPulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GridPulse.Infrastructure.Persistence;

public sealed class GridPulseDbContext(DbContextOptions<GridPulseDbContext> options) : DbContext(options)
{
    public DbSet<Outage> Outages => Set<Outage>();
    public DbSet<OutageEvent> OutageEvents => Set<OutageEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GridPulseDbContext).Assembly);
    }
}
