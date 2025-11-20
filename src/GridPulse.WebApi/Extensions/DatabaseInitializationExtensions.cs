using GridPulse.Infrastructure.Persistence;
using GridPulse.Infrastructure.Persistence.SampleData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GridPulse.WebApi.Extensions;

internal static class DatabaseInitializationExtensions
{
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<GridPulseDbContext>();
        var database = context.Database;
        if (database.IsRelational())
        {
            await database.MigrateAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
        }

        var seeders = scope.ServiceProvider.GetServices<IDataSeeder>();
        foreach (var seeder in seeders)
        {
            await seeder.SeedAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
