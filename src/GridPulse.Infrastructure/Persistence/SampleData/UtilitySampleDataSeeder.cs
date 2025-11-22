using GridPulse.Application.Abstractions;
using GridPulse.Domain.Entities;
using GridPulse.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GridPulse.Infrastructure.Persistence.SampleData;

/// <summary>
/// Seeds the database with realistic utility company scenarios, crews, and telemetry data.
/// </summary>
internal sealed class UtilitySampleDataSeeder : IDataSeeder
{
    private readonly GridPulseDbContext _dbContext;
    private readonly UtilitySampleDataOptions _options;
    private readonly ILogger<UtilitySampleDataSeeder> _logger;
    private readonly IUserContext _userContext;
    private readonly TimeProvider _timeProvider;

    public UtilitySampleDataSeeder(
        GridPulseDbContext dbContext,
        IOptions<UtilitySampleDataOptions> options,
        ILogger<UtilitySampleDataSeeder> logger,
        IUserContext userContext,
        TimeProvider? timeProvider = null)
    {
        _dbContext = dbContext;
        _options = options.Value;
        _logger = logger;
        _userContext = userContext;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Utility sample data seeding is disabled.");
            return;
        }

        if (await _dbContext.Tickets.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            _logger.LogInformation("Skipping utility sample data; tickets already exist.");
            return;
        }

        var referenceTime = _timeProvider.GetUtcNow();
        var actor = _userContext.Current?.DisplayName ?? "system";

        // Create crews
        var crews = UtilityCrews.CreateCrews(_options.ServiceTerritories, _options.CrewCount);
        await _dbContext.Crews.AddRangeAsync(crews, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Created {CrewCount} specialized crews.", crews.Count);

        // Create telemetry snapshots
        var telemetry = UtilityCrews.CreateTelemetry(crews, referenceTime, _options.TelemetryStaleness);
        await _dbContext.CrewLocationSnapshots.AddRangeAsync(telemetry, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Created {SnapshotCount} telemetry snapshots.", telemetry.Count);

        // Create realistic scenarios
        var scenarios = UtilityScenarios.CreateScenarios(referenceTime, _options.IncludeScenarios);
        _logger.LogInformation("Generated {ScenarioCount} realistic utility scenarios.", scenarios.Count);

        // Convert scenarios to tickets and seed them
        var tickets = new List<Ticket>();
        var recommendations = new List<DispatchRecommendation>();
        var events = new List<AssignmentEvent>();

        foreach (var scenario in scenarios)
        {
            var ticket = CreateTicketFromScenario(scenario, referenceTime);
            tickets.Add(ticket);

            // Create ticket created event
            events.Add(new AssignmentEvent
            {
                Id = Guid.NewGuid(),
                TicketId = ticket.Id,
                EventType = AssignmentEventType.TicketCreated,
                Actor = actor,
                OccurredAt = ticket.OpenedAt,
                Details = new Dictionary<string, string>
                {
                    ["source"] = "seed",
                    ["scenario"] = scenario.ScenarioType
                }
            });

            // If ticket has crew assigned or dispatched, create recommendations and assignment events
            if (ticket.Status == TicketStatus.InProgress)
            {
                var ticketRecommendations = CreateRecommendationsForTicket(ticket, scenario, crews, referenceTime);
                recommendations.AddRange(ticketRecommendations);

                // Find assigned crew if any
                var assignedCrew = FindBestCrewMatch(crews, scenario.RequiredSkills, scenario.Zone);
                if (assignedCrew != null && ticket.Status != TicketStatus.Open)
                {
                    // Note: Ticket is immutable, so we need to recreate it with crew assignment
                    // For now, we'll just create the recommendation events

                    // Create recommendation generated event
                    events.Add(new AssignmentEvent
                    {
                        Id = Guid.NewGuid(),
                        TicketId = ticket.Id,
                        EventType = AssignmentEventType.RecommendationGenerated,
                        Actor = "automation",
                        OccurredAt = ticket.OpenedAt.AddMinutes(2),
                        CrewId = null,
                        Details = new Dictionary<string, string>
                        {
                            ["recommendationCount"] = ticketRecommendations.Count.ToString(),
                            ["topScore"] = ticketRecommendations.FirstOrDefault()?.CompositeScore.ToString("F2") ?? "0"
                        }
                    });

                    // Create assignment event
                    events.Add(new AssignmentEvent
                    {
                        Id = Guid.NewGuid(),
                        TicketId = ticket.Id,
                        EventType = AssignmentEventType.AssignmentPublished,
                        Actor = actor,
                        OccurredAt = ticket.OpenedAt.AddMinutes(5),
                        CrewId = assignedCrew.Id,
                        Details = new Dictionary<string, string>
                        {
                            ["crewName"] = assignedCrew.DisplayName,
                            ["eta"] = ticket.EtaMinutes.ToString()!,
                            ["method"] = "auto-selected"
                        }
                    });

                    if (ticket.Status == TicketStatus.InProgress)
                    {
                        // Crew acknowledged
                        events.Add(new AssignmentEvent
                        {
                            Id = Guid.NewGuid(),
                            TicketId = ticket.Id,
                            EventType = AssignmentEventType.CrewAcknowledged,
                            Actor = assignedCrew.DisplayName,
                            OccurredAt = ticket.OpenedAt.AddMinutes(7),
                            CrewId = assignedCrew.Id,
                            Details = new Dictionary<string, string>
                            {
                                ["status"] = "Acknowledged",
                                ["note"] = "Crew en route to incident location."
                            }
                        });
                    }

                    if (ticket.Status == TicketStatus.InProgress)
                    {
                        // Crew on site
                        events.Add(new AssignmentEvent
                        {
                            Id = Guid.NewGuid(),
                            TicketId = ticket.Id,
                            EventType = AssignmentEventType.CrewOnScene,
                            Actor = assignedCrew.DisplayName,
                            OccurredAt = ticket.OpenedAt.AddMinutes(ticket.EtaMinutes ?? 30),
                            CrewId = assignedCrew.Id,
                            Details = new Dictionary<string, string>
                            {
                                ["status"] = "OnSite",
                                ["note"] = "Crew arrived and beginning assessment."
                            }
                        });
                    }
                }
            }
        }

        await _dbContext.Tickets.AddRangeAsync(tickets, cancellationToken).ConfigureAwait(false);
        await _dbContext.DispatchRecommendations.AddRangeAsync(recommendations, cancellationToken).ConfigureAwait(false);
        await _dbContext.AssignmentEvents.AddRangeAsync(events, cancellationToken).ConfigureAwait(false);

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "✅ Seeded utility sample data: {TicketCount} tickets, {CrewCount} crews, {SnapshotCount} telemetry snapshots, {RecommendationCount} recommendations, {EventCount} events.",
            tickets.Count,
            crews.Count,
            telemetry.Count,
            recommendations.Count,
            events.Count);
    }

    private Ticket CreateTicketFromScenario(UtilityScenario scenario, DateTimeOffset referenceTime)
    {
        var openedAt = scenario.ScheduledFor ?? referenceTime.AddMinutes(-Random.Shared.Next(15, 180));
        
        return new Ticket
        {
            Id = Guid.NewGuid(),
            OutageReferenceId = scenario.OutageReference,
            Title = scenario.Title,
            Description = scenario.Description,
            Priority = scenario.Priority,
            Status = scenario.Status,
            AffectedAssets = scenario.AffectedAssets,
            CustomerImpact = scenario.CustomerImpact,
            EtaMinutes = scenario.EtaMinutes,
            AssignedCrewId = null, // Will be set later if applicable
            AutomationSource = "seed",
            AuditVersion = 1,
            OpenedAt = openedAt,
            UpdatedAt = openedAt.AddMinutes(Random.Shared.Next(1, 30)),
            ClosedAt = scenario.Status == TicketStatus.Resolved ? openedAt.AddHours(Random.Shared.Next(2, 8)) : null
        };
    }

    private List<DispatchRecommendation> CreateRecommendationsForTicket(
        Ticket ticket, 
        UtilityScenario scenario, 
        List<Crew> allCrews, 
        DateTimeOffset referenceTime)
    {
        var recommendations = new List<DispatchRecommendation>();
        
        // Find crews with matching skills
        var matchingCrews = allCrews
            .Where(c => scenario.RequiredSkills.Any(skill => c.Skills.Contains(skill)))
            .OrderByDescending(c => scenario.RequiredSkills.Count(skill => c.Skills.Contains(skill)))
            .Take(5)
            .ToList();

        foreach (var crew in matchingCrews)
        {
            var skillMatchScore = (double)scenario.RequiredSkills.Count(skill => crew.Skills.Contains(skill)) / scenario.RequiredSkills.Count;
            var availabilityScore = crew.Status == CrewStatus.Available ? 1.0 : 0.5;
            var workloadScore = Math.Max(0, 1.0 - (crew.CurrentTicketCount * 0.3));
            var distanceScore = Random.Shared.NextDouble() * 0.5 + 0.5; // Simplified
            
            var compositeScore = (skillMatchScore * 0.4) + (availabilityScore * 0.3) + (workloadScore * 0.2) + (distanceScore * 0.1);
            var eta = CalculateEta(scenario.Latitude, scenario.Longitude);

            recommendations.Add(new DispatchRecommendation
            {
                Id = Guid.NewGuid(),
                TicketId = ticket.Id,
                CrewId = crew.Id,
                CompositeScore = Math.Round(compositeScore, 2),
                ScoreComponents = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
                {
                    ["skill"] = Math.Round(skillMatchScore, 2),
                    ["availability"] = Math.Round(availabilityScore, 2),
                    ["workload"] = Math.Round(workloadScore, 2),
                    ["distance"] = Math.Round(distanceScore, 2)
                },
                RecommendedRouteEtaMinutes = eta,
                IsAutoSelected = false,
                IsOverride = false,
                OverrideReason = null,
                CreatedAt = referenceTime,
                ExpiresAt = referenceTime.AddHours(2)
            });
        }

        return recommendations.OrderByDescending(r => r.CompositeScore).ToList();
    }

    private Crew? FindBestCrewMatch(List<Crew> crews, List<CrewSkill> requiredSkills, string zone)
    {
        return crews
            .Where(c => requiredSkills.Any(skill => c.Skills.Contains(skill)))
            .OrderByDescending(c => c.Region == zone ? 1 : 0)
            .ThenByDescending(c => requiredSkills.Count(skill => c.Skills.Contains(skill)))
            .ThenBy(c => c.CurrentTicketCount)
            .FirstOrDefault();
    }

    private int CalculateEta(decimal lat, decimal lon)
    {
        // Simplified ETA calculation - in reality would use actual distance and traffic
        return Random.Shared.Next(15, 45);
    }
}
