using GridPulse.Application.Abstractions;

namespace GridPulse.Infrastructure.Repositories;

internal sealed class InMemoryOutageRepository : IOutageRepository
{
    private static readonly IReadOnlyCollection<Outage> SeedData = CreateSeedData();

    public Task<IReadOnlyCollection<Outage>> GetRecentAsync(int take, CancellationToken cancellationToken = default)
    {
        var data = SeedData
            .OrderByDescending(o => o.ReportedAt)
            .Take(take)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<Outage>>(data);
    }

    public Task<Outage?> GetByIdAsync(Guid outageId, CancellationToken cancellationToken = default)
    {
        var outage = SeedData.FirstOrDefault(o => o.Id == outageId);
        return Task.FromResult(outage);
    }

    private static IReadOnlyCollection<Outage> CreateSeedData()
    {
        var locationId = Guid.Parse("f0b8c3a0-4f85-4f50-9b26-7b6b4c1cf001");
        return new[]
        {
            new Outage
            {
                Id = Guid.Parse("17aa5a0f-4f5f-45ec-8dc0-1b53e876c111"),
                ServiceLocationId = locationId,
                ServiceAddress = "123 Contoso Ave, Apex, NC",
                Status = OutageStatus.CrewDispatched,
                ReportedAt = DateTimeOffset.UtcNow.AddHours(-2),
                LastUpdatedAt = DateTimeOffset.UtcNow.AddMinutes(-20),
                EstimatedRestoration = DateTimeOffset.UtcNow.AddHours(1),
                Cause = "Tree on line",
                Events = new List<OutageEvent>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        OutageId = Guid.Parse("17aa5a0f-4f5f-45ec-8dc0-1b53e876c111"),
                        Timestamp = DateTimeOffset.UtcNow.AddHours(-2),
                        Type = OutageEventType.StatusChange,
                        StatusFrom = OutageStatus.Reported,
                        StatusTo = OutageStatus.Acknowledged,
                        Message = "Outage acknowledged",
                        CreatedBy = "system"
                    }
                }
            }
        };
    }
}
