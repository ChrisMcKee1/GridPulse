using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using GridPulse.Application.Abstractions;
using GridPulse.Application.Models;
using GridPulse.Application.Services;
using GridPulse.Domain.Entities;
using GridPulse.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace GridPulse.Tests.Unit.Application;

public sealed class DispatchOptimizationServiceTests
{
    private readonly ITicketRepository _ticketRepository = Substitute.For<ITicketRepository>();
    private readonly IDispatchRepository _dispatchRepository = Substitute.For<IDispatchRepository>();
    private readonly ICrewTelemetryFeed _telemetryFeed = Substitute.For<ICrewTelemetryFeed>();
    private readonly IUserContext _userContext = Substitute.For<IUserContext>();
    private readonly ILogger<DispatchOptimizationService> _logger = NullLogger<DispatchOptimizationService>.Instance;
    private readonly FrozenTimeProvider _timeProvider = new(DateTimeOffset.Parse("2025-11-20T12:00:00Z"));

    public DispatchOptimizationServiceTests()
    {
        _userContext.Current.Returns(new UserIdentity(true, "Auto Dispatcher", new Dictionary<string, string>(), new[] { "dispatcher" }));
    }

    [Fact]
    public async Task GetRecommendationsAsync_RanksCrewsByCompositeScore()
    {
        var ticket = BuildTicket(priority: TicketPriority.Critical, affectedAssets: new[] { "TX-900" });
        var matchingCrew = BuildCrew(skills: new[] { CrewSkill.Transformer }, currentTicketCount: 0);
        var overloadedCrew = BuildCrew(skills: new[] { CrewSkill.Distribution }, currentTicketCount: 4);

        _ticketRepository.GetByIdAsync(ticket.Id, Arg.Any<CancellationToken>()).Returns(ticket);
        _dispatchRepository.GetCrewsAsync(Arg.Any<CancellationToken>()).Returns(new[] { overloadedCrew, matchingCrew });
        _telemetryFeed.GetLatestSnapshotsAsync(Arg.Any<CancellationToken>()).Returns(new[]
        {
            BuildSnapshot(matchingCrew, isStale: false),
            BuildSnapshot(overloadedCrew, isStale: false)
        });

        var service = CreateService();

        var envelope = await service.GetRecommendationsAsync(ticket.Id);

        envelope.Recommendations.Should().HaveCount(2);
        var top = envelope.Recommendations.First();
        top.Crew.CrewId.Should().Be(matchingCrew.Id, "crews with matching skill and lower workload should rank first");
        top.IsAutoSelected.Should().BeTrue();
        envelope.Recommendations.Last().Crew.CrewId.Should().Be(overloadedCrew.Id);

        await _dispatchRepository.Received().ReplaceRecommendationsForTicketAsync(
            ticket.Id,
            Arg.Is<IEnumerable<DispatchRecommendation>>(recs => recs.First().CrewId == matchingCrew.Id && recs.First().IsAutoSelected),
            Arg.Any<CancellationToken>());

        await _ticketRepository.Received().AddEventsAsync(
            Arg.Is<IEnumerable<AssignmentEvent>>(events =>
                events.Single().EventType == AssignmentEventType.RecommendationGenerated &&
                events.Single().TicketId == ticket.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetRecommendationsAsync_AppliesTelemetryStalenessPenalty()
    {
        var ticket = BuildTicket(priority: TicketPriority.High);
        var freshCrew = BuildCrew(region: "North", skills: new[] { CrewSkill.Distribution }, currentTicketCount: 1);
        var staleCrew = BuildCrew(region: "North", skills: new[] { CrewSkill.Distribution }, currentTicketCount: 1);

        _ticketRepository.GetByIdAsync(ticket.Id, Arg.Any<CancellationToken>()).Returns(ticket);
        _dispatchRepository.GetCrewsAsync(Arg.Any<CancellationToken>()).Returns(new[] { freshCrew, staleCrew });
        _telemetryFeed.GetLatestSnapshotsAsync(Arg.Any<CancellationToken>()).Returns(new[]
        {
            BuildSnapshot(freshCrew, isStale: false),
            BuildSnapshot(staleCrew, isStale: true)
        });

        var service = CreateService();

        var envelope = await service.GetRecommendationsAsync(ticket.Id);

        envelope.Recommendations.Should().HaveCount(2);
        envelope.Recommendations.First().Crew.CrewId.Should().Be(freshCrew.Id, "fresh telemetry should retain a higher composite score");

        var freshRecommendation = envelope.Recommendations.Single(r => r.Crew.CrewId == freshCrew.Id);
        var staleRecommendation = envelope.Recommendations.Single(r => r.Crew.CrewId == staleCrew.Id);
        
        staleRecommendation.Crew.IsTelemetryStale.Should().BeTrue();
        staleRecommendation.CompositeScore.Should().BeLessThan(freshRecommendation.CompositeScore, "stale telemetry should have lower composite score");
        staleRecommendation.ScoreComponents["distance"].Should().BeLessThan(freshRecommendation.ScoreComponents["distance"], "stale telemetry should have distance penalty applied");
    }

    private DispatchOptimizationService CreateService()
    {
        return new DispatchOptimizationService(
            _ticketRepository,
            _dispatchRepository,
            _telemetryFeed,
            _userContext,
            _logger,
            _timeProvider);
    }

    private static Ticket BuildTicket(TicketPriority priority, IEnumerable<string>? affectedAssets = null)
    {
        return new Ticket
        {
            Id = Guid.NewGuid(),
            Title = "Outage",
            OutageReferenceId = "OUT-123",
            Priority = priority,
            Status = TicketStatus.InProgress,
            AffectedAssets = affectedAssets?.ToList() ?? new List<string> { "LINE-10" },
            CustomerImpact = 25,
            AutomationSource = "ingestion",
            AuditVersion = 1,
            OpenedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    private static Crew BuildCrew(
        string region = "Central",
        IEnumerable<CrewSkill>? skills = null,
        int currentTicketCount = 0)
    {
        return new Crew
        {
            Id = Guid.NewGuid(),
            DisplayName = $"Crew-{Guid.NewGuid():N}",
            Region = region,
            Skills = skills?.ToList() ?? new List<CrewSkill> { CrewSkill.Distribution },
            CurrentTicketCount = currentTicketCount,
            Status = CrewStatus.Available,
            LastStatusUpdate = DateTimeOffset.UtcNow.AddMinutes(-5)
        };
    }

    private CrewLocationSnapshot BuildSnapshot(Crew crew, bool isStale)
    {
        return new CrewLocationSnapshot
        {
            Id = Guid.NewGuid(),
            CrewId = crew.Id,
            Latitude = 32.779167m,
            Longitude = -96.808891m,
            CapturedAt = _timeProvider.GetUtcNow(),
            SignalAgeSeconds = isStale ? 700 : 60,
            IsStale = isStale,
            SpeedMph = 35
        };
    }

    private sealed class FrozenTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _frozen;

        public FrozenTimeProvider(DateTimeOffset frozen)
        {
            _frozen = frozen;
        }

        public override DateTimeOffset GetUtcNow() => _frozen;
    }
}
