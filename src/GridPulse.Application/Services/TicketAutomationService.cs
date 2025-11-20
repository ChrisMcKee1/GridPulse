using System;
using System.Collections.Generic;
using System.Linq;
using GridPulse.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace GridPulse.Application.Services;

internal sealed class TicketAutomationService : ITicketAutomationService
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ITicketWriteService _ticketWriteService;
    private readonly IUserContext _userContext;
    private readonly ILogger<TicketAutomationService> _logger;
    private readonly TimeProvider _timeProvider;

    public TicketAutomationService(
        ITicketRepository ticketRepository,
        ITicketWriteService ticketWriteService,
        IUserContext userContext,
        ILogger<TicketAutomationService> logger,
        TimeProvider? timeProvider = null)
    {
        _ticketRepository = ticketRepository;
        _ticketWriteService = ticketWriteService;
        _userContext = userContext;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<TicketDto?> CreateOrUpdateAutomatedTicketAsync(TicketSeedData payload, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);

        var createRequest = new TicketCreateRequest
        {
            Title = payload.Title,
            Description = payload.Description,
            OutageReferenceId = payload.OutageReferenceId,
            Priority = payload.Priority,
            AffectedAssets = payload.AffectedAssets?.ToList() ?? new List<string>(),
            CustomerImpact = payload.CustomerImpact
        };

        ValidateRequest(createRequest);

        if (await TicketAlreadyExistsAsync(createRequest, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogDebug(
                "Skipping automated ticket for outage {OutageReferenceId} because one already exists.",
                createRequest.OutageReferenceId);
            return null;
        }

        var duplicates = await _ticketWriteService
            .DetectPotentialDuplicatesAsync(createRequest, cancellationToken)
            .ConfigureAwait(false);

        if (duplicates.Count > 0)
        {
            _logger.LogInformation(
                "Suppressed automated ticket creation for outage {OutageReferenceId} due to {DuplicateCount} potential duplicates.",
                payload.OutageReferenceId,
                duplicates.Count);
            return null;
        }

        return await CreateTicketInternalAsync(
                createRequest,
                source: "automation",
                openedAt: payload.OpenedAt,
                actorOverride: "automation",
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<TicketDto> CreateManualTicketAsync(TicketCreateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateRequest(request);

        if (await TicketAlreadyExistsAsync(request, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogInformation(
                "Operator attempted to create ticket for outage {OutageReferenceId}, but it already exists.",
                request.OutageReferenceId);
            throw new InvalidOperationException($"A ticket already exists for outage {request.OutageReferenceId}.");
        }

        await _ticketWriteService.DetectPotentialDuplicatesAsync(request, cancellationToken).ConfigureAwait(false);

        var now = _timeProvider.GetUtcNow();
        var actor = ResolveActor();

        return await CreateTicketInternalAsync(
                request,
                source: "operator",
                openedAt: now,
                actorOverride: actor,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<TicketDto> CreateTicketInternalAsync(
        TicketCreateRequest request,
        string source,
        DateTimeOffset openedAt,
        string actorOverride,
        CancellationToken cancellationToken)
    {
        var normalizedAssets = NormalizeAssets(request.AffectedAssets);

        var outageReferenceId = NormalizeOutageReference(request.OutageReferenceId);

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            OutageReferenceId = outageReferenceId,
            Priority = request.Priority,
            Status = TicketStatus.Open,
            AffectedAssets = normalizedAssets,
            CustomerImpact = Math.Max(0, request.CustomerImpact),
            AutomationSource = source,
            AuditVersion = 1,
            OpenedAt = openedAt,
            UpdatedAt = openedAt,
            ClosedAt = null,
            Events = new List<AssignmentEvent>(),
            Recommendations = new List<DispatchRecommendation>()
        };

        var createdEvent = BuildCreatedEvent(ticket.Id, actorOverride, source, openedAt, ticket.Priority);
        ticket.Events.Add(createdEvent);

        await _ticketRepository.AddAsync(ticket, cancellationToken).ConfigureAwait(false);
        await _ticketRepository.AddEventsAsync(new[] { createdEvent }, cancellationToken).ConfigureAwait(false);
        await _ticketRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return TicketProjection.ToDto(ticket);
    }

    private static void ValidateRequest(TicketCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ArgumentException("Ticket title is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.OutageReferenceId))
        {
            throw new ArgumentException("Outage reference identifier is required.", nameof(request));
        }
    }

    private static string NormalizeOutageReference(string outageReference)
    {
        return outageReference.Trim();
    }

    private static ICollection<string> NormalizeAssets(IEnumerable<string>? assets)
    {
        if (assets is null)
        {
            return new List<string>();
        }

        var normalized = assets
            .Where(asset => !string.IsNullOrWhiteSpace(asset))
            .Select(asset => asset.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return normalized.Count == 0 ? new List<string>() : normalized;
    }

    private static AssignmentEvent BuildCreatedEvent(
        Guid ticketId,
        string actor,
        string source,
        DateTimeOffset occurredAt,
        TicketPriority priority)
    {
        var eventActor = string.IsNullOrWhiteSpace(actor) ? "system" : actor;

        return new AssignmentEvent
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            EventType = AssignmentEventType.TicketCreated,
            Actor = eventActor,
            OccurredAt = occurredAt,
            Details = new Dictionary<string, string>
            {
                ["source"] = source,
                ["priority"] = priority.ToString()
            }
        };
    }

    private string ResolveActor()
    {
        var identity = _userContext.Current ?? UserIdentity.Anonymous;
        return string.IsNullOrWhiteSpace(identity.DisplayName) ? "operator" : identity.DisplayName;
    }

    private async Task<bool> TicketAlreadyExistsAsync(TicketCreateRequest request, CancellationToken cancellationToken)
    {
        var outageReference = NormalizeOutageReference(request.OutageReferenceId);
        return await _ticketRepository.ExistsByOutageReferenceAsync(outageReference, cancellationToken).ConfigureAwait(false);
    }
}
