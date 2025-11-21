using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using GridPulse.Application.Models;
using GridPulse.Domain.Entities;
using GridPulse.Domain.Enums;
using GridPulse.Infrastructure.Persistence;
using GridPulse.Tests.Unit.TestInfrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GridPulse.Tests.Unit.WebApi;

[Collection(TestCollections.Api)]
public sealed class CrewEndpointTests
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();
    private readonly GridPulseApiFactory _factory;

    public CrewEndpointTests(GridPulseApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostCrewStatus_AcknowledgesAssignmentAndWritesTimeline()
    {
        var crew = BuildCrew();
        var ticket = BuildTicket(crew.Id);
        await SeedAsync(db =>
        {
            db.Crews.Add(crew);
            db.Tickets.Add(ticket);
            return db.SaveChangesAsync();
        });

        using var client = _factory.CreateClient();
        var request = new CrewStatusUpdateRequest
        {
            Status = CrewAssignmentStatus.Acknowledged,
            Note = "Received dispatch",
            Location = new CrewLocationSnapshotDto(33.1m, -96.8m, DateTimeOffset.UtcNow, 45)
        };

        var response = await client.PostAsJsonAsync($"/api/crews/{crew.Id:D}/status", request);

        var raw = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Crew endpoint response: {raw}");
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var payload = await response.Content.ReadFromJsonAsync<CrewStatusUpdateResponse>(JsonOptions);
        payload.Should().NotBeNull();
        payload!.TicketId.Should().Be(ticket.Id);

        await _factory.ExecuteDbContextAsync(async db =>
        {
            var persistedTicket = await db.Tickets.Include(t => t.Events).FirstAsync(t => t.Id == ticket.Id);
            persistedTicket.Status.Should().Be(TicketStatus.InProgress);
            persistedTicket.Events.Should().Contain(evt => evt.EventType == AssignmentEventType.CrewAcknowledged);
        });
    }

    [Fact]
    public async Task PostCrewStatus_WhenAssignmentMissing_ReturnsNotFound()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync($"/api/crews/{Guid.NewGuid():D}/status", new CrewStatusUpdateRequest
        {
            Status = CrewAssignmentStatus.Acknowledged
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        problem.Should().NotBeNull();
        problem!.Status.Should().Be((int)HttpStatusCode.NotFound);
    }

    private Task SeedAsync(Func<GridPulseDbContext, Task> seeder)
    {
        return _factory.ExecuteDbContextAsync(seeder);
    }

    private static Crew BuildCrew()
    {
        return new Crew
        {
            Id = Guid.NewGuid(),
            DisplayName = "Crew Echo",
            Region = "north",
            Skills = new List<CrewSkill> { CrewSkill.Distribution },
            CurrentTicketCount = 1,
            Status = CrewStatus.Assigned,
            LastStatusUpdate = DateTimeOffset.UtcNow.AddMinutes(-10),
            PreferredShiftEnd = TimeSpan.FromHours(17)
        };
    }

    private static Ticket BuildTicket(Guid crewId)
    {
        var now = DateTimeOffset.UtcNow.AddMinutes(-30);
        return new Ticket
        {
            Id = Guid.NewGuid(),
            Title = "Transformer fire",
            Description = "",
            OutageReferenceId = "OUT-500",
            Priority = TicketPriority.High,
            Status = TicketStatus.Open,
            AffectedAssets = new List<string> { "TX-500" },
            CustomerImpact = 120,
            AssignedCrewId = crewId,
            AutomationSource = "dispatcher",
            AuditVersion = 1,
            OpenedAt = now,
            UpdatedAt = now,
            Events = new List<AssignmentEvent>(),
            Recommendations = new List<DispatchRecommendation>()
        };
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}