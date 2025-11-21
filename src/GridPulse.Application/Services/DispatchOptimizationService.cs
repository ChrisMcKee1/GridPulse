using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using GridPulse.Application.Abstractions;
using GridPulse.Application.Models;
using GridPulse.Domain.Entities;
using GridPulse.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace GridPulse.Application.Services;

internal sealed class DispatchOptimizationService : IDispatchOptimizationService
{
    private const double DistanceWeight = 0.5;
    private const double SkillWeight = 0.3;
    private const double WorkloadWeight = 0.2;
    private const double DefaultSkillPenalty = 0.35;
    private const int DefaultEtaMinutes = 45;
    private const int MaxEtaMinutes = 180;
    private const int MinEtaMinutes = 5;
    private const double MaxWorkload = 5d;
    private const double TelemetryDistancePenalty = 0.25;
    private const double TelemetryCompositePenalty = 0.3;
    private const int TelemetryEtaPenaltyMinutes = 60;

    private readonly ITicketRepository _ticketRepository;
    private readonly IDispatchRepository _dispatchRepository;
    private readonly ICrewTelemetryFeed _telemetryFeed;
    private readonly IUserContext _userContext;
    private readonly ILogger<DispatchOptimizationService> _logger;
    private readonly TimeProvider _timeProvider;

    public DispatchOptimizationService(
        ITicketRepository ticketRepository,
        IDispatchRepository dispatchRepository,
        ICrewTelemetryFeed telemetryFeed,
        IUserContext userContext,
        ILogger<DispatchOptimizationService> logger,
        TimeProvider? timeProvider = null)
    {
        _ticketRepository = ticketRepository;
        _dispatchRepository = dispatchRepository;
        _telemetryFeed = telemetryFeed;
        _userContext = userContext;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<DispatchRecommendationsEnvelope> GetRecommendationsAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        if (ticketId == Guid.Empty)
        {
            throw new ArgumentException("Ticket identifier is required.", nameof(ticketId));
        }

        var ticket = await _ticketRepository
            .GetByIdAsync(ticketId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} was not found.");

        var crews = await _dispatchRepository
            .GetCrewsAsync(cancellationToken)
            .ConfigureAwait(false);

        if (crews.Count == 0)
        {
            _logger.LogWarning("No crews available for ticket {TicketId}; returning empty recommendation set.", ticketId);
            return new DispatchRecommendationsEnvelope(ToSummary(ticket), Array.Empty<DispatchRecommendationDto>());
        }

        var snapshots = await _telemetryFeed
            .GetLatestSnapshotsAsync(cancellationToken)
            .ConfigureAwait(false);

        var snapshotLookup = snapshots.ToDictionary(snapshot => snapshot.CrewId, snapshot => snapshot);
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var scoredCrews = crews
            .Select(crew =>
            {
                var snapshot = snapshotLookup.TryGetValue(crew.Id, out var entry) ? entry : null;
                var scoring = ScoreCrew(ticket, crew, snapshot);
                return new CrewScore(crew, snapshot, scoring);
            })
            .OrderByDescending(score => score.Scoring.CompositeScore)
            .ThenBy(score => score.Scoring.EtaMinutes)
            .ToList();

        var domainRecommendations = new List<DispatchRecommendation>(scoredCrews.Count);
        var dtoRecommendations = new List<DispatchRecommendationDto>(scoredCrews.Count);

        for (var index = 0; index < scoredCrews.Count; index++)
        {
            var scored = scoredCrews[index];
            var isAutoSelected = index == 0;
            var recommendationId = Guid.NewGuid();
            var scoreComponents = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["distance"] = scored.Scoring.DistanceScore,
                ["skill"] = scored.Scoring.SkillScore,
                ["workload"] = scored.Scoring.WorkloadScore
            };

            var domain = new DispatchRecommendation
            {
                Id = recommendationId,
                TicketId = ticket.Id,
                CrewId = scored.Crew.Id,
                CompositeScore = scored.Scoring.CompositeScore,
                ScoreComponents = scoreComponents,
                RecommendedRouteEtaMinutes = scored.Scoring.EtaMinutes,
                IsAutoSelected = isAutoSelected,
                IsOverride = false,
                CreatedAt = now,
                ExpiresAt = now.AddMinutes(5)
            };

            domainRecommendations.Add(domain);

            var crewDto = MapCrewStatusDto(scored.Crew, scored.Snapshot, scored.Scoring.IsTelemetryStale);
            var dto = new DispatchRecommendationDto(
                recommendationId,
                ticket.Id,
                crewDto,
                scored.Scoring.CompositeScore,
                new ReadOnlyDictionary<string, double>(scoreComponents),
                scored.Scoring.EtaMinutes,
                isAutoSelected,
                false,
                null,
                now,
                domain.ExpiresAt);

            dtoRecommendations.Add(dto);
        }

        await _dispatchRepository
            .ReplaceRecommendationsForTicketAsync(ticket.Id, domainRecommendations, cancellationToken)
            .ConfigureAwait(false);
        await _dispatchRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await AppendRecommendationEventAsync(ticket, dtoRecommendations.Count > 0 ? dtoRecommendations[0] : null, now, cancellationToken).ConfigureAwait(false);

        return new DispatchRecommendationsEnvelope(ToSummary(ticket), dtoRecommendations);
    }

    private async Task AppendRecommendationEventAsync(
        Ticket ticket,
        DispatchRecommendationDto? topRecommendation,
        DateTime currentTimestamp,
        CancellationToken cancellationToken)
    {
        if (topRecommendation is null)
        {
            return;
        }

        var details = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["crewId"] = topRecommendation.Crew.CrewId.ToString(),
            ["crewName"] = topRecommendation.Crew.DisplayName,
            ["score"] = topRecommendation.CompositeScore.ToString("0.###", CultureInfo.InvariantCulture),
            ["etaMinutes"] = topRecommendation.EtaMinutes.ToString(CultureInfo.InvariantCulture)
        };

        var recommendationEvent = new AssignmentEvent
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            EventType = AssignmentEventType.RecommendationGenerated,
            Actor = ResolveActor(),
            OccurredAt = currentTimestamp,
            Details = details
        };

        await _ticketRepository.AddEventsAsync(new[] { recommendationEvent }, cancellationToken).ConfigureAwait(false);
        await _ticketRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private (int EtaMinutes, double DistanceScore, double SkillScore, double WorkloadScore, double CompositeScore, bool IsTelemetryStale) ScoreCrew(
        Ticket ticket,
        Crew crew,
        CrewLocationSnapshot? snapshot)
    {
        var eta = EstimateEtaMinutes(ticket, crew, snapshot);
        var distanceScore = 1d - Math.Clamp(eta / 90d, 0d, 1d);

        var targetSkill = ResolveTargetSkill(ticket);
        var hasSkill = crew.Skills?.Contains(targetSkill) == true;
        var skillScore = hasSkill ? 1d : DefaultSkillPenalty;

        var workloadScore = 1d - Math.Clamp(crew.CurrentTicketCount / MaxWorkload, 0d, 1d);

        var composite = Math.Round(
            (DistanceWeight * distanceScore) +
            (SkillWeight * skillScore) +
            (WorkloadWeight * workloadScore),
            4,
            MidpointRounding.AwayFromZero);

        var telemetryStale = snapshot is null || snapshot.IsStale;
        if (telemetryStale)
        {
            distanceScore = Math.Max(0d, distanceScore - TelemetryDistancePenalty);
            composite = Math.Round(
                (DistanceWeight * distanceScore) +
                (SkillWeight * skillScore) +
                (WorkloadWeight * workloadScore),
                4,
                MidpointRounding.AwayFromZero);
            composite = Math.Max(0d, composite - TelemetryCompositePenalty);
        }

        return (eta, distanceScore, skillScore, workloadScore, composite, telemetryStale);
    }

    private static CrewStatusDto MapCrewStatusDto(Crew crew, CrewLocationSnapshot? snapshot, bool isTelemetryStale)
    {
        CrewLocationSnapshotDto? locationDto = null;
        if (snapshot is not null)
        {
            locationDto = new CrewLocationSnapshotDto(
                snapshot.Latitude,
                snapshot.Longitude,
                snapshot.CapturedAt,
                snapshot.SpeedMph);
        }

        var skills = crew.Skills?.ToArray() ?? Array.Empty<CrewSkill>();

        return new CrewStatusDto(
            crew.Id,
            crew.DisplayName,
            crew.Status,
            crew.CurrentTicketCount,
            skills,
            crew.LastStatusUpdate,
            locationDto,
            isTelemetryStale);
    }

    private static CrewSkill ResolveTargetSkill(Ticket ticket)
    {
        if ((ticket.AffectedAssets ?? Array.Empty<string>()).Any(asset => asset.Contains("TX", StringComparison.OrdinalIgnoreCase)))
        {
            return CrewSkill.Transformer;
        }

        if ((ticket.AffectedAssets ?? Array.Empty<string>()).Any(asset => asset.Contains("UG", StringComparison.OrdinalIgnoreCase)))
        {
            return CrewSkill.Underground;
        }

        return ticket.Priority switch
        {
            TicketPriority.Critical => CrewSkill.HighVoltage,
            TicketPriority.High => CrewSkill.Distribution,
            TicketPriority.Medium => CrewSkill.Distribution,
            _ => CrewSkill.Fiber
        };
    }

    private static int EstimateEtaMinutes(Ticket ticket, Crew crew, CrewLocationSnapshot? snapshot)
    {
        var hash = Math.Abs(HashCode.Combine(ticket.Id, crew.Id)) % 40;
        var baseMinutes = DefaultEtaMinutes + hash + (crew.CurrentTicketCount * 5);
        if (snapshot is null)
        {
            return Math.Clamp(baseMinutes + 15, MinEtaMinutes, MaxEtaMinutes);
        }

        var signalPenalty = snapshot.SignalAgeSeconds switch
        {
            >= 600 => 35,
            >= 300 => 25,
            >= 180 => 15,
            >= 60 => 5,
            _ => 0
        };

        var stalePenalty = snapshot.IsStale ? TelemetryEtaPenaltyMinutes : 0;
        return Math.Clamp(baseMinutes + signalPenalty + stalePenalty, MinEtaMinutes, MaxEtaMinutes);
    }

    private static TicketSummaryDto ToSummary(Ticket ticket)
    {
        return new TicketSummaryDto(
            ticket.Id,
            ticket.Title,
            ticket.Priority,
            ticket.Status,
            ticket.OpenedAt,
            ticket.CustomerImpact,
            ticket.AssignedCrewId,
            null);
    }

    private string ResolveActor()
    {
        var identity = _userContext.Current ?? UserIdentity.Anonymous;
        return string.IsNullOrWhiteSpace(identity.DisplayName) ? "system" : identity.DisplayName;
    }

    private sealed record CrewScore(Crew Crew, CrewLocationSnapshot? Snapshot, (int EtaMinutes, double DistanceScore, double SkillScore, double WorkloadScore, double CompositeScore, bool IsTelemetryStale) Scoring);
}