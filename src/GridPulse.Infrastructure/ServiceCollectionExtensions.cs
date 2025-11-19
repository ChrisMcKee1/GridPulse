using GridPulse.Infrastructure.Persistence.SampleData;
using GridPulse.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GridPulse.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<SampleDataOptions>()
                .Bind(configuration.GetSection(SampleDataOptions.SectionName))
                .ValidateOnStart();

        services.AddScoped<IOutageRepository, EfOutageRepository>();
        services.AddScoped<IDataSeeder, OutageCsvSampleDataSeeder>();
        return services;
    }
}
