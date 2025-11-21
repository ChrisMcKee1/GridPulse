using System.Globalization;
using System.Linq;
using GridPulse.Application.Abstractions;
using GridPulse.Application.Exceptions;
using GridPulse.Application.Models;
using GridPulse.Domain.Entities;
using GridPulse.Domain.Enums;

namespace GridPulse.Application.Services;

internal sealed class TicketWriteService : ITicketWriteService
{
    private static readonly IReadOnlyDictionary<TicketStatus, TicketStatus[]> AllowedTransitions = new Dictionary<TicketStatus, TicketStatus[]>
    {
        [TicketStatus.Open] = new[] { TicketStatus.InProgress, TicketStatus.Cancelled },
        [TicketStatus.InProgress] = new[] { TicketStatus.Resolved, TicketStatus.Cancelled },
        [TicketStatus.Resolved] = new[] { TicketStatus.Closed, TicketStatus.Cancelled },
        [TicketStatus.Closed] = Array.Empty<TicketStatus>(),
        [TicketStatus.Cancelled] = Array.Empty<TicketStatus>()
    };

    private const int DuplicateWindowMinutes = 5;

    private readonly ITicketRepository _ticketRepository;
    private readonly IUserContext _userContext;
    private readonly TimeProvider _timeProvider;

    public TicketWriteService(
        ITicketRepository ticketRepository,
        IUserContext userContext,
        TimeProvider? timeProvider = null)
    {
        _ticketRepository = ticketRepository;
        _userContext = userContext;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<Ticket> UpdateStatusAsync(Guid ticketId, TicketStatusUpdateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var ticket = await _ticketRepository.GetByIdAsync(ticketId, cancellationToken).ConfigureAwait(false)
                     ?? throw new InvalidOperationException($"Ticket {ticketId} was not found.");

        var targetStatus = request.Status;
        EnsureTransitionAllowed(ticket.Status, targetStatus);

        if (targetStatus == TicketStatus.Cancelled && string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ArgumentException("A cancellation reason is required when cancelling a ticket.", nameof(request));
        }

        var now = _timeProvider.GetUtcNow();
        var statusEvent = BuildStatusChangeEvent(ticket, targetStatus, request.Reason, now);
        var updatedTicket = CloneTicket(ticket, targetStatus, now);
        updatedTicket.Events.Add(statusEvent);

        await _ticketRepository.UpdateAsync(updatedTicket, cancellationToken).ConfigureAwait(false);

        await _ticketRepository.AddEventsAsync(new[] { statusEvent }, cancellationToken).ConfigureAwait(false);

        await _ticketRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return updatedTicket;
    }

    public async Task<IReadOnlyCollection<Ticket>> DetectPotentialDuplicatesAsync(TicketCreateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var now = _timeProvider.GetUtcNow();

        var candidates = await _ticketRepository.GetAsync(new TicketQueryOptions
        {
            Statuses = new[] { TicketStatus.Open, TicketStatus.InProgress }
        }, cancellationToken).ConfigureAwait(false);

        var duplicates = candidates
            .Where(ticket => IsDuplicateCandidate(ticket, request, now))
            .ToArray();

        if (duplicates.Length == 0)
        {
            return duplicates;
        }

        var duplicateEvents = duplicates.Select(ticket => BuildDuplicateFlagEvent(ticket, request, now)).ToArray();
        await _ticketRepository.AddEventsAsync(duplicateEvents, cancellationToken).ConfigureAwait(false);
        await _ticketRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return duplicates;
    }

    public Task<Ticket> ApplyCrewStatusAsync(Guid ticketId, CrewStatusUpdateRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Crew status workflow will be wired during US3 implementation.");
    }

    private static void EnsureTransitionAllowed(TicketStatus current, TicketStatus next)
    {
        if (!AllowedTransitions.TryGetValue(current, out var allowed) || !allowed.Contains(next))
        {
            throw new TicketWorkflowException($"Cannot transition ticket from {current} to {next}.");
        }
    }

    private Ticket CloneTicket(Ticket ticket, TicketStatus newStatus, DateTimeOffset updatedAt)
    {
        return new Ticket
        {
            Id = ticket.Id,
            OutageReferenceId = ticket.OutageReferenceId,
            Title = ticket.Title,
            Description = ticket.Description,
            Priority = ticket.Priority,
            Status = newStatus,
            AffectedAssets = ticket.AffectedAssets.ToList(),
            CustomerImpact = ticket.CustomerImpact,
            EtaMinutes = ticket.EtaMinutes,
            AssignedCrewId = ticket.AssignedCrewId,
            AutomationSource = ticket.AutomationSource,
            AuditVersion = ticket.AuditVersion + 1,
            OpenedAt = ticket.OpenedAt,
            UpdatedAt = updatedAt,
            ClosedAt = ShouldSetClosedAt(newStatus) ? updatedAt : ticket.ClosedAt,
            Events = ticket.Events.ToList(),
            Recommendations = ticket.Recommendations.ToList()
        };
    }

    private static bool ShouldSetClosedAt(TicketStatus status)
    {
        return status is TicketStatus.Closed or TicketStatus.Cancelled;
    }

    private AssignmentEvent BuildStatusChangeEvent(Ticket ticket, TicketStatus targetStatus, string? reason, DateTimeOffset timestamp)
    {
        var eventType = targetStatus switch
        {
            TicketStatus.Closed => AssignmentEventType.OperatorClosed,
            TicketStatus.Cancelled => AssignmentEventType.TicketCancelled,
            _ => AssignmentEventType.TicketEdited
        };

        var details = new Dictionary<string, string>
        {
            ["fromStatus"] = ticket.Status.ToString(),
            ["toStatus"] = targetStatus.ToString()
        };

        if (!string.IsNullOrWhiteSpace(reason))
        {
            details["reason"] = reason;
        }

        return new AssignmentEvent
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            EventType = eventType,
            Details = details,
            Actor = ResolveActor(),
            OccurredAt = timestamp
        };
    }

    private AssignmentEvent BuildDuplicateFlagEvent(Ticket ticket, TicketCreateRequest request, DateTimeOffset timestamp)
    {
        return new AssignmentEvent
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            EventType = AssignmentEventType.TicketDuplicateFlag,
            Actor = ResolveActor(),
            OccurredAt = timestamp,
            Details = new Dictionary<string, string>
            {
                ["incomingOutageId"] = request.OutageReferenceId,
                ["incomingPriority"] = request.Priority.ToString(),
                ["incomingTitle"] = request.Title,
                ["detectedAt"] = timestamp.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)
            }
        };
    }

    private bool IsDuplicateCandidate(Ticket ticket, TicketCreateRequest request, DateTimeOffset now)
    {
        if (now - ticket.OpenedAt > TimeSpan.FromMinutes(DuplicateWindowMinutes))
        {
            return false;
        }

        var outageMatches = !string.IsNullOrWhiteSpace(request.OutageReferenceId) &&
                            string.Equals(ticket.OutageReferenceId, request.OutageReferenceId, StringComparison.OrdinalIgnoreCase);

        var assetMatches = ticket.AffectedAssets
            .Intersect(request.AffectedAssets, StringComparer.OrdinalIgnoreCase)
            .Any();

        return outageMatches || assetMatches;
    }

    private string ResolveActor()
    {
        var identity = _userContext.Current ?? UserIdentity.Anonymous;
        return string.IsNullOrWhiteSpace(identity.DisplayName) ? "system" : identity.DisplayName;
    }
}