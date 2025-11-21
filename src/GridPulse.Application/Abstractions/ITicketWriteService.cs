using GridPulse.Application.Models;
using GridPulse.Domain.Entities;

namespace GridPulse.Application.Abstractions;

/// <summary>
/// Encapsulates ticket write operations including workflow transitions and duplicate detection so
/// both automation and operator flows share a single set of guardrails.
/// </summary>
public interface ITicketWriteService
{
    Task<Ticket> UpdateStatusAsync(Guid ticketId, TicketStatusUpdateRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Ticket>> DetectPotentialDuplicatesAsync(TicketCreateRequest request, CancellationToken cancellationToken = default);

    Task<Ticket> ApplyCrewStatusAsync(Guid ticketId, CrewStatusUpdateRequest request, CancellationToken cancellationToken = default);
}