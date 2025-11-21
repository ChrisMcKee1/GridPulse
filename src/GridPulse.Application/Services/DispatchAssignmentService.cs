using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GridPulse.Application.Abstractions;
using GridPulse.Application.Exceptions;
using GridPulse.Application.Models;
using GridPulse.Domain.Entities;
using GridPulse.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace GridPulse.Application.Services;

internal sealed class DispatchAssignmentService : IDispatchAssignmentService
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IDispatchRepository _dispatchRepository;
    private readonly ICrewAssignmentDeliveryService _crewDeliveryService;
    private readonly ITicketWriteService _ticketWriteService;
    private readonly ILogger<DispatchAssignmentService> _logger;
    private readonly IUserContext _userContext;
    private readonly TimeProvider _timeProvider;

    public DispatchAssignmentService(
        ITicketRepository ticketRepository,
        IDispatchRepository dispatchRepository,
        ICrewAssignmentDeliveryService crewDeliveryService,
        ITicketWriteService ticketWriteService,
        ILogger<DispatchAssignmentService> logger,
        IUserContext userContext,
        TimeProvider? timeProvider = null)
    {
        _ticketRepository = ticketRepository;
        _dispatchRepository = dispatchRepository;
        _crewDeliveryService = crewDeliveryService;
        _ticketWriteService = ticketWriteService;
        _logger = logger;
        _userContext = userContext;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<AssignmentReceiptDto> PublishAssignmentAsync(DispatchAssignmentRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureIdentifiers(request);

        var ticket = await _ticketRepository
            .GetByIdAsync(request.TicketId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {request.TicketId} was not found.");

        var crew = await _dispatchRepository
            .GetCrewByIdAsync(request.CrewId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Crew {request.CrewId} was not found.");

        var recommendations = await _dispatchRepository
            .GetRecommendationsForTicketAsync(ticket.Id, cancellationToken)
            .ConfigureAwait(false);

        var sortedRecommendations = recommendations
            .OrderByDescending(r => r.CompositeScore)
            .ThenByDescending(r => r.CreatedAt)
            .ToList();

        var chosenRecommendation = sortedRecommendations.FirstOrDefault(r => r.CrewId == crew.Id);
        var autoRecommendation = sortedRecommendations.FirstOrDefault(r => r.IsAutoSelected) ?? sortedRecommendations.FirstOrDefault();

        var requiresOverride = autoRecommendation is not null && autoRecommendation.CrewId != crew.Id;
        if (requiresOverride && string.IsNullOrWhiteSpace(request.OverrideReason))
        {
            throw new ArgumentException("Override justification is required when selecting a non auto-selected crew.", nameof(request));
        }

        var timestamp = _timeProvider.GetUtcNow();
        if (chosenRecommendation?.ExpiresAt is DateTimeOffset expiresAt && expiresAt < timestamp)
        {
            throw new DispatchWorkflowException("Recommendation is stale. Refresh recommendations before dispatching.");
        }

        var latestSnapshot = await _dispatchRepository
            .GetLatestLocationAsync(crew.Id, cancellationToken)
            .ConfigureAwait(false);

        if (latestSnapshot is { IsStale: true })
        {
            throw new DispatchWorkflowException("Crew telemetry is stale. Refresh recommendations before dispatching.");
        }

        var etaMinutes = chosenRecommendation?.RecommendedRouteEtaMinutes
                         ?? autoRecommendation?.RecommendedRouteEtaMinutes
                         ?? ticket.EtaMinutes
                         ?? 60;

        var context = BuildDeliveryContext(ticket, crew, latestSnapshot, etaMinutes, requiresOverride, request.OverrideReason);
        var receipt = await _crewDeliveryService
            .QueueAssignmentAsync(context, cancellationToken)
            .ConfigureAwait(false);

        var updatedRecommendations = BuildUpdatedRecommendations(sortedRecommendations, crew.Id, requiresOverride, request.OverrideReason);
        var assignmentEvent = BuildAssignmentEvent(
            ticket.Id,
            crew,
            receipt,
            requiresOverride,
            request.OverrideReason,
            timestamp,
            chosenRecommendation ?? autoRecommendation);
        var updatedTicket = CloneTicketWithAssignment(ticket, crew.Id, etaMinutes, timestamp, assignmentEvent, updatedRecommendations);
        var updatedCrew = CloneCrewWithAssignment(crew, timestamp);

        await _ticketRepository.UpdateAsync(updatedTicket, cancellationToken).ConfigureAwait(false);
        await _ticketRepository.AddEventsAsync(new[] { assignmentEvent }, cancellationToken).ConfigureAwait(false);
        await _dispatchRepository.UpdateRecommendationsAsync(updatedRecommendations).ConfigureAwait(false);
        await _dispatchRepository.UpdateCrewAsync(updatedCrew).ConfigureAwait(false);

        await _ticketRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await _dispatchRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Dispatcher {Dispatcher} assigned crew {CrewId} to ticket {TicketId} with delivery tracking {TrackingId}.",
            context.RequestedBy,
            crew.Id,
            ticket.Id,
            receipt.TrackingId);

        return receipt;
    }

    public async Task<CrewStatusUpdateResponse> ProcessCrewStatusAsync(Guid crewId, CrewStatusUpdateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (crewId == Guid.Empty)
        {
            throw new ArgumentException("Crew identifier is required.", nameof(crewId));
        }

        var ticket = await _ticketRepository
            .GetByAssignedCrewIdAsync(crewId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"No active ticket assignment found for crew {crewId}.");

        CrewStatusUpdateResponse response;
        try
        {
            response = await _crewDeliveryService
                .RecordStatusAsync(crewId, ticket.Id, request, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "No delivery record found for crew {CrewId} and ticket {TicketId}. Accepting status anyway.", crewId, ticket.Id);
            response = new CrewStatusUpdateResponse(ticket.Id, _timeProvider.GetUtcNow());
        }

        await _ticketWriteService
            .ApplyCrewStatusAsync(ticket.Id, request, cancellationToken)
            .ConfigureAwait(false);

        return response;
    }

    private static void EnsureIdentifiers(DispatchAssignmentRequest request)
    {
        if (request.TicketId == Guid.Empty)
        {
            throw new ArgumentException("TicketId is required.", nameof(request));
        }

        if (request.CrewId == Guid.Empty)
        {
            throw new ArgumentException("CrewId is required.", nameof(request));
        }
    }

    private AssignmentDeliveryContext BuildDeliveryContext(
        Ticket ticket,
        Crew crew,
        CrewLocationSnapshot? snapshot,
        int etaMinutes,
        bool requiresOverride,
        string? overrideReason)
    {
        var assets = ticket.AffectedAssets?.ToArray() ?? Array.Empty<string>();
        var locationDto = snapshot is null
            ? null
            : new CrewLocationSnapshotDto(snapshot.Latitude, snapshot.Longitude, snapshot.CapturedAt, snapshot.SpeedMph);

        return new AssignmentDeliveryContext(
            ticket.Id,
            crew.Id,
            ticket.Title,
            ticket.Priority,
            crew.DisplayName,
            etaMinutes,
            Array.AsReadOnly(assets),
            locationDto,
            requiresOverride,
            overrideReason,
            ResolveActor());
    }

    private static IReadOnlyCollection<DispatchRecommendation> BuildUpdatedRecommendations(
        IReadOnlyCollection<DispatchRecommendation> existing,
        Guid selectedCrewId,
        bool requiresOverride,
        string? overrideReason)
    {
        var updated = new List<DispatchRecommendation>(existing.Count);

        foreach (var recommendation in existing)
        {
            var isSelected = recommendation.CrewId == selectedCrewId;
            updated.Add(new DispatchRecommendation
            {
                Id = recommendation.Id,
                TicketId = recommendation.TicketId,
                CrewId = recommendation.CrewId,
                CompositeScore = recommendation.CompositeScore,
                ScoreComponents = recommendation.ScoreComponents is null
                    ? new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string, double>(recommendation.ScoreComponents, StringComparer.OrdinalIgnoreCase),
                RecommendedRouteEtaMinutes = recommendation.RecommendedRouteEtaMinutes,
                IsAutoSelected = recommendation.IsAutoSelected,
                IsOverride = isSelected && requiresOverride,
                OverrideReason = isSelected ? overrideReason : recommendation.OverrideReason,
                CreatedAt = recommendation.CreatedAt,
                ExpiresAt = recommendation.ExpiresAt
            });
        }

        return updated;
    }

    private Ticket CloneTicketWithAssignment(
        Ticket ticket,
        Guid crewId,
        int etaMinutes,
        DateTimeOffset timestamp,
        AssignmentEvent assignmentEvent,
        IReadOnlyCollection<DispatchRecommendation> updatedRecommendations)
    {
        return new Ticket
        {
            Id = ticket.Id,
            OutageReferenceId = ticket.OutageReferenceId,
            Title = ticket.Title,
            Description = ticket.Description,
            Priority = ticket.Priority,
            Status = ticket.Status,
            AffectedAssets = ticket.AffectedAssets?.ToList() ?? new List<string>(),
            CustomerImpact = ticket.CustomerImpact,
            EtaMinutes = etaMinutes,
            AssignedCrewId = crewId,
            AutomationSource = ticket.AutomationSource,
            AuditVersion = ticket.AuditVersion + 1,
            OpenedAt = ticket.OpenedAt,
            UpdatedAt = timestamp,
            ClosedAt = ticket.ClosedAt,
            Events = ticket.Events?.Concat(new[] { assignmentEvent }).ToList() ?? new List<AssignmentEvent> { assignmentEvent },
            Recommendations = updatedRecommendations.ToList()
        };
    }

    private static Crew CloneCrewWithAssignment(Crew crew, DateTimeOffset timestamp)
    {
        return new Crew
        {
            Id = crew.Id,
            DisplayName = crew.DisplayName,
            Region = crew.Region,
            Skills = crew.Skills?.ToList() ?? new List<CrewSkill>(),
            CurrentTicketCount = Math.Max(crew.CurrentTicketCount, 0) + 1,
            Status = crew.Status,
            LastStatusUpdate = timestamp,
            PreferredShiftEnd = crew.PreferredShiftEnd,
            DeviceEndpoint = crew.DeviceEndpoint,
            LocationHistory = crew.LocationHistory?.ToList() ?? new List<CrewLocationSnapshot>(),
            Recommendations = crew.Recommendations?.ToList() ?? new List<DispatchRecommendation>(),
            Events = crew.Events?.ToList() ?? new List<AssignmentEvent>()
        };
    }

    private AssignmentEvent BuildAssignmentEvent(
        Guid ticketId,
        Crew crew,
        AssignmentReceiptDto receipt,
        bool requiresOverride,
        string? overrideReason,
        DateTimeOffset timestamp,
        DispatchRecommendation? recommendation)
    {
        var details = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["crewId"] = crew.Id.ToString(),
            ["crewName"] = crew.DisplayName,
            ["deliveryStatus"] = receipt.DeliveryStatus,
            ["trackingId"] = receipt.TrackingId,
            ["requiresOverride"] = requiresOverride.ToString()
        };

        if (!string.IsNullOrWhiteSpace(overrideReason))
        {
            details["overrideReason"] = overrideReason;
        }

        if (recommendation is not null)
        {
            details["score.composite"] = recommendation.CompositeScore.ToString("P0", CultureInfo.InvariantCulture);
            details["score.distance"] = FormatScoreComponent(recommendation, "distance");
            details["score.skill"] = FormatScoreComponent(recommendation, "skill");
            details["score.workload"] = FormatScoreComponent(recommendation, "workload");
            details["score.etaMinutes"] = recommendation.RecommendedRouteEtaMinutes.ToString(CultureInfo.InvariantCulture);
        }

        return new AssignmentEvent
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            CrewId = crew.Id,
            EventType = AssignmentEventType.AssignmentPublished,
            Details = details,
            Actor = ResolveActor(),
            OccurredAt = timestamp
        };
    }

    private static string FormatScoreComponent(DispatchRecommendation recommendation, string componentKey)
    {
        if (recommendation.ScoreComponents is not null && recommendation.ScoreComponents.TryGetValue(componentKey, out var value))
        {
            return value.ToString("P0", CultureInfo.InvariantCulture);
        }

        return "n/a";
    }

    private string ResolveActor()
    {
        var identity = _userContext.Current ?? UserIdentity.Anonymous;
        return string.IsNullOrWhiteSpace(identity.DisplayName) ? "system" : identity.DisplayName;
    }
}
