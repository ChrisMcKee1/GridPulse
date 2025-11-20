using FluentAssertions;
using GridPulse.Application.Abstractions;
using GridPulse.Application.Exceptions;
using GridPulse.Application.Models;
using GridPulse.Application.Services;
using GridPulse.Domain.Entities;
using GridPulse.Domain.Enums;
using NSubstitute;

namespace GridPulse.Tests.Unit.Application;

public sealed class TicketWriteServiceTests
{
    private readonly ITicketRepository _ticketRepository = Substitute.For<ITicketRepository>();
    private readonly IUserContext _userContext = Substitute.For<IUserContext>();
    private readonly TestTimeProvider _timeProvider = new(DateTimeOffset.Parse("2025-11-20T12:00:00Z"));

    public TicketWriteServiceTests()
    {
        _userContext.Current.Returns(new UserIdentity(true, "Auto Operator", new Dictionary<string, string>(), new[] { "operator" }));
    }

    [Fact]
    public async Task UpdateStatusAsync_PromotesTicketAndAppendsAuditEvent()
    {
        var ticket = BuildTicket(TicketStatus.Open, openedAt: _timeProvider.GetUtcNow().AddMinutes(-2));
        _ticketRepository.GetByIdAsync(ticket.Id, Arg.Any<CancellationToken>()).Returns(ticket);

        var service = CreateService();

        var result = await service.UpdateStatusAsync(ticket.Id, new TicketStatusUpdateRequest
        {
            Status = TicketStatus.InProgress
        });

        result.Status.Should().Be(TicketStatus.InProgress);
        result.Events.Should().ContainSingle(e => e.EventType == AssignmentEventType.TicketEdited && e.Actor == "Auto Operator");

        await _ticketRepository.Received().AddEventsAsync(
            Arg.Is<IEnumerable<AssignmentEvent>>(events =>
                events.Single().EventType == AssignmentEventType.TicketEdited &&
                events.Single().Details["fromStatus"] == TicketStatus.Open.ToString() &&
                events.Single().Details["toStatus"] == TicketStatus.InProgress.ToString()),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateStatusAsync_InvalidTransitionThrows()
    {
        var ticket = BuildTicket(TicketStatus.Open);
        _ticketRepository.GetByIdAsync(ticket.Id, Arg.Any<CancellationToken>()).Returns(ticket);

        var service = CreateService();

        await Assert.ThrowsAsync<TicketWorkflowException>(() => service.UpdateStatusAsync(ticket.Id, new TicketStatusUpdateRequest
        {
            Status = TicketStatus.Resolved
        }));
    }

    [Fact]
    public async Task UpdateStatusAsync_CancelRequiresReason()
    {
        var ticket = BuildTicket(TicketStatus.Open);
        _ticketRepository.GetByIdAsync(ticket.Id, Arg.Any<CancellationToken>()).Returns(ticket);

        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateStatusAsync(ticket.Id, new TicketStatusUpdateRequest
        {
            Status = TicketStatus.Cancelled
        }));
    }

    [Fact]
    public async Task DetectPotentialDuplicatesAsync_FlagsMatchingOutageWithinWindow()
    {
        var candidate = BuildTicket(TicketStatus.Open, outageReferenceId: "OUT-100", openedAt: _timeProvider.GetUtcNow().AddMinutes(-3));
        _ticketRepository.GetAsync(Arg.Any<TicketQueryOptions>(), Arg.Any<CancellationToken>())
            .Returns(new[] { candidate });

        var service = CreateService();

        var duplicates = await service.DetectPotentialDuplicatesAsync(new TicketCreateRequest
        {
            Title = "Follow-up outage",
            OutageReferenceId = "OUT-100",
            Priority = TicketPriority.High,
            AffectedAssets = new List<string> { "TX-101" }
        });

        duplicates.Should().ContainSingle(t => t.Id == candidate.Id);

        await _ticketRepository.Received().AddEventsAsync(
            Arg.Is<IEnumerable<AssignmentEvent>>(events =>
                events.Single().EventType == AssignmentEventType.TicketDuplicateFlag &&
                events.Single().TicketId == candidate.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DetectPotentialDuplicatesAsync_IgnoresTicketsOutsideWindow()
    {
        var candidate = BuildTicket(TicketStatus.Open, openedAt: _timeProvider.GetUtcNow().AddMinutes(-10));
        _ticketRepository.GetAsync(Arg.Any<TicketQueryOptions>(), Arg.Any<CancellationToken>())
            .Returns(new[] { candidate });

        var service = CreateService();

        var duplicates = await service.DetectPotentialDuplicatesAsync(new TicketCreateRequest
        {
            Title = "Late outage",
            OutageReferenceId = candidate.OutageReferenceId,
            Priority = TicketPriority.Medium,
            AffectedAssets = new List<string> { "TX-101" }
        });

        duplicates.Should().BeEmpty();
        await _ticketRepository.DidNotReceive().AddEventsAsync(Arg.Any<IEnumerable<AssignmentEvent>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DetectPotentialDuplicatesAsync_DetectsAssetOverlap()
    {
        var candidate = BuildTicket(TicketStatus.InProgress, outageReferenceId: "OUT-222", affectedAssets: new[] { "TX-900" });
        _ticketRepository.GetAsync(Arg.Any<TicketQueryOptions>(), Arg.Any<CancellationToken>())
            .Returns(new[] { candidate });

        var service = CreateService();

        var duplicates = await service.DetectPotentialDuplicatesAsync(new TicketCreateRequest
        {
            Title = "Transformer incident",
            OutageReferenceId = "OUT-333",
            Priority = TicketPriority.Critical,
            AffectedAssets = new List<string> { "TX-900" }
        });

        duplicates.Should().ContainSingle(t => t.Id == candidate.Id);
    }

    private TicketWriteService CreateService()
    {
        return new TicketWriteService(_ticketRepository, _userContext, _timeProvider);
    }

    private static Ticket BuildTicket(
        TicketStatus status,
        string outageReferenceId = "OUT-001",
        IEnumerable<string>? affectedAssets = null,
        DateTimeOffset? openedAt = null)
    {
        var assets = affectedAssets?.ToList() ?? new List<string> { "TX-101" };
        var opened = openedAt ?? DateTimeOffset.UtcNow.AddMinutes(-1);

        return new Ticket
        {
            Id = Guid.NewGuid(),
            Title = "Sample ticket",
            Description = "",
            OutageReferenceId = outageReferenceId,
            Priority = TicketPriority.Medium,
            Status = status,
            AffectedAssets = assets,
            CustomerImpact = 10,
            AutomationSource = "ingestion",
            AuditVersion = 1,
            OpenedAt = opened,
            UpdatedAt = opened,
            ClosedAt = null,
            Events = new List<AssignmentEvent>(),
            Recommendations = new List<DispatchRecommendation>()
        };
    }

    private sealed class TestTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow;

        public TestTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow() => _utcNow;
    }
}