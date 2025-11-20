using GridPulse.Application.Abstractions;
using GridPulse.Domain.Entities;
using GridPulse.Infrastructure.Persistence.SampleData;
using GridPulse.ServiceDefaults.Observability;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Metrics;

namespace GridPulse.Infrastructure.Telemetry;

internal sealed class MockCrewTelemetryFeed : BackgroundService, ITelemetryFeedMonitor
{
    private readonly Meter _meter = new(TicketingDiagnostics.CrewTelemetryMeter);
    private readonly Counter<int> _samplesCounter;
    private readonly Histogram<double> _latencyHistogram;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MockCrewTelemetryFeed> _logger;
    private readonly TimeSpan _cadence;
    private readonly ConcurrentDictionary<Guid, GeoPoint> _crewPositions = new();
    private DateTimeOffset _lastHeartbeat = DateTimeOffset.MinValue;
    private static readonly decimal LatitudeMin = 25m;
    private static readonly decimal LatitudeMax = 49m;
    private static readonly decimal LongitudeMin = -124m;
    private static readonly decimal LongitudeMax = -66m;

    public MockCrewTelemetryFeed(
        IServiceScopeFactory scopeFactory,
        IOptions<TicketSeedOptions> options,
        ILogger<MockCrewTelemetryFeed> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _cadence = options.Value.TelemetryCadence <= TimeSpan.Zero
            ? TimeSpan.FromSeconds(60)
            : options.Value.TelemetryCadence;
        _samplesCounter = _meter.CreateCounter<int>("gridpulse.telemetry.samples");
        _latencyHistogram = _meter.CreateHistogram<double>("gridpulse.telemetry.latency", unit: "ms");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var start = DateTimeOffset.UtcNow;
            try
            {
                await RefreshTelemetryAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Mock telemetry feed failed to refresh crew locations.");
            }
            var latency = (DateTimeOffset.UtcNow - start).TotalMilliseconds;
            _latencyHistogram.Record(latency);
            _lastHeartbeat = DateTimeOffset.UtcNow;
            await Task.Delay(_cadence, stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task RefreshTelemetryAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var dispatchRepository = scope.ServiceProvider.GetRequiredService<IDispatchRepository>();
        var crews = await dispatchRepository.GetCrewsAsync(cancellationToken).ConfigureAwait(false);
        if (crews.Count == 0)
        {
            _logger.LogInformation("No crews were found for telemetry refresh.");
            return;
        }

        await EnsureStartingPositionsAsync(dispatchRepository, crews, cancellationToken).ConfigureAwait(false);

        var snapshots = new List<CrewLocationSnapshot>(crews.Count);
        foreach (var crew in crews)
        {
            var position = NextPosition(crew.Id);
            var signalAge = Random.Shared.Next(0, (int)Math.Max(30, _cadence.TotalSeconds * 2));
            var now = DateTimeOffset.UtcNow;
            snapshots.Add(new CrewLocationSnapshot
            {
                Id = Guid.NewGuid(),
                CrewId = crew.Id,
                Latitude = position.Latitude,
                Longitude = position.Longitude,
                CapturedAt = now,
                SignalAgeSeconds = signalAge,
                IsStale = signalAge > _cadence.TotalSeconds * 2,
                SpeedMph = Math.Round(Random.Shared.NextDouble() * 55, 2)
            });
        }

        await dispatchRepository.AddLocationSnapshotsAsync(snapshots, cancellationToken).ConfigureAwait(false);
        await dispatchRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _samplesCounter.Add(snapshots.Count);
        _logger.LogInformation("Mock telemetry feed published {Count} snapshots.", snapshots.Count);
    }

    public ValueTask<ResourceHealthStatus> GetStatusAsync(CancellationToken cancellationToken)
    {
        var age = _lastHeartbeat == DateTimeOffset.MinValue
            ? (TimeSpan?)null
            : DateTimeOffset.UtcNow - _lastHeartbeat;
        var isHealthy = age is null || age < _cadence.Add(_cadence);
        return new ValueTask<ResourceHealthStatus>(new ResourceHealthStatus(isHealthy, "Telemetry feed", age));
    }

    private GeoPoint NextPosition(Guid crewId)
    {
        var current = _crewPositions.GetOrAdd(crewId, _ => GenerateStartingPoint());
        var lat = Clamp(current.Latitude + NextOffset(), LatitudeMin, LatitudeMax);
        var lon = Clamp(current.Longitude + NextOffset(), LongitudeMin, LongitudeMax);
        var updated = new GeoPoint(lat, lon);
        _crewPositions[crewId] = updated;
        return updated;
    }

    private static decimal Clamp(decimal value, decimal min, decimal max) => Math.Min(Math.Max(value, min), max);

    private static decimal NextOffset()
    {
        var delta = (Random.Shared.NextDouble() - 0.5d) * 0.02d;
        return Math.Round((decimal)delta, 6);
    }

    private static GeoPoint GenerateStartingPoint()
    {
        var latitude = Math.Round((decimal)(Random.Shared.NextDouble() * (double)(LatitudeMax - LatitudeMin) + (double)LatitudeMin), 6);
        var longitude = Math.Round((decimal)(Random.Shared.NextDouble() * (double)(LongitudeMax - LongitudeMin) + (double)LongitudeMin), 6);
        return new GeoPoint(latitude, longitude);
    }

    private async Task EnsureStartingPositionsAsync(IDispatchRepository repository, IReadOnlyCollection<Crew> crews, CancellationToken cancellationToken)
    {
        foreach (var crew in crews)
        {
            if (_crewPositions.ContainsKey(crew.Id))
            {
                continue;
            }

            var snapshot = await repository.GetLatestLocationAsync(crew.Id, cancellationToken).ConfigureAwait(false);
            if (snapshot is not null)
            {
                _crewPositions[crew.Id] = new GeoPoint(snapshot.Latitude, snapshot.Longitude);
            }
        }
    }

    private sealed record GeoPoint(decimal Latitude, decimal Longitude);
}

internal sealed class AssignmentQueueMonitor(ILogger<AssignmentQueueMonitor> logger) : IAssignmentQueueMonitor
{
    private readonly ILogger<AssignmentQueueMonitor> _logger = logger;

    public ValueTask<ResourceHealthStatus> GetStatusAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Assignment queue monitor invoked.");
        return new ValueTask<ResourceHealthStatus>(new ResourceHealthStatus(true, "Queue is idle."));
    }
}