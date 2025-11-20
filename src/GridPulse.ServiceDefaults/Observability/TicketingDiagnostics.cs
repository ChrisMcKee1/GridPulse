using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Npgsql;
using OpenTelemetry.Metrics;

namespace GridPulse.ServiceDefaults.Observability;

public static class TicketingDiagnostics
{
    public const string TicketAutomationMeter = "GridPulse.Ticketing.Automation";
    public const string DispatchMeter = "GridPulse.Dispatch.Recommendations";
    public const string CrewTelemetryMeter = "GridPulse.Crew.Telemetry";

    public static IReadOnlyList<string> MeterNames { get; } =
    [
        TicketAutomationMeter,
        DispatchMeter,
        CrewTelemetryMeter
    ];

    public const string TicketAutomationLogger = "GridPulse.Ticketing.Automation";
    public const string DispatchLogger = "GridPulse.Dispatch";
    public const string CrewTelemetryLogger = "GridPulse.Crew.Telemetry";

    public static MeterProviderBuilder AddTicketingMeters(this MeterProviderBuilder builder)
    {
        foreach (var meter in MeterNames)
        {
            builder.AddMeter(meter);
        }

        return builder;
    }

    public static ILoggingBuilder AddTicketingLogFilters(this ILoggingBuilder logging)
    {
        logging.AddFilter(TicketAutomationLogger, LogLevel.Information);
        logging.AddFilter(DispatchLogger, LogLevel.Information);
        logging.AddFilter(CrewTelemetryLogger, LogLevel.Information);
        return logging;
    }

    public static IServiceCollection AddTicketingHealthChecks(
        this IServiceCollection services,
        Action<TicketingHealthCheckOptions>? configure)
    {
        var options = new TicketingHealthCheckOptions();
        configure?.Invoke(options);

        var healthChecks = services.AddHealthChecks();

        if (!string.IsNullOrWhiteSpace(options.PostgresConnectionString))
        {
            healthChecks.AddTypeActivatedCheck<PostgresHealthCheck>(
                "ticketing-postgres",
                failureStatus: HealthStatus.Unhealthy,
                args: new object[] { options.PostgresConnectionString! });
        }

        healthChecks.AddCheck<TelemetryFeedHealthCheck>("ticketing-telemetry");
        healthChecks.AddCheck<AssignmentQueueHealthCheck>("ticketing-assignment-queue");

        return services;
    }
}

public sealed class TicketingHealthCheckOptions
{
    public string? PostgresConnectionString { get; set; }
}

internal sealed class PostgresHealthCheck(string connectionString) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await connection.CloseAsync().ConfigureAwait(false);
            return HealthCheckResult.Healthy("PostgreSQL connection opened successfully.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL connection failed.", ex);
        }
    }
}

internal sealed class TelemetryFeedHealthCheck(IServiceProvider services) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var monitor = services.GetService<ITelemetryFeedMonitor>();
        if (monitor is null)
        {
            return HealthCheckResult.Healthy("Telemetry feed monitor not registered yet.");
        }

        var status = await monitor.GetStatusAsync(cancellationToken).ConfigureAwait(false);
        return status.ToResult("telemetry feed");
    }
}

internal sealed class AssignmentQueueHealthCheck(IServiceProvider services) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var monitor = services.GetService<IAssignmentQueueMonitor>();
        if (monitor is null)
        {
            return HealthCheckResult.Healthy("Assignment queue monitor not registered yet.");
        }

        var status = await monitor.GetStatusAsync(cancellationToken).ConfigureAwait(false);
        return status.ToResult("assignment queue");
    }
}

public interface ITelemetryFeedMonitor
{
    ValueTask<ResourceHealthStatus> GetStatusAsync(CancellationToken cancellationToken);
}

public interface IAssignmentQueueMonitor
{
    ValueTask<ResourceHealthStatus> GetStatusAsync(CancellationToken cancellationToken);
}

public sealed record ResourceHealthStatus(bool IsHealthy, string? Description = null, TimeSpan? Age = null)
{
    public HealthCheckResult ToResult(string componentName)
    {
        var description = Description ?? $"{componentName} is {(IsHealthy ? "healthy" : "unhealthy")}.";
        var data = Age.HasValue ? new Dictionary<string, object> { ["ageSeconds"] = Age.Value.TotalSeconds } : null;
        return IsHealthy
            ? HealthCheckResult.Healthy(description, data)
            : HealthCheckResult.Unhealthy(description, null, data);
    }
}