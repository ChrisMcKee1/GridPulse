using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using GridPulse.Application.Models;
using GridPulse.Domain.Enums;

namespace GridPulse.Web.Services;

public static class GridPulseApiClientExtensions
{
    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();

    public static async Task<TicketDto> CreateTicketAsync(
        this GridPulseApiClient client,
        TicketCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(request);

        var response = await client.HttpClient
            .PostAsJsonAsync("api/tickets", request, SerializerOptions, cancellationToken)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
        return await DeserializeAsync<TicketDto>(response.Content, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<PagedResult<TicketDto>> GetTicketsAsync(
        this GridPulseApiClient client,
        TicketFilter? filter = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        var normalizedFilter = filter ?? new TicketFilter();
        var query = BuildTicketsQueryString(normalizedFilter);
        var endpoint = string.IsNullOrEmpty(query) ? "api/tickets" : $"api/tickets{query}";

        var result = await client.HttpClient
            .GetFromJsonAsync<PagedResult<TicketDto>>(endpoint, SerializerOptions, cancellationToken)
            .ConfigureAwait(false);

        return result ?? new PagedResult<TicketDto>(Array.Empty<TicketDto>(), 0);
    }

    public static async Task<TicketDto> UpdateTicketStatusAsync(
        this GridPulseApiClient client,
        Guid ticketId,
        TicketStatusUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(request);

        if (ticketId == Guid.Empty)
        {
            throw new ArgumentException("Ticket identifier must be provided.", nameof(ticketId));
        }

        var response = await client.HttpClient
            .PatchAsJsonAsync($"api/tickets/{ticketId:D}/status", request, SerializerOptions, cancellationToken)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
        return await DeserializeAsync<TicketDto>(response.Content, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<DispatchRecommendationsEnvelope> GetRecommendationsAsync(
        this GridPulseApiClient client,
        Guid ticketId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);

        if (ticketId == Guid.Empty)
        {
            throw new ArgumentException("Ticket identifier must be provided.", nameof(ticketId));
        }

        var endpoint = $"api/dispatch/recommendations?ticketId={ticketId:D}";
        var envelope = await client.HttpClient
            .GetFromJsonAsync<DispatchRecommendationsEnvelope>(endpoint, SerializerOptions, cancellationToken)
            .ConfigureAwait(false);

        return envelope ?? throw new InvalidOperationException("Dispatch recommendations could not be retrieved from the API.");
    }

    public static async Task<AssignmentReceiptDto> PublishAssignmentAsync(
        this GridPulseApiClient client,
        DispatchAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(request);

        if (request.TicketId == Guid.Empty)
        {
            throw new ArgumentException("TicketId must be provided.", nameof(request));
        }

        if (request.CrewId == Guid.Empty)
        {
            throw new ArgumentException("CrewId must be provided.", nameof(request));
        }

        var response = await client.HttpClient
            .PostAsJsonAsync("api/dispatch/assignments", request, SerializerOptions, cancellationToken)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
        return await DeserializeAsync<AssignmentReceiptDto>(response.Content, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<CrewStatusUpdateResponse> PostCrewStatusAsync(
        this GridPulseApiClient client,
        Guid crewId,
        CrewStatusUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(request);

        if (crewId == Guid.Empty)
        {
            throw new ArgumentException("Crew identifier must be provided.", nameof(crewId));
        }

        var response = await client.HttpClient
            .PostAsJsonAsync($"api/crews/{crewId:D}/status", request, SerializerOptions, cancellationToken)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
        return await DeserializeAsync<CrewStatusUpdateResponse>(response.Content, cancellationToken).ConfigureAwait(false);
    }

    private static string BuildTicketsQueryString(TicketFilter filter)
    {
        var parameters = new List<string>();

        if (filter.Statuses is { Count: > 0 })
        {
            var joinedStatuses = string.Join(',', filter.Statuses.Select(status => status.ToString()));
            if (!string.IsNullOrEmpty(joinedStatuses))
            {
                parameters.Add($"statuses={Uri.EscapeDataString(joinedStatuses)}");
            }
        }

        if (filter.MinPriority is TicketPriority minPriority)
        {
            parameters.Add($"minPriority={Uri.EscapeDataString(minPriority.ToString())}");
        }

        if (filter.IncludeRecommendations)
        {
            parameters.Add("includeRecommendations=true");
        }

        if (filter.IncludeTimeline)
        {
            parameters.Add("includeTimeline=true");
        }

        if (parameters.Count == 0)
        {
            return string.Empty;
        }

        return $"?{string.Join('&', parameters)}";
    }

    private static async Task<T> DeserializeAsync<T>(HttpContent content, CancellationToken cancellationToken)
    {
        var result = await content.ReadFromJsonAsync<T>(SerializerOptions, cancellationToken).ConfigureAwait(false);
        if (result is null)
        {
            throw new InvalidOperationException($"Unable to deserialize API response to type '{typeof(T).Name}'.");
        }

        return result;
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
