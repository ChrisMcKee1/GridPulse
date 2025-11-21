namespace GridPulse.Application.Abstractions;

public interface IDispatchOptimizationService
{
    Task<DispatchRecommendationsEnvelope> GetRecommendationsAsync(Guid ticketId, CancellationToken cancellationToken = default);
}
