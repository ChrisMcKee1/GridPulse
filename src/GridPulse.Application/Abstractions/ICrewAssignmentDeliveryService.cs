using GridPulse.Application.Models;

namespace GridPulse.Application.Abstractions;

/// <summary>
/// Handles queuing dispatcher assignments to crew devices along with capturing acknowledgement
/// metadata so other services stay persistence-agnostic.
/// </summary>
public interface ICrewAssignmentDeliveryService
{
    Task<AssignmentReceiptDto> QueueAssignmentAsync(
        AssignmentDeliveryContext context,
        CancellationToken cancellationToken = default);

    Task<CrewStatusUpdateResponse> RecordStatusAsync(
        Guid crewId,
        Guid ticketId,
        CrewStatusUpdateRequest request,
        CancellationToken cancellationToken = default);
}