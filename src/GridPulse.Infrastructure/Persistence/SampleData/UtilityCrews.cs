using GridPulse.Domain.Entities;
using GridPulse.Domain.Enums;

namespace GridPulse.Infrastructure.Persistence.SampleData;

/// <summary>
/// Factory for creating specialized utility company crew configurations.
/// </summary>
public static class UtilityCrews
{
    public static List<Crew> CreateCrews(List<string> territories, int targetCount)
    {
        var crews = new List<Crew>();
        var now = DateTimeOffset.UtcNow;

        // Specialized crews - always created
        crews.AddRange(new[]
        {
            CreateCrew("Heavy Equipment Crew Alpha", "North", CrewStatus.Available, 
                new[] { CrewSkill.Transformer, CrewSkill.HighVoltage }, 0, now, 35.7900m, -78.6300m),
            
            CreateCrew("Line Crew Bravo", "West", CrewStatus.EnRoute, 
                new[] { CrewSkill.Distribution, CrewSkill.HighVoltage }, 1, now, 35.7600m, -78.7150m),
            
            CreateCrew("Underground Crew Delta", "East", CrewStatus.Available, 
                new[] { CrewSkill.Underground, CrewSkill.Fiber }, 0, now, 35.7950m, -78.5950m),
            
            CreateCrew("Transformer Crew Echo", "South", CrewStatus.Available, 
                new[] { CrewSkill.Transformer, CrewSkill.HighVoltage }, 0, now, 35.7150m, -78.6450m),
            
            CreateCrew("Cable Crew Charlie", "Central", CrewStatus.Assigned, 
                new[] { CrewSkill.Underground, CrewSkill.Fiber }, 1, now, 35.7750m, -78.6380m),
            
            CreateCrew("Service Crew Foxtrot", "Central", CrewStatus.Available, 
                new[] { CrewSkill.Distribution }, 0, now, 35.7820m, -78.6420m),
            
            CreateCrew("Substation Crew Golf", "North", CrewStatus.Available, 
                new[] { CrewSkill.HighVoltage, CrewSkill.Transformer }, 0, now, 35.8050m, -78.6250m),
            
            CreateCrew("Emergency Response Crew Hotel", "West", CrewStatus.Available, 
                new[] { CrewSkill.Underground, CrewSkill.Distribution, CrewSkill.HighVoltage }, 0, now, 35.7580m, -78.7080m)
        });

        // Add additional general crews if needed to reach target count
        var remaining = targetCount - crews.Count;
        if (remaining > 0)
        {
            for (int i = 0; i < remaining; i++)
            {
                var territory = territories[i % territories.Count];
                var crewNumber = crews.Count + 1;
                var lat = 35.7500m + (decimal)(i * 0.01);
                var lon = -78.6500m + (decimal)(i * 0.01);
                
                crews.Add(CreateCrew(
                    $"General Crew {crewNumber:D2}", 
                    territory, 
                    CrewStatus.Available,
                    new[] { CrewSkill.Distribution, CrewSkill.Transformer },
                    0, 
                    now, 
                    lat, 
                    lon));
            }
        }

        return crews;
    }

    private static Crew CreateCrew(
        string displayName, 
        string region, 
        CrewStatus status, 
        CrewSkill[] skills, 
        int ticketCount, 
        DateTimeOffset statusUpdate,
        decimal latitude,
        decimal longitude)
    {
        var id = Guid.NewGuid();
        var crew = new Crew
        {
            Id = id,
            DisplayName = displayName,
            Region = region,
            Status = status,
            Skills = skills.ToList(),
            CurrentTicketCount = ticketCount,
            LastStatusUpdate = statusUpdate.AddMinutes(-Random.Shared.Next(5, 45)),
            PreferredShiftEnd = TimeSpan.FromHours(17),
            DeviceEndpoint = null
        };

        return crew;
    }

    public static List<CrewLocationSnapshot> CreateTelemetry(List<Crew> crews, DateTimeOffset referenceTime, TimeSpan stalenessThreshold)
    {
        var snapshots = new List<CrewLocationSnapshot>();
        var random = Random.Shared;

        // Base coordinates for Raleigh, NC area
        var baseCoordinates = new Dictionary<string, (decimal lat, decimal lon)>
        {
            ["North"] = (35.8100m, -78.6200m),
            ["South"] = (35.7100m, -78.6400m),
            ["East"] = (35.7950m, -78.5900m),
            ["West"] = (35.7500m, -78.7200m),
            ["Central"] = (35.7804m, -78.6391m)
        };

        foreach (var crew in crews)
        {
            var baseCoord = baseCoordinates.GetValueOrDefault(crew.Region, baseCoordinates["Central"]);
            
            // Add small random offset (±0.02 degrees ≈ 1-2 miles)
            var lat = baseCoord.lat + (decimal)(random.NextDouble() * 0.04 - 0.02);
            var lon = baseCoord.lon + (decimal)(random.NextDouble() * 0.04 - 0.02);

            // Determine if telemetry should be stale based on crew status
            var shouldBeStale = crew.Status == CrewStatus.Offline || random.Next(10) < 2; // 20% chance
            var ageSeconds = shouldBeStale 
                ? (int)(stalenessThreshold.TotalSeconds * 2) // Definitely stale
                : random.Next(10, (int)(stalenessThreshold.TotalSeconds * 0.8)); // Fresh

            var capturedAt = referenceTime.AddSeconds(-ageSeconds);
            var speed = crew.Status == CrewStatus.EnRoute ? random.Next(25, 55) : random.Next(0, 15);

            snapshots.Add(new CrewLocationSnapshot
            {
                Id = Guid.NewGuid(),
                CrewId = crew.Id,
                Latitude = lat,
                Longitude = lon,
                CapturedAt = capturedAt,
                SignalAgeSeconds = ageSeconds,
                IsStale = shouldBeStale,
                SpeedMph = speed
            });
        }

        return snapshots;
    }
}
