using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using GridPulse.Application.Abstractions;
using GridPulse.Application.Models;
using GridPulse.Domain.Entities;
using GridPulse.Domain.Enums;
using GridPulse.ServiceDefaults.Observability;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace GridPulse.WebApi.Workers;

internal sealed class TicketAutomationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TicketAutomationWorker> _logger;
    private readonly TicketAutomationWorkerOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly Meter _meter = new(TicketingDiagnostics.TicketAutomationMeter);
    private readonly Counter<int> _ticketsCreated;
    private readonly Counter<int> _ticketsSuppressed;
    private readonly Histogram<double> _pollLatency;
    private readonly ConcurrentDictionary<Guid, DateTimeOffset> _processedOutages = new();

    public TicketAutomationWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<TicketAutomationWorkerOptions> options,
        ILogger<TicketAutomationWorker> logger,
        TimeProvider? timeProvider = null)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value ?? throw new InvalidOperationException("Ticket automation options were not configured.");
        _timeProvider = timeProvider ?? TimeProvider.System;
        _ticketsCreated = _meter.CreateCounter<int>("gridpulse.ticketing.automation.created");
        _ticketsSuppressed = _meter.CreateCounter<int>("gridpulse.ticketing.automation.suppressed");
        _pollLatency = _meter.CreateHistogram<double>("gridpulse.ticketing.automation.poll-latency", unit: "ms");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Ticket automation worker started with poll interval {Interval}.", _options.PollInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            var started = _timeProvider.GetUtcNow();
            try
            {
                await ProcessOutagesAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Swallow cancellation so hosted service can stop gracefully.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ticket automation worker failed to process outage batch.");
            }

            var elapsed = (_timeProvider.GetUtcNow() - started).TotalMilliseconds;
            _pollLatency.Record(elapsed);

            try
            {
                await Task.Delay(_options.PollInterval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task ProcessOutagesAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var outageRepository = scope.ServiceProvider.GetRequiredService<IOutageRepository>();
        var automationService = scope.ServiceProvider.GetRequiredService<ITicketAutomationService>();

        var outages = await outageRepository
            .GetRecentAsync(_options.OutageBatchSize, cancellationToken)
            .ConfigureAwait(false);

        if (outages.Count == 0)
        {
            return;
        }

        var now = _timeProvider.GetUtcNow();
        PruneProcessed(now);

        var createdCount = 0;
        var skippedCount = 0;

        foreach (var outage in outages)
        {
            if (!ShouldProcessOutage(outage, now))
            {
                continue;
            }

            var seedPayload = ToSeedPayload(outage, now);
            var result = await automationService
                .CreateOrUpdateAutomatedTicketAsync(seedPayload, cancellationToken)
                .ConfigureAwait(false);

            if (result is not null)
            {
                createdCount++;
                _ticketsCreated.Add(1);
                _logger.LogInformation("Automated ticket {TicketId} created for outage {OutageId}.", result.Id, outage.Id);
            }
            else
            {
                skippedCount++;
                _ticketsSuppressed.Add(1);
            }

            _processedOutages[outage.Id] = outage.ReportedAt == default ? now : outage.ReportedAt;
        }

        if (createdCount == 0 && skippedCount == 0)
        {
            return;
        }

        _logger.LogDebug("Ticket automation processed {CreatedCount} new tickets and skipped {SkippedCount} outages this cycle.", createdCount, skippedCount);
    }

    private bool ShouldProcessOutage(Outage outage, DateTimeOffset now)
    {
        if (outage.Status == OutageStatus.Restored)
        {
            return false;
        }

        if (_processedOutages.ContainsKey(outage.Id))
        {
            return false;
        }

        var reportedAt = outage.ReportedAt == default ? now : outage.ReportedAt;
        if (now - reportedAt > _options.MaxOutageAge)
        {
            return false;
        }

        return true;
    }

    private void PruneProcessed(DateTimeOffset now)
    {
        if (_processedOutages.IsEmpty)
        {
            return;
        }

        foreach (var entry in _processedOutages)
        {
            if (now - entry.Value > _options.ProcessedRetention)
            {
                _processedOutages.TryRemove(entry.Key, out _);
            }
        }
    }

    private static TicketSeedData ToSeedPayload(Outage outage, DateTimeOffset now)
    {
        var title = string.IsNullOrWhiteSpace(outage.ServiceAddress)
            ? $"Outage {outage.Id:N}"
            : $"Outage at {outage.ServiceAddress}";

        var affectedAssets = new[]
        {
            outage.ServiceLocationId == Guid.Empty ? outage.Id.ToString("N") : outage.ServiceLocationId.ToString()
        };

        return new TicketSeedData(
            outage.Id.ToString("D"),
            title,
            ResolvePriority(outage, now),
            outage.Cause,
            affectedAssets,
            0,
            outage.ReportedAt == default ? now : outage.ReportedAt);
    }

    private static TicketPriority ResolvePriority(Outage outage, DateTimeOffset now)
    {
        if (outage.Status == OutageStatus.CrewDispatched)
        {
            return TicketPriority.Critical;
        }

        if (outage.Status == OutageStatus.Acknowledged)
        {
            return TicketPriority.High;
        }

        if (outage.EstimatedRestoration is { } eta && eta <= now.AddHours(2))
        {
            return TicketPriority.High;
        }

        return TicketPriority.Medium;
    }

    public override void Dispose()
    {
        base.Dispose();
        _meter.Dispose();
    }
}

public sealed class TicketAutomationWorkerOptions
{
    public const string SectionName = "TicketAutomation";

    [Range(typeof(TimeSpan), "00:00:02", "1.00:00:00")]
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(5);

    [Range(1, 250)]
    public int OutageBatchSize { get; set; } = 25;

    [Range(typeof(TimeSpan), "00:05:00", "7.00:00:00")]
    public TimeSpan MaxOutageAge { get; set; } = TimeSpan.FromHours(12);

    [Range(typeof(TimeSpan), "00:05:00", "7.00:00:00")]
    public TimeSpan ProcessedRetention { get; set; } = TimeSpan.FromHours(1);
}
