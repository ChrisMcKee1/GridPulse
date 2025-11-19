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
        await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);

        var seeders = scope.ServiceProvider.GetServices<IDataSeeder>();
        foreach (var seeder in seeders)
        {
            await seeder.SeedAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
