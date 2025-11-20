using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using GridPulse.Application.Models;
using GridPulse.Domain.Enums;
using GridPulse.Web.Services;
using Xunit;

namespace GridPulse.Tests.Unit.Web.Services;

public class GridPulseApiClientExtensionsTests
{
    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();

    [Fact]
    public async Task GetTicketsAsync_BuildsQueryFromFilter()
    {
        var handler = new StubHttpMessageHandler();
        var expectedTicket = CreateTicket();
        handler.ResponseFactory = _ => BuildJsonResponse(new PagedResult<TicketDto>(new[] { expectedTicket }, 1));
        var apiClient = CreateClient(handler);

        var filter = new TicketFilter
        {
            Statuses = new[] { TicketStatus.Open, TicketStatus.Resolved },
            MinPriority = TicketPriority.High,
            IncludeRecommendations = true,
            IncludeTimeline = true
        };

        var result = await apiClient.GetTicketsAsync(filter);

        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.Method.Should().Be(HttpMethod.Get);
        handler.LastRequest.RequestUri.Should().Be(new Uri("https://localhost/api/tickets?statuses=Open%2CResolved&minPriority=High&includeRecommendations=true&includeTimeline=true"));
        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle(ticket => ticket.Id == expectedTicket.Id);
    }

    [Fact]
    public async Task CreateTicketAsync_SendsPostRequest()
    {
        var handler = new StubHttpMessageHandler();
        var expectedTicket = CreateTicket();
        handler.ResponseFactory = _ => BuildJsonResponse(expectedTicket, HttpStatusCode.Created);
        var apiClient = CreateClient(handler);

        var request = new TicketCreateRequest
        {
            Title = "New ticket",
            OutageReferenceId = "OUT-100",
            Priority = TicketPriority.High,
            CustomerImpact = 12,
            AffectedAssets = new List<string> { "TX-01" }
        };

        var result = await apiClient.CreateTicketAsync(request);

        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest.RequestUri.Should().Be(new Uri("https://localhost/api/tickets"));
        var payload = await handler.LastRequest.Content!.ReadAsStringAsync();
        payload.Should().Contain("\"priority\":\"High\"");
        result.Id.Should().Be(expectedTicket.Id);
    }

    [Fact]
    public async Task UpdateTicketStatusAsync_SendsPatchRequest()
    {
        var handler = new StubHttpMessageHandler();
        var expectedTicket = CreateTicket();
        handler.ResponseFactory = _ => BuildJsonResponse(expectedTicket);
        var apiClient = CreateClient(handler);
        var ticketId = Guid.NewGuid();

        var result = await apiClient.UpdateTicketStatusAsync(ticketId, new TicketStatusUpdateRequest
        {
            Status = TicketStatus.InProgress,
            Reason = "Crew dispatched"
        });

        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.Method.Should().Be(HttpMethod.Patch);
        handler.LastRequest.RequestUri.Should().Be(new Uri($"https://localhost/api/tickets/{ticketId:D}/status"));
        var payload = await handler.LastRequest.Content!.ReadAsStringAsync();
        payload.Should().Contain("\"status\":\"InProgress\"");
        result.Id.Should().Be(expectedTicket.Id);
    }

    private static GridPulseApiClient CreateClient(StubHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost/")
        };

        return new GridPulseApiClient(httpClient);
    }

    private static HttpResponseMessage BuildJsonResponse<T>(T payload, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var json = JsonSerializer.Serialize(payload, SerializerOptions);
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private static TicketDto CreateTicket()
    {
        return new TicketDto(
            Guid.NewGuid(),
            "Pole fire",
            "OUT-42",
            TicketPriority.High,
            TicketStatus.Open,
            25,
            60,
            Guid.NewGuid(),
            "Crew Alpha",
            Array.Empty<string>(),
            DateTimeOffset.UtcNow.AddMinutes(-30),
            DateTimeOffset.UtcNow,
            null,
            Array.Empty<AssignmentEventDto>(),
            Array.Empty<DispatchRecommendationDto>());
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public Func<HttpRequestMessage, HttpResponseMessage>? ResponseFactory { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            var response = ResponseFactory?.Invoke(request) ?? new HttpResponseMessage(HttpStatusCode.OK);
            return Task.FromResult(response);
        }
    }
}
