using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace GridPulse.Infrastructure.Persistence;

internal sealed class DesignTimeGridPulseDbContextFactory : IDesignTimeDbContextFactory<GridPulseDbContext>
{
    public GridPulseDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("gridpulse-db")
            ?? "Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=gridpulse";

        var optionsBuilder = new DbContextOptionsBuilder<GridPulseDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new GridPulseDbContext(optionsBuilder.Options);
    }
}
