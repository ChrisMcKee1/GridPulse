using GridPulse.Application.Abstractions;
using GridPulse.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GridPulse.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IOutageReadService, OutageReadService>();
        services.AddScoped<ITicketWriteService, TicketWriteService>();
        services.AddScoped<ITicketQueryService, TicketQueryService>();
        services.AddScoped<ITicketAutomationService, TicketAutomationService>();
        services.AddScoped<IDispatchOptimizationService, DispatchOptimizationService>();
        services.AddScoped<IDispatchAssignmentService, DispatchAssignmentService>();
        services.AddScoped<ICrewAssignmentDeliveryService, CrewAssignmentDeliveryService>();
        return services;
    }
}
