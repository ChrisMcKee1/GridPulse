namespace GridPulse.Domain.Entities;

public sealed class UsageReading
{
    public Guid Id { get; init; }
    public Guid ServiceLocationId { get; init; }
    public DateOnly ReadingDate { get; init; }
    public decimal KilowattHours { get; init; }
    public decimal? EstimatedCost { get; init; }
}
