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
}
