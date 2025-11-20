namespace GridPulse.Application.Models;

public sealed record TicketSeedData(
    string OutageReferenceId,
    string Title,
    TicketPriority Priority,
    string? Description,
    IReadOnlyCollection<string> AffectedAssets,
    int CustomerImpact,
    DateTimeOffset OpenedAt
);
