namespace GridPulse.Application.Abstractions;

public interface ITicketRepository
{
    Task<Ticket?> GetByIdAsync(Guid ticketId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Ticket>> GetAsync(TicketQueryOptions options, CancellationToken cancellationToken = default);
    Task AddAsync(Ticket ticket, CancellationToken cancellationToken = default);
    Task UpdateAsync(Ticket ticket, CancellationToken cancellationToken = default);
    Task AddEventsAsync(IEnumerable<AssignmentEvent> events, CancellationToken cancellationToken = default);
    Task AddRecommendationsAsync(IEnumerable<DispatchRecommendation> recommendations, CancellationToken cancellationToken = default);
    Task<bool> ExistsByOutageReferenceAsync(string outageReferenceId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
