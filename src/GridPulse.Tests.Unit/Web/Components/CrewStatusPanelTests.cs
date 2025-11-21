using System.Collections.Generic;
using Bunit;
using FluentAssertions;
using GridPulse.Application.Models;
using GridPulse.Domain.Enums;
using GridPulse.Tests.Unit.TestInfrastructure;
using GridPulse.Web.Components.Organisms;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace GridPulse.Tests.Unit.Web.Components;

public sealed class CrewStatusPanelTests : ComponentTestBase
{
    [Fact]
    public void ShowsAcknowledgementAlertWhenTimelineMissingAck()
    {
        var crew = BuildCrew();
        var events = new[]
        {
            BuildEvent(AssignmentEventType.AssignmentPublished, DateTimeOffset.UtcNow.AddMinutes(-2), crew.CrewId)
        };

        var component = RenderComponent<CrewStatusPanel>(parameters => parameters
            .Add(p => p.Crew, crew)
            .Add(p => p.Timeline, events));

        component.Find("[data-testid='crew-status-alert']").TextContent.Should().ContainEquivalentOf("awaiting acknowledgement");
    }

    [Fact]
    public void ShowsEscalationAlertWhenAcknowledgementIsOverdue()
    {
        var crew = BuildCrew();
        var events = new[]
        {
            BuildEvent(AssignmentEventType.AssignmentPublished, DateTimeOffset.UtcNow.AddMinutes(-8), crew.CrewId)
        };

        var component = RenderComponent<CrewStatusPanel>(parameters => parameters
            .Add(p => p.Crew, crew)
            .Add(p => p.Timeline, events));

        component.Find("[data-testid='crew-status-alert']").TextContent.Should().ContainEquivalentOf("overdue");
    }

    [Fact]
    public void InvokesCallbackWhenSimulationRequested()
    {
        var crew = BuildCrew();
        var events = new[] { BuildEvent(AssignmentEventType.AssignmentPublished, DateTimeOffset.UtcNow, crew.CrewId) };
        CrewAssignmentStatus? reported = null;

        var component = RenderComponent<CrewStatusPanel>(parameters => parameters
            .Add(p => p.Crew, crew)
            .Add(p => p.Timeline, events)
            .Add(p => p.StatusSubmitted, EventCallback.Factory.Create<CrewAssignmentStatus>(this, status => reported = status)));

        component.Find("select[data-testid='crew-status-select']").Change("EnRoute");
        component.Find("button[data-testid='crew-status-send']").Click();

        reported.Should().Be(CrewAssignmentStatus.EnRoute);
    }

    private static CrewStatusDto BuildCrew()
    {
        return new CrewStatusDto(
            Guid.NewGuid(),
            "Crew Sierra",
            CrewStatus.Assigned,
            1,
            new[] { CrewSkill.HighVoltage },
            DateTimeOffset.UtcNow.AddMinutes(-5),
            new CrewLocationSnapshotDto(32.9m, -96.8m, DateTimeOffset.UtcNow.AddMinutes(-1), 38),
            false);
    }

    private static AssignmentEventDto BuildEvent(AssignmentEventType type, DateTimeOffset occurredAt, Guid? crewId = null)
    {
        return new AssignmentEventDto(
            Guid.NewGuid(),
            type,
            "dispatcher",
            occurredAt,
            crewId,
            new Dictionary<string, string>());
    }
}