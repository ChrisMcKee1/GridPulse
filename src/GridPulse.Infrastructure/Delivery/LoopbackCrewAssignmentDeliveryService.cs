using GridPulse.Application.Abstractions;
using GridPulse.Application.Models;
using Microsoft.Extensions.Logging;

namespace GridPulse.Infrastructure.Delivery;

internal sealed class LoopbackCrewAssignmentDeliveryService : ICrewAssignmentDeliveryService
{
    private readonly ILogger<LoopbackCrewAssignmentDeliveryService> _logger;
    private readonly TimeProvider _timeProvider;

    public LoopbackCrewAssignmentDeliveryService(ILogger<LoopbackCrewAssignmentDeliveryService> logger, TimeProvider? timeProvider = null)
    {
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public Task<AssignmentReceiptDto> QueueAssignmentAsync(AssignmentDeliveryContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var trackingId = $"loopback-{Guid.NewGuid():N}";
        _logger.LogInformation(
            "Queued assignment {TrackingId} for ticket {TicketId} targeting crew {CrewId}.",
            trackingId,
            context.TicketId,
            context.CrewId);

        return Task.FromResult(new AssignmentReceiptDto(context.TicketId, context.CrewId, "queued", trackingId));
    }

    public Task<CrewStatusUpdateResponse> RecordStatusAsync(
        Guid crewId,
        Guid ticketId,
        CrewStatusUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation(
            "Crew {CrewId} reported status {Status} for ticket {TicketId}.",
            crewId,
            request.Status,
            ticketId);

        return Task.FromResult(new CrewStatusUpdateResponse(ticketId, _timeProvider.GetUtcNow()));
    }
}
