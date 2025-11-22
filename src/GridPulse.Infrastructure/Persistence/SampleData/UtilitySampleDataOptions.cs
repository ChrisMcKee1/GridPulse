namespace GridPulse.Infrastructure.Persistence.SampleData;

/// <summary>
/// Configuration for utility company sample data scenarios.
/// </summary>
public sealed class UtilitySampleDataOptions
{
    public const string SectionName = "UtilitySampleData";

    /// <summary>
    /// Enable or disable sample data seeding.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Scenario types to include in the seed data.
    /// </summary>
    public List<string> IncludeScenarios { get; set; } = new()
    {
        "VehicleCollision",
        "StormDamage",
        "NewConstruction",
        "BuildingDemo",
        "EquipmentFailure",
        "UndergroundFault",
        "Wildlife",
        "ThirdPartyDamage"
    };

    /// <summary>
    /// Number of crews to generate (8-12 recommended).
    /// </summary>
    public int CrewCount { get; set; } = 10;

    /// <summary>
    /// Service territories for crews and incidents.
    /// </summary>
    public List<string> ServiceTerritories { get; set; } = new()
    {
        "North",
        "South",
        "East",
        "West",
        "Central"
    };

    /// <summary>
    /// How long before telemetry is considered stale (for demo purposes).
    /// </summary>
    public TimeSpan TelemetryStaleness { get; set; } = TimeSpan.FromMinutes(5);
}
