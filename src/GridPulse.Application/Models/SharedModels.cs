namespace GridPulse.Application.Models;

// ===== Generic Infrastructure Models =====

public sealed record PagedResult<T>(IReadOnlyCollection<T> Items, int TotalCount);

public sealed record UserIdentity(
    bool IsAuthenticated,
    string DisplayName,
    IReadOnlyDictionary<string, string> Claims,
    IReadOnlyCollection<string> Roles)
{
    public static UserIdentity Anonymous { get; } = new(
        false,
        "Anonymous",
        new Dictionary<string, string>(),
        Array.Empty<string>());
}

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
