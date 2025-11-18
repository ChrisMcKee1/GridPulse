namespace GridPulse.Application.Models;

public sealed record OutageSummary(
    Guid Id,
    Guid ServiceLocationId,
    string ServiceAddress,
    OutageStatus Status,
    DateTimeOffset ReportedAt,
    DateTimeOffset LastUpdatedAt,
    DateTimeOffset? EstimatedRestoration,
    string? Cause,
    int AffectedCustomers
);
