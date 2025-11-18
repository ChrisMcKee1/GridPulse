using GridPulse.Infrastructure.Options;
using GridPulse.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GridPulse.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OracleOptions>(configuration.GetSection(OracleOptions.SectionName));
        services.AddSingleton<IOutageRepository, InMemoryOutageRepository>();
        return services;
    }
}
