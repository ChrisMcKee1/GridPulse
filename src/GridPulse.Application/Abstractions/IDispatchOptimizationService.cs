namespace GridPulse.Application.Abstractions;

public interface IDispatchOptimizationService
{
    Task<DispatchRecommendationsEnvelope> GetRecommendationsAsync(Guid ticketId, CancellationToken cancellationToken = default);
    Task<AssignmentReceiptDto> PublishAssignmentAsync(DispatchAssignmentRequest request, CancellationToken cancellationToken = default);
}
