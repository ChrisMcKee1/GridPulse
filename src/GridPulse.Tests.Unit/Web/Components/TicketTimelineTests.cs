using Bunit;
using FluentAssertions;
using GridPulse.Application.Models;
using GridPulse.Domain.Enums;
using GridPulse.Tests.Unit.TestInfrastructure;
using GridPulse.Web.Components.Organisms;

namespace GridPulse.Tests.Unit.Web.Components;

public sealed class TicketTimelineTests : ComponentTestBase
{
    [Fact] // T013
    public void OrdersEventsDescendingByTime()
    {
        var events = new List<AssignmentEventDto>
        {
            BuildEvent(AssignmentEventType.TicketCreated, DateTimeOffset.UtcNow.AddMinutes(-15), "system"),
            BuildEvent(AssignmentEventType.AssignmentPublished, DateTimeOffset.UtcNow.AddMinutes(-5), "dispatcher")
        };

        var component = RenderComponent<TicketTimeline>(parameters =>
            parameters.Add(p => p.Events, events));

        var timeline = component.Find("[data-testid='ticket-timeline']");
        timeline.InnerHtml.Should().Contain("dispatcher");
    }

    [Fact] // T013
    public void HighlightsDuplicateFlags()
    {
        var events = new List<AssignmentEventDto>
        {
            BuildEvent(AssignmentEventType.TicketDuplicateFlag, DateTimeOffset.UtcNow, "operator")
        };

        var component = RenderComponent<TicketTimeline>(parameters =>
            parameters.Add(p => p.Events, events));

        component.Find("[data-testid='ticket-timeline']").InnerHtml.Should().Contain("duplicate");
    }

    private static AssignmentEventDto BuildEvent(AssignmentEventType type, DateTimeOffset occurredAt, string actor)
    {
        return new AssignmentEventDto(Guid.NewGuid(), type, actor, occurredAt, null, new Dictionary<string, string>());
    }
}