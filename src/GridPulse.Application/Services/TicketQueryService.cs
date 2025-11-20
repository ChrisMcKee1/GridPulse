using System;
using System.Linq;
using GridPulse.Application.Abstractions;

namespace GridPulse.Application.Services;

internal sealed class TicketQueryService : ITicketQueryService
{
    private readonly ITicketRepository _ticketRepository;

    public TicketQueryService(ITicketRepository ticketRepository)
    {
        _ticketRepository = ticketRepository;
    }

    public async Task<PagedResult<TicketDto>> GetTicketsAsync(TicketFilter filter, CancellationToken cancellationToken = default)
    {
        filter ??= new TicketFilter();

        var options = new TicketQueryOptions
        {
            Statuses = filter.Statuses,
            MinPriority = filter.MinPriority,
            IncludeRecommendations = filter.IncludeRecommendations,
            IncludeTimeline = filter.IncludeTimeline
        };

        var tickets = await _ticketRepository.GetAsync(options, cancellationToken).ConfigureAwait(false);
        var items = tickets.Select(TicketProjection.ToDto).ToArray();
        return new PagedResult<TicketDto>(items, items.Length);
    }

    public async Task<TicketDto?> GetByIdAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketId, cancellationToken).ConfigureAwait(false);
        return ticket is null ? null : TicketProjection.ToDto(ticket);
    }
}
