using System.Net.Http.Json;
using GridPulse.Web.Services.Models;

namespace GridPulse.Web.Services;

public sealed class GridPulseApiClient(HttpClient httpClient)
{
    internal HttpClient HttpClient => httpClient;

    public async Task<IReadOnlyList<OutageSummaryResponse>> GetRecentOutagesAsync(CancellationToken cancellationToken = default)
    {
        var items = await httpClient.GetFromJsonAsync<IReadOnlyList<OutageSummaryResponse>>("api/outages", cancellationToken);
        return items ?? Array.Empty<OutageSummaryResponse>();
    }

    public async Task<TicketListResponse> GetAllTicketsAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetFromJsonAsync<TicketListResponse>("api/tickets", cancellationToken);
        return response ?? new TicketListResponse(Array.Empty<TicketDto>(), 0);
    }

    public record TicketListResponse(IReadOnlyList<TicketDto> Items, int TotalCount);

    public record TicketDto(
        Guid Id,
        string Title,
        string OutageReferenceId,
        string Priority,
        string Status,
        int CustomerImpact,
        int? EtaMinutes,
        Guid? AssignedCrewId,
        string? AssignedCrewName,
        DateTimeOffset OpenedAt,
        DateTimeOffset UpdatedAt,
        DateTimeOffset? ClosedAt
    );
}
