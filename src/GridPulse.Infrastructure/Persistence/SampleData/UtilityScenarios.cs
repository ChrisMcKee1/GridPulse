using GridPulse.Domain.Enums;

namespace GridPulse.Infrastructure.Persistence.SampleData;

/// <summary>
/// Represents a realistic utility company incident scenario.
/// </summary>
public sealed record UtilityScenario
{
    public required string ScenarioType { get; init; }
    public required string Title { get; init; }
    public required string OutageReference { get; init; }
    public required string Description { get; init; }
    public required TicketPriority Priority { get; init; }
    public required TicketStatus Status { get; init; }
    public required List<string> AffectedAssets { get; init; }
    public required int CustomerImpact { get; init; }
    public required string Zone { get; init; }
    public required List<CrewSkill> RequiredSkills { get; init; }
    public required decimal Latitude { get; init; }
    public required decimal Longitude { get; init; }
    public int? EtaMinutes { get; init; }
    public DateTimeOffset? ScheduledFor { get; init; }
}

/// <summary>
/// Factory for creating realistic utility company scenarios.
/// </summary>
public static class UtilityScenarios
{
    public static List<UtilityScenario> CreateScenarios(DateTimeOffset referenceTime, List<string> enabledScenarios)
    {
        var all = new List<UtilityScenario>();

        if (enabledScenarios.Contains("VehicleCollision"))
            all.Add(VehicleCollisionScenario(referenceTime));

        if (enabledScenarios.Contains("StormDamage"))
            all.AddRange(StormDamageScenarios(referenceTime));

        if (enabledScenarios.Contains("NewConstruction"))
            all.Add(NewConstructionScenario(referenceTime));

        if (enabledScenarios.Contains("BuildingDemo"))
            all.Add(BuildingDemoScenario(referenceTime));

        if (enabledScenarios.Contains("EquipmentFailure"))
            all.Add(EquipmentFailureScenario(referenceTime));

        if (enabledScenarios.Contains("UndergroundFault"))
            all.Add(UndergroundFaultScenario(referenceTime));

        if (enabledScenarios.Contains("Wildlife"))
            all.Add(WildlifeContactScenario(referenceTime));

        if (enabledScenarios.Contains("ThirdPartyDamage"))
            all.Add(ThirdPartyDamageScenario(referenceTime));

        return all;
    }

    private static UtilityScenario VehicleCollisionScenario(DateTimeOffset now) => new()
    {
        ScenarioType = "VehicleCollision",
        Title = "Vehicle collision with transformer TX-NORTH-442",
        OutageReference = "INCIDENT-NORTH-2024-TX442",
        Description = "Vehicle collision reported at Oak St & Maple Ave. Transformer TX-NORTH-442 knocked off foundation. Visible oil leak. Feeder FDR-NORTH-14A auto-sectioned upstream. Heavy equipment required for transformer replacement.",
        Priority = TicketPriority.Critical,
        Status = TicketStatus.InProgress,
        AffectedAssets = new() { "TX-NORTH-442", "FDR-NORTH-14A", "POLE-N-8847" },
        CustomerImpact = 340,
        Zone = "North",
        RequiredSkills = new() { CrewSkill.Transformer, CrewSkill.HighVoltage },
        Latitude = 35.7796m,
        Longitude = -78.6382m,
        EtaMinutes = 28
    };

    private static List<UtilityScenario> StormDamageScenarios(DateTimeOffset now) => new()
    {
        new()
        {
            ScenarioType = "StormDamage",
            Title = "Multiple tree-on-line incidents - Storm Debbie",
            OutageReference = "STORM-DEBBIE-WEST-001",
            Description = "Thunderstorm cell moved through West zone at 2:35 PM. Multiple reports of trees on distribution lines. Feeders 23A and 23B both reporting faults. SCADA shows recloser operations at multiple locations. Tree removal and line repair required.",
            Priority = TicketPriority.High,
            Status = TicketStatus.InProgress,
            AffectedAssets = new() { "FDR-WEST-23A", "FDR-WEST-23B", "POLE-W-8833", "POLE-W-8901", "RECLOSER-W23-R04" },
            CustomerImpact = 1240,
            Zone = "West",
            RequiredSkills = new() { CrewSkill.Distribution, CrewSkill.HighVoltage },
            Latitude = 35.7515m,
            Longitude = -78.7250m,
            EtaMinutes = 18
        },
        new()
        {
            ScenarioType = "StormDamage",
            Title = "Storm damage assessment - multiple poles down",
            OutageReference = "STORM-DEBBIE-SOUTH-002",
            Description = "High winds in South zone caused multiple pole failures along County Road 42. Estimated 6-8 poles down. Primary conductor on ground. Area secure, no injuries. Full patrol and reconstruction required.",
            Priority = TicketPriority.Critical,
            Status = TicketStatus.Open,
            AffectedAssets = new() { "FDR-SOUTH-11C", "POLE-S-6741", "POLE-S-6742", "POLE-S-6743", "POLE-S-6744" },
            CustomerImpact = 892,
            Zone = "South",
            RequiredSkills = new() { CrewSkill.Distribution, CrewSkill.HighVoltage },
            Latitude = 35.7200m,
            Longitude = -78.6100m
        }
    };

    private static UtilityScenario NewConstructionScenario(DateTimeOffset now) => new()
    {
        ScenarioType = "NewConstruction",
        Title = "New service connection - Meadowbrook Commons Phase 2",
        OutageReference = "PROJECT-EAST-MB-PHASE2",
        Description = "Install new pad-mount transformer and underground laterals for Meadowbrook Commons subdivision Phase 2. 47 new residential lots. Scheduled outage window 6 AM - 11 AM on Saturday. Transformer TX-EAST-NEW-091 on site ready for installation.",
        Priority = TicketPriority.Medium,
        Status = TicketStatus.Open,
        AffectedAssets = new() { "TX-EAST-NEW-091", "UG-LATERAL-MB2-001", "UG-LATERAL-MB2-002", "FDR-EAST-08A" },
        CustomerImpact = 0,
        Zone = "East",
        RequiredSkills = new() { CrewSkill.Underground, CrewSkill.Transformer },
        Latitude = 35.8000m,
        Longitude = -78.5900m,
        ScheduledFor = new DateTimeOffset(now.Year, now.Month, now.Day, 6, 0, 0, TimeSpan.Zero).AddDays(3) // Saturday 6 AM UTC
    };

    private static UtilityScenario BuildingDemoScenario(DateTimeOffset now) => new()
    {
        ScenarioType = "BuildingDemo",
        Title = "Service disconnect for demolition - Old Acme Warehouse",
        OutageReference = "DEMO-CENTRAL-ACME-001",
        Description = "Disconnect 480V 3-phase service for Acme Warehouse demolition permit. Coordinate with city inspector on site. Remove meter and disconnect at transformer secondary. Transformer TX-CENTRAL-229 to remain energized for neighboring businesses.",
        Priority = TicketPriority.Low,
        Status = TicketStatus.Open,
        AffectedAssets = new() { "TX-CENTRAL-229", "METER-COM-4472", "DISC-ACME-001" },
        CustomerImpact = 1,
        Zone = "Central",
        RequiredSkills = new() { CrewSkill.Distribution, CrewSkill.HighVoltage },
        Latitude = 35.7804m,
        Longitude = -78.6391m,
        ScheduledFor = new DateTimeOffset(now.Year, now.Month, now.Day, 9, 0, 0, TimeSpan.Zero).AddDays(2) // Thursday 9 AM UTC
    };

    private static UtilityScenario EquipmentFailureScenario(DateTimeOffset now) => new()
    {
        ScenarioType = "EquipmentFailure",
        Title = "Transformer oil leak detected - TX-SOUTH-667",
        OutageReference = "MAINT-SOUTH-TX667",
        Description = "Routine patrol reported oil stains under TX-SOUTH-667. Visual inspection confirms active leak from gasket. Transformer installed 1987, due for replacement. SCADA temps normal but rising. Plan customer notifications and load transfer to TX-SOUTH-668 before replacement.",
        Priority = TicketPriority.High,
        Status = TicketStatus.InProgress,
        AffectedAssets = new() { "TX-SOUTH-667", "FDR-SOUTH-08C", "TX-SOUTH-668" },
        CustomerImpact = 85,
        Zone = "South",
        RequiredSkills = new() { CrewSkill.Transformer, CrewSkill.HighVoltage },
        Latitude = 35.7100m,
        Longitude = -78.6500m
    };

    private static UtilityScenario UndergroundFaultScenario(DateTimeOffset now) => new()
    {
        ScenarioType = "UndergroundFault",
        Title = "Underground cable fault - Riverside Business Park",
        OutageReference = "FAULT-WEST-UG-RBP",
        Description = "Primary underground cable fault detected in Riverside Business Park. Relay R-48 operated on ground fault. Estimated fault location between manholes MH-RBP-03 and MH-RBP-04 based on fault impedance. Network reconfigured to restore 18 customers. 5 commercial still out. Splice crew and fault locating equipment en route.",
        Priority = TicketPriority.Critical,
        Status = TicketStatus.InProgress,
        AffectedAssets = new() { "UG-CABLE-RBP-MAIN", "JUNCTION-RBP-J04", "MH-RBP-03", "MH-RBP-04", "RELAY-R48" },
        CustomerImpact = 5,
        Zone = "West",
        RequiredSkills = new() { CrewSkill.Underground, CrewSkill.Fiber },
        Latitude = 35.7600m,
        Longitude = -78.7100m,
        EtaMinutes = 22
    };

    private static UtilityScenario WildlifeContactScenario(DateTimeOffset now) => new()
    {
        ScenarioType = "Wildlife",
        Title = "Substation breaker trip - wildlife contact suspected",
        OutageReference = "SUB-NORTH-WILDLIFE-001",
        Description = "Main breaker M12 at Northridge substation tripped at 3:47 AM. Successful reclose after 30 seconds. Brief outage to 2,800 customers. Visual inspection found evidence of raccoon contact on buswork. Animal guards installed on A-phase. No equipment damage detected. Thermal scan completed. Returned to service 4:15 AM.",
        Priority = TicketPriority.Medium,
        Status = TicketStatus.Resolved,
        AffectedAssets = new() { "SUB-NORTHRIDGE-MAIN", "BREAKER-NR-M12", "BUS-NR-138KV-A" },
        CustomerImpact = 2800,
        Zone = "North",
        RequiredSkills = new() { CrewSkill.HighVoltage, CrewSkill.Transformer },
        Latitude = 35.8100m,
        Longitude = -78.6200m
    };

    private static UtilityScenario ThirdPartyDamageScenario(DateTimeOffset now) => new()
    {
        ScenarioType = "ThirdPartyDamage",
        Title = "Underground cable struck - fiber optic installation crew",
        OutageReference = "DAMAGE-CENTRAL-FIBER-001",
        Description = "Third-party contractor installing fiber optic cable struck 15kV underground cable with horizontal boring equipment at intersection of Main St and 5th Ave. Cable de-energized automatically by protective relay. Contractor has valid USA 811 ticket on file. Splice repair required. Police on scene directing traffic. Estimated 4-6 hours for repair.",
        Priority = TicketPriority.Critical,
        Status = TicketStatus.InProgress,
        AffectedAssets = new() { "UG-CABLE-15KV-C44", "SPLICE-C44-S09", "RELAY-C-MH11" },
        CustomerImpact = 127,
        Zone = "Central",
        RequiredSkills = new() { CrewSkill.Underground, CrewSkill.HighVoltage },
        Latitude = 35.7780m,
        Longitude = -78.6400m,
        EtaMinutes = 12
    };
}
