using System.IO;

namespace GridPulse.Infrastructure.Persistence.SampleData;

public sealed class SampleDataOptions
{
    public const string SectionName = "SampleData";

    public string RootPath { get; set; } = "sample-data";

    public string OutagesFile { get; set; } = Path.Combine("outages", "outages.csv");

    public string OutageEventsFile { get; set; } = Path.Combine("outages", "outage-events.csv");
}
