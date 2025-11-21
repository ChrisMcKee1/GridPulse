using System;
using GridPulse.Application.Abstractions;
using GridPulse.Infrastructure.Auth;
using GridPulse.Infrastructure.Persistence.SampleData;
using GridPulse.Infrastructure.Repositories;
using GridPulse.Infrastructure.Telemetry;
using GridPulse.ServiceDefaults.Observability;
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
        services.AddOptions<TicketSeedOptions>()
            .Bind(configuration.GetSection(TicketSeedOptions.SectionName))
            .ValidateOnStart();

        services.AddMockUserContext(configuration);

        services.AddScoped<IDataSeeder, OutageCsvSampleDataSeeder>();
        services.AddScoped<IDataSeeder, TicketSeed>();

        services.AddSingleton<MockCrewTelemetryFeed>();
        services.AddSingleton<ICrewTelemetryFeed>(sp => sp.GetRequiredService<MockCrewTelemetryFeed>());
        services.AddSingleton<ITelemetryFeedMonitor>(sp => sp.GetRequiredService<MockCrewTelemetryFeed>());
        services.AddSingleton<IAssignmentQueueMonitor, AssignmentQueueMonitor>();
        services.AddHostedService(sp => sp.GetRequiredService<MockCrewTelemetryFeed>());

        return services;
    }

    public static IServiceCollection AddTicketingRepositories(this IServiceCollection services)
    {
        services.AddScoped<IOutageRepository, EfOutageRepository>();
        services.AddScoped<ITicketRepository, EfTicketRepository>();
        services.AddScoped<IDispatchRepository, EfDispatchRepository>();
        services.AddScoped<IAssignmentDeliveryRepository, EfAssignmentDeliveryRepository>();
        return services;
    }

        public static IServiceCollection AddMockUserContext(this IServiceCollection services, IConfiguration configuration)
        {
            var authProvider = configuration["Authentication:Provider"] ?? "Mock";
            if (!string.Equals(authProvider, "Mock", StringComparison.OrdinalIgnoreCase))
            {
                throw new NotSupportedException($"Authentication provider '{authProvider}' is not implemented. Replace MockUserContext with the Entra-backed provider before flipping this flag.");
            }

            services.AddOptions<MockUserContextOptions>()
                .Bind(configuration.GetSection("Authentication:Mock"))
                .ValidateOnStart();
            services.AddSingleton<IUserContext, MockUserContext>();
            return services;
        }
}
