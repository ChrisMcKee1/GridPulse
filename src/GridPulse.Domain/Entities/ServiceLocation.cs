namespace GridPulse.Domain.Entities;

public sealed class ServiceLocation
{
    public Guid Id { get; init; }
    public Guid CustomerId { get; init; }
    public string AddressLine1 { get; init; } = string.Empty;
    public string? AddressLine2 { get; init; }
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    public string MeterId { get; init; } = string.Empty;
    public bool IsPrimary { get; init; }
    public IReadOnlyCollection<UsageReading> UsageReadings { get; init; } = Array.Empty<UsageReading>();
}
