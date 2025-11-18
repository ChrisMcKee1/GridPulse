namespace GridPulse.Web.Services.Models;

public sealed record OutageSummaryResponse(
    Guid Id,
    Guid ServiceLocationId,
    string ServiceAddress,
    string Status,
    DateTimeOffset ReportedAt,
    DateTimeOffset LastUpdatedAt,
    DateTimeOffset? EstimatedRestoration,
    string? Cause,
    int AffectedCustomers
);
