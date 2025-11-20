using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using GridPulse.Application.Models;
using GridPulse.Domain.Entities;
using GridPulse.Domain.Enums;
using GridPulse.Infrastructure.Persistence;
using GridPulse.Tests.Unit.TestInfrastructure;
using Microsoft.AspNetCore.Mvc;

namespace GridPulse.Tests.Unit.WebApi;

[Collection(TestCollections.Api)]
public sealed class TicketEndpointTests
{
    private readonly GridPulseApiFactory _factory;
    private readonly HttpClient _client;

    public TicketEndpointTests(GridPulseApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateTicket_ReturnsCreatedTicket()
    {
        var request = new TicketCreateRequest
        {
            Title = "Pole fire on 5th Ave",
            OutageReferenceId = "OUT-5001",
            Priority = TicketPriority.High,
            CustomerImpact = 42,
            AffectedAssets = new List<string> { "TX-009" }
        };

        var response = await _client.PostAsJsonAsync("/api/tickets", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<TicketDto>();
        body.Should().NotBeNull();
        body!.Title.Should().Be(request.Title);
        body.Status.Should().Be(TicketStatus.Open);
        body.AffectedAssets.Should().Contain("TX-009");
    }

    [Fact]
    public async Task GetTickets_FiltersByStatus()
    {
        var now = DateTimeOffset.UtcNow;
        await SeedTicketAsync(new Ticket
        {
            Id = Guid.NewGuid(),
            Title = "Damaged transformer",
            Description = "",
            OutageReferenceId = "OUT-9000",
            Priority = TicketPriority.Medium,
            Status = TicketStatus.Open,
            CustomerImpact = 15,
            OpenedAt = now.AddMinutes(-10),
            UpdatedAt = now.AddMinutes(-5),
            Events = new List<AssignmentEvent>(),
            Recommendations = new List<DispatchRecommendation>(),
            AffectedAssets = new List<string> { "TX-007" }
        });

        await SeedTicketAsync(new Ticket
        {
            Id = Guid.NewGuid(),
            Title = "Cleared outage",
            Description = "",
            OutageReferenceId = "OUT-9001",
            Priority = TicketPriority.Low,
            Status = TicketStatus.Resolved,
            CustomerImpact = 2,
            OpenedAt = now.AddHours(-2),
            UpdatedAt = now.AddHours(-1),
            ClosedAt = now.AddMinutes(-30),
            Events = new List<AssignmentEvent>(),
            Recommendations = new List<DispatchRecommendation>(),
            AffectedAssets = new List<string> { "TX-008" }
        });

        var response = await _client.GetAsync("/api/tickets?statuses=Open");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<TicketDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
        result.Items.Single().Status.Should().Be(TicketStatus.Open);
    }

    [Fact]
    public async Task UpdateTicketStatus_InvalidTransitionReturnsProblemDetails()
    {
        var ticketId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        await SeedTicketAsync(new Ticket
        {
            Id = ticketId,
            Title = "Crew requested",
            Description = "",
            OutageReferenceId = "OUT-9100",
            Priority = TicketPriority.Critical,
            Status = TicketStatus.Open,
            CustomerImpact = 120,
            OpenedAt = now.AddMinutes(-20),
            UpdatedAt = now.AddMinutes(-10),
            Events = new List<AssignmentEvent>(),
            Recommendations = new List<DispatchRecommendation>(),
            AffectedAssets = new List<string> { "TX-010" }
        });

        var request = new TicketStatusUpdateRequest
        {
            Status = TicketStatus.Resolved
        };

        var response = await _client.PatchAsJsonAsync($"/api/tickets/{ticketId}/status", request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be((int)HttpStatusCode.Conflict);
        problem.Detail.Should().Contain("Cannot transition ticket");
    }

    private Task SeedTicketAsync(Ticket ticket)
    {
        return _factory.ExecuteDbContextAsync(async dbContext =>
        {
            dbContext.Tickets.Add(ticket);
            await dbContext.SaveChangesAsync();
        });
    }
}