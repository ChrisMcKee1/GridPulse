namespace GridPulse.Application.Models;

public sealed record PagedResult<T>(IReadOnlyCollection<T> Items, int TotalCount);
