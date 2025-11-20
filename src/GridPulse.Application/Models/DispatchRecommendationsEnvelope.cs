namespace GridPulse.Application.Models;

public sealed record DispatchRecommendationsEnvelope(
    TicketSummaryDto Ticket,
    IReadOnlyCollection<DispatchRecommendationDto> Recommendations
);
