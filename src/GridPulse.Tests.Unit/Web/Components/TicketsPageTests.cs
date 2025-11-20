using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bunit;
using FluentAssertions;
using GridPulse.Application.Models;
using GridPulse.Domain.Enums;
using GridPulse.Tests.Unit.TestInfrastructure;
using GridPulse.Web.Components.Pages;

namespace GridPulse.Tests.Unit.Web.Components;

public sealed class TicketsPageTests : ComponentTestBase
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public void RendersTicketGridAndFilters()
    {
        var ticket = CreateTicketDto();
        EnqueueHttpResponse(BuildPagedResponse(ticket));

        var component = RenderComponent<Tickets>();

        component.WaitForAssertion(() =>
        {
            component.Find("[data-testid='tickets-grid']").TextContent.Should().Contain(ticket.Title);
        });
    }

    [Fact]
    public void PromotingTicketRaisesStatusRequest()
    {
        var ticket = CreateTicketDto();
        var promoted = ticket with { Status = TicketStatus.InProgress, UpdatedAt = ticket.UpdatedAt.AddMinutes(5) };

        EnqueueHttpResponse(BuildPagedResponse(ticket));
        EnqueueHttpResponse(BuildTicketResponse(promoted));

        var component = RenderComponent<Tickets>();

        component.WaitForAssertion(() =>
        {
            component.Find("[data-testid='ticket-status-value']").TextContent.Should().Contain("Open");
        });

        component.Find("[data-testid='tickets-promote-action']").Click();

        component.WaitForAssertion(() =>
        {
            component.Find("[data-testid='ticket-status-value']").TextContent.Should().Contain("InProgress");
        });
    }

    private static HttpResponseMessage BuildPagedResponse(params TicketDto[] tickets)
    {
        var payload = new PagedResult<TicketDto>(tickets, tickets.Length);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, SerializerOptions), Encoding.UTF8, "application/json")
        };
    }

    private static HttpResponseMessage BuildTicketResponse(TicketDto ticket) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(JsonSerializer.Serialize(ticket, SerializerOptions), Encoding.UTF8, "application/json")
    };

    private static TicketDto CreateTicketDto(TicketStatus status = TicketStatus.Open)
    {
        var events = new[]
        {
            new AssignmentEventDto(Guid.NewGuid(), AssignmentEventType.TicketDuplicateFlag, "system", DateTimeOffset.UtcNow.AddMinutes(-30), null, new Dictionary<string, string>())
        };

        return new TicketDto(
            Guid.NewGuid(),
            "Pole fire",
            "OUT-5001",
            TicketPriority.High,
            status,
            42,
            60,
            null,
            null,
            new[] { "TX-09" },
            DateTimeOffset.UtcNow.AddHours(-1),
            DateTimeOffset.UtcNow.AddMinutes(-5),
            null,
            events,
            Array.Empty<DispatchRecommendationDto>());
    }
}