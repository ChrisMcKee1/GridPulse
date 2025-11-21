using GridPulse.Domain.Entities;

namespace GridPulse.Application.Abstractions;

/// <summary>
/// Persists outbound assignment payloads so future crew clients can poll for
/// pending work and delivery telemetry remains storage-agnostic.
/// </summary>
public interface IAssignmentDeliveryRepository
{
    Task<AssignmentDelivery?> GetLatestAsync(Guid ticketId, Guid crewId, CancellationToken cancellationToken = default);
    Task AddAsync(AssignmentDelivery delivery, CancellationToken cancellationToken = default);
    Task UpdateAsync(AssignmentDelivery delivery, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}