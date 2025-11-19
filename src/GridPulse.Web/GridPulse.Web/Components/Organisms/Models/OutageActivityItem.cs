namespace GridPulse.Web.Components.Organisms.Models;

public sealed record OutageActivityItem(string Title, string Description, DateTimeOffset Timestamp, string StatusVariant);
