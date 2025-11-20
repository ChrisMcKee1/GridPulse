namespace GridPulse.Infrastructure.Persistence.SampleData;

public sealed class TicketSeedOptions
{
    public const string SectionName = "TicketSeed";

    public int TargetTicketCount { get; set; } = 12;
    public int CrewCount { get; set; } = 8;
    public TimeSpan TelemetryCadence { get; set; } = TimeSpan.FromSeconds(60);
}