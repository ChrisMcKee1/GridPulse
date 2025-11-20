namespace GridPulse.Application.Abstractions;

public interface ITicketAutomationService
{
    Task<TicketDto?> CreateOrUpdateAutomatedTicketAsync(TicketSeedData payload, CancellationToken cancellationToken = default);
    Task<TicketDto> CreateManualTicketAsync(TicketCreateRequest request, CancellationToken cancellationToken = default);
}
