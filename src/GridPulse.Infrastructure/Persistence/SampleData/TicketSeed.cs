using Bogus;
using GridPulse.Application.Abstractions;
using GridPulse.Application.Models;
using GridPulse.Domain.Entities;
using GridPulse.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace GridPulse.Infrastructure.Persistence.SampleData;

internal sealed class TicketSeed(
    GridPulseDbContext dbContext,
    IOptions<TicketSeedOptions> options,
    ILogger<TicketSeed> logger,
    ITicketAutomationService automationService,
    TimeProvider? timeProvider = null)
    : IDataSeeder
{
    private static readonly Guid DemoTicketId = Guid.Parse("bd34f4d1-6cf0-49ad-8dd3-88f8d97bb5c8");
    private static readonly Guid DemoAssignedCrewId = Guid.Parse("e4e2a3cc-8d6d-4a32-8d3e-4a5f5d948c9d");
    private static readonly Guid DemoStaleCrewId = Guid.Parse("3d0f772f-8a95-49f0-a4cf-8d6c3e2d83b2");
    private const string DemoDispatchOutageId = "DEMO-DISPATCH-001";
    private const string DemoTrackingId = "SEED-TRACK-001";

    private readonly TicketSeedOptions _options = options.Value;
    private readonly ILogger<TicketSeed> _logger = logger;
    private readonly ITicketAutomationService _automationService = automationService;
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (await dbContext.Tickets.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            _logger.LogInformation("Skipping ticket seed; tickets already exist.");
            return;
        }

        var crewFaker = CreateCrewFaker();
        var crews = crewFaker.Generate(Math.Max(1, _options.CrewCount));
        EnsureDemoCrews(crews);
        await dbContext.Crews.AddRangeAsync(crews, cancellationToken).ConfigureAwait(false);

        var referenceTime = _timeProvider.GetUtcNow();
        var snapshots = CreateInitialSnapshots(crews);
        EnsureDemoSnapshots(snapshots, referenceTime, _options.TelemetryCadence);
        await dbContext.CrewLocationSnapshots.AddRangeAsync(snapshots, cancellationToken).ConfigureAwait(false);

        var ticketFaker = CreateTicketFaker(crews.Select(c => c.Id).ToArray());
        var tickets = ticketFaker.Generate(Math.Max(1, _options.TargetTicketCount));
        var demoDataset = CreateDemoDataset(crews, referenceTime);
        tickets.Insert(0, demoDataset.Ticket);
        await dbContext.Tickets.AddRangeAsync(tickets, cancellationToken).ConfigureAwait(false);

        if (demoDataset.Recommendations.Count > 0)
        {
            await dbContext.DispatchRecommendations.AddRangeAsync(demoDataset.Recommendations, cancellationToken).ConfigureAwait(false);
        }

        var events = tickets.Select(ticket => new AssignmentEvent
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            EventType = AssignmentEventType.TicketCreated,
            Actor = "automation",
            OccurredAt = ticket.OpenedAt,
            Details = new Dictionary<string, string>
            {
                ["source"] = ticket.AutomationSource
            }
        }).ToList();

        if (demoDataset.Events.Count > 0)
        {
            events.AddRange(demoDataset.Events);
        }

        await dbContext.AssignmentEvents.AddRangeAsync(events, cancellationToken).ConfigureAwait(false);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var (automationCreated, automationSuppressed) = await SeedAutomationTicketsAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Seeded {TicketCount} synthetic tickets, {AutomationCount} automation tickets (suppressed {DuplicateSuppressions}), {CrewCount} crews, and {SnapshotCount} telemetry snapshots for ticketing demo.",
            tickets.Count,
            automationCreated,
            automationSuppressed,
            crews.Count,
            snapshots.Count);
    }

    private async Task<(int Created, int Suppressed)> SeedAutomationTicketsAsync(CancellationToken cancellationToken)
    {
        var seeds = CreateAutomationSeeds(_timeProvider.GetUtcNow());
        var created = 0;
        var suppressed = 0;

        foreach (var payload in seeds)
        {
            var result = await _automationService
                .CreateOrUpdateAutomatedTicketAsync(payload, cancellationToken)
                .ConfigureAwait(false);

            if (result is not null)
            {
                created++;
            }
            else
            {
                suppressed++;
            }
        }

        return (created, suppressed);
    }

    private static IReadOnlyList<TicketSeedData> CreateAutomationSeeds(DateTimeOffset referenceTime)
    {
        static TicketSeedData Build(
            string outageReference,
            string title,
            TicketPriority priority,
            string description,
            string[] assets,
            int customerImpact,
            DateTimeOffset openedAt) => new(
                outageReference,
                title,
                priority,
                description,
                assets,
                customerImpact,
                openedAt);

        return new[]
        {
            Build(
                "AUTO-FDR-041",
                "Feeder 41 lockout near West Ridge",
                TicketPriority.High,
                "Breaker lockout triggered from SCADA sensors in the West Ridge industrial corridor.",
                new[] { "FDR-41A" },
                940,
                referenceTime.AddMinutes(-6)),
            Build(
                "AUTO-FDR-041-DUP",
                "Telemetry repeat for West Ridge",
                TicketPriority.Medium,
                "Downstream sensors reported the same feeder fault a few minutes later.",
                new[] { "FDR-41A" },
                380,
                referenceTime.AddMinutes(-4)),
            Build(
                "AUTO-TX-930",
                "Transformer alarm near Lakewood",
                TicketPriority.Critical,
                "Oil pressure dropped below safe thresholds at transformer TX-930.",
                new[] { "TX-LKW-930" },
                1280,
                referenceTime.AddMinutes(-18)),
            Build(
                "AUTO-FDR-230",
                "Midtown double feeder outage",
                TicketPriority.High,
                "Voltage sag detected on parallel feeders 230A and 230B heading into Midtown.",
                new[] { "FDR-230A", "FDR-230B" },
                760,
                referenceTime.AddMinutes(-24))
        };
    }

    private static Faker<Crew> CreateCrewFaker()
    {
        var skills = Enum.GetValues<CrewSkill>();
        return new Faker<Crew>()
            .RuleFor(c => c.Id, _ => Guid.NewGuid())
            .RuleFor(c => c.DisplayName, f => $"Crew {f.IndexFaker + 1:D2}")
            .RuleFor(c => c.Region, f => f.Address.City())
            .RuleFor(c => c.Status, f => f.PickRandom<CrewStatus>())
            .RuleFor(c => c.CurrentTicketCount, f => f.Random.Int(0, 3))
            .RuleFor(c => c.LastStatusUpdate, f => DateTimeOffset.UtcNow.AddMinutes(-f.Random.Int(0, 90)))
            .RuleFor(c => c.PreferredShiftEnd, _ => TimeSpan.FromHours(17))
            .RuleFor(c => c.DeviceEndpoint, _ => null)
            .RuleFor(c => c.Skills, f => f.PickRandom(skills, f.Random.Int(1, 3)).ToList());
    }

    private static Faker<Ticket> CreateTicketFaker(IReadOnlyList<Guid> crewIds)
    {
        var priorities = Enum.GetValues<TicketPriority>();
        var now = DateTimeOffset.UtcNow;
        var crewPool = crewIds.ToArray();

        return new Faker<Ticket>()
            .RuleFor(t => t.Id, _ => Guid.NewGuid())
            .RuleFor(t => t.OutageReferenceId, f => f.Random.AlphaNumeric(8).ToUpperInvariant())
            .RuleFor(t => t.Title, f => f.Hacker.Phrase())
            .RuleFor(t => t.Description, f => f.Lorem.Paragraph())
            .RuleFor(t => t.Priority, f => f.PickRandom(priorities))
            .RuleFor(t => t.Status, f => f.PickRandom<TicketStatus>())
            .RuleFor(t => t.CustomerImpact, f => f.Random.Int(5, 800))
            .RuleFor(t => t.AffectedAssets, f => Enumerable.Range(0, f.Random.Int(1, 4)).Select(_ => f.Address.StreetAddress()).ToList())
            .RuleFor(t => t.AutomationSource, _ => "seed")
            .RuleFor(t => t.OpenedAt, f => now.AddMinutes(-f.Random.Int(10, 240)))
            .RuleFor(t => t.UpdatedAt, (f, t) => t.OpenedAt.AddMinutes(f.Random.Int(1, 60)))
                .RuleFor(t => t.AssignedCrewId, (f, _) => (Guid?)f.PickRandom(crewPool))
            .RuleFor(t => t.AuditVersion, _ => 1);
    }

    private static List<CrewLocationSnapshot> CreateInitialSnapshots(IEnumerable<Crew> crews)
    {
        var faker = new Faker();
        return crews.Select(crew =>
            {
                var capturedAt = DateTimeOffset.UtcNow.AddSeconds(-faker.Random.Int(5, 120));
                var signalAge = (int)Math.Max(0, (DateTimeOffset.UtcNow - capturedAt).TotalSeconds);

                return new CrewLocationSnapshot
                {
                    Id = Guid.NewGuid(),
                    CrewId = crew.Id,
                    Latitude = Math.Round((decimal)faker.Address.Latitude(32, 47), 6),
                    Longitude = Math.Round((decimal)faker.Address.Longitude(-118, -72), 6),
                    CapturedAt = capturedAt,
                    SignalAgeSeconds = signalAge,
                    IsStale = signalAge > 90,
                    SpeedMph = Math.Round(faker.Random.Double(5, 55), 2)
                };
            })
            .ToList();
    }

    private static void EnsureDemoCrews(ICollection<Crew> crews)
    {
        ArgumentNullException.ThrowIfNull(crews);

        crews.Add(new Crew
        {
            Id = DemoAssignedCrewId,
            DisplayName = "Crew Bravo",
            Region = "Central Operations",
            Status = CrewStatus.EnRoute,
            CurrentTicketCount = 1,
            LastStatusUpdate = DateTimeOffset.UtcNow.AddMinutes(-12),
            PreferredShiftEnd = TimeSpan.FromHours(18),
            DeviceEndpoint = null,
            Skills = new List<CrewSkill> { CrewSkill.Distribution, CrewSkill.Underground }
        });

        crews.Add(new Crew
        {
            Id = DemoStaleCrewId,
            DisplayName = "Crew Delta",
            Region = "North Ridge",
            Status = CrewStatus.Available,
            CurrentTicketCount = 0,
            LastStatusUpdate = DateTimeOffset.UtcNow.AddMinutes(-34),
            PreferredShiftEnd = TimeSpan.FromHours(17),
            DeviceEndpoint = null,
            Skills = new List<CrewSkill> { CrewSkill.HighVoltage, CrewSkill.Transformer }
        });
    }

    private static void EnsureDemoSnapshots(List<CrewLocationSnapshot> snapshots, DateTimeOffset referenceTime, TimeSpan telemetryCadence)
    {
        ArgumentNullException.ThrowIfNull(snapshots);

        ReplaceSnapshot(
            snapshots,
            DemoAssignedCrewId,
            referenceTime.AddSeconds(-45),
            45,
            false,
            36.4m,
            -96.803m,
            28.5);

        var staleSignalAge = Math.Max((int)(telemetryCadence.TotalSeconds * 3), 240);
        ReplaceSnapshot(
            snapshots,
            DemoStaleCrewId,
            referenceTime.AddSeconds(-staleSignalAge),
            staleSignalAge,
            true,
            32.78m,
            -97.034m,
            19.2);
    }

    private static void ReplaceSnapshot(
        List<CrewLocationSnapshot> snapshots,
        Guid crewId,
        DateTimeOffset capturedAt,
        int signalAge,
        bool isStale,
        decimal latitude,
        decimal longitude,
        double speedMph)
    {
        snapshots.RemoveAll(snapshot => snapshot.CrewId == crewId);
        snapshots.Add(new CrewLocationSnapshot
        {
            Id = Guid.NewGuid(),
            CrewId = crewId,
            Latitude = latitude,
            Longitude = longitude,
            CapturedAt = capturedAt,
            SignalAgeSeconds = signalAge,
            IsStale = isStale,
            SpeedMph = speedMph
        });
    }

    private DemoSeedData CreateDemoDataset(IReadOnlyList<Crew> crews, DateTimeOffset referenceTime)
    {
        ArgumentNullException.ThrowIfNull(crews);

        var assignedCrew = crews.FirstOrDefault(c => c.Id == DemoAssignedCrewId)
                          ?? throw new InvalidOperationException("Demo assigned crew was not generated.");
        var staleCrew = crews.FirstOrDefault(c => c.Id == DemoStaleCrewId)
                        ?? throw new InvalidOperationException("Demo stale crew was not generated.");

        var ticket = new Ticket
        {
            Id = DemoTicketId,
            OutageReferenceId = DemoDispatchOutageId,
            Title = "Critical feeder fault near Hillcrest",
            Description = "Automation captured a sustained feeder lockout impacting the Hillcrest corridor.",
            Priority = TicketPriority.Critical,
            Status = TicketStatus.InProgress,
            AffectedAssets = new List<string> { "FDR-HIL-014", "TX-HIL-330" },
            CustomerImpact = 1540,
            EtaMinutes = 38,
            AssignedCrewId = assignedCrew.Id,
            AutomationSource = "seed",
            AuditVersion = 1,
            OpenedAt = referenceTime.AddMinutes(-35),
            UpdatedAt = referenceTime.AddMinutes(-5)
        };

        var recommendationTime = referenceTime.AddMinutes(-6);
        var assignedRecommendation = new DispatchRecommendation
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            CrewId = assignedCrew.Id,
            CompositeScore = 0.92,
            ScoreComponents = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["distance"] = 0.82,
                ["skill"] = 0.95,
                ["workload"] = 0.65
            },
            RecommendedRouteEtaMinutes = 32,
            IsAutoSelected = true,
            IsOverride = false,
            CreatedAt = recommendationTime,
            ExpiresAt = recommendationTime.AddMinutes(10)
        };

        var backupRecommendation = new DispatchRecommendation
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            CrewId = staleCrew.Id,
            CompositeScore = 0.74,
            ScoreComponents = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["distance"] = 0.55,
                ["skill"] = 0.9,
                ["workload"] = 0.8
            },
            RecommendedRouteEtaMinutes = 54,
            IsAutoSelected = false,
            IsOverride = false,
            CreatedAt = recommendationTime,
            ExpiresAt = recommendationTime.AddMinutes(10)
        };

        var assignmentEvent = CreateDemoAssignmentEvent(ticket.Id, assignedCrew.Id, assignedCrew.DisplayName, assignedRecommendation, referenceTime.AddMinutes(-4));

        return new DemoSeedData(
            ticket,
            new[] { assignmentEvent },
            new[] { assignedRecommendation, backupRecommendation });
    }

    private static AssignmentEvent CreateDemoAssignmentEvent(
        Guid ticketId,
        Guid crewId,
        string crewName,
        DispatchRecommendation recommendation,
        DateTimeOffset occurredAt)
    {
        var details = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["crewId"] = crewId.ToString(),
            ["crewName"] = crewName,
            ["deliveryStatus"] = "queued",
            ["trackingId"] = DemoTrackingId,
            ["requiresOverride"] = bool.FalseString,
            ["score.composite"] = recommendation.CompositeScore.ToString("P0", CultureInfo.InvariantCulture),
            ["score.distance"] = FormatScoreComponent(recommendation.ScoreComponents, "distance"),
            ["score.skill"] = FormatScoreComponent(recommendation.ScoreComponents, "skill"),
            ["score.workload"] = FormatScoreComponent(recommendation.ScoreComponents, "workload"),
            ["score.etaMinutes"] = recommendation.RecommendedRouteEtaMinutes.ToString(CultureInfo.InvariantCulture)
        };

        return new AssignmentEvent
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            CrewId = crewId,
            EventType = AssignmentEventType.AssignmentPublished,
            Details = details,
            Actor = "Auto Operator",
            OccurredAt = occurredAt
        };
    }

    private static string FormatScoreComponent(IDictionary<string, double> components, string key)
    {
        return components.TryGetValue(key, out var value)
            ? value.ToString("P0", CultureInfo.InvariantCulture)
            : "n/a";
    }

    private sealed record DemoSeedData(Ticket Ticket, IReadOnlyList<AssignmentEvent> Events, IReadOnlyList<DispatchRecommendation> Recommendations);
}