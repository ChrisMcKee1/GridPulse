using System;
using GridPulse.Application.Models;

namespace GridPulse.Application.Abstractions;

public interface ITicketQueryService
{
    Task<PagedResult<TicketDto>> GetTicketsAsync(TicketFilter filter, CancellationToken cancellationToken = default);

    Task<TicketDto?> GetByIdAsync(Guid ticketId, CancellationToken cancellationToken = default);
}
