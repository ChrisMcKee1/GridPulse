using Bunit;
using FluentAssertions;
using GridPulse.Tests.Unit.TestInfrastructure;
using GridPulse.Web.Components.Pages;

namespace GridPulse.Tests.Unit.Web.Components;

public sealed class TicketsPageTests : ComponentTestBase
{
    [Fact(Skip = "Tickets page not implemented yet")] // T013
    public void RendersTicketGridAndFilters()
    {
        var component = RenderComponent<Tickets>();

        component.Find("[data-testid='tickets-heading']").InnerHtml.Should().Contain("Tickets");
        component.Find("[data-testid='tickets-grid']").Should().NotBeNull();
    }

    [Fact(Skip = "Tickets page not implemented yet")] // T013
    public void PromotingTicketRaisesStatusRequest()
    {
        var component = RenderComponent<Tickets>();

        var promoteButton = component.Find("[data-testid='tickets-promote-action']");
        promoteButton.Click();

        component.Instance.Should().NotBeNull();
    }
}