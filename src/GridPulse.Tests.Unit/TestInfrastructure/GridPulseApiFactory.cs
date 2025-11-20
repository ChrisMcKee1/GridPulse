extern alias GridPulseWebApi;

using GridPulse.Infrastructure.Persistence;
using GridPulse.Infrastructure.Persistence.SampleData;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GridPulse.Tests.Unit.TestInfrastructure;

public sealed class GridPulseApiFactory : WebApplicationFactory<GridPulseWebApi::Program>
{
    private readonly string _databaseName = $"GridPulseApiTests_{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, configBuilder) =>
        {
            var overrides = new Dictionary<string, string?>
            {
                ["Authentication:Provider"] = "Mock",
                [$"{SampleDataOptions.SectionName}:RootPath"] = "sample-data"
            };

            configBuilder.AddInMemoryCollection(overrides);
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<GridPulseDbContext>));
            services.AddDbContext<GridPulseDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
            });

            services.RemoveAll(typeof(IDataSeeder));

            var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<GridPulseDbContext>();
            dbContext.Database.EnsureCreated();
        });
    }

    public Task ExecuteScopedAsync(Func<IServiceProvider, Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        using var scope = Services.CreateScope();
        return action(scope.ServiceProvider);
    }

    public Task ExecuteDbContextAsync(Func<GridPulseDbContext, Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        return ExecuteScopedAsync(sp => action(sp.GetRequiredService<GridPulseDbContext>()));
    }
}