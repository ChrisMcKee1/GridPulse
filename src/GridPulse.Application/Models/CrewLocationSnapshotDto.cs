namespace GridPulse.Application.Models;

public sealed record CrewLocationSnapshotDto(
    decimal Latitude,
    decimal Longitude,
    DateTimeOffset CapturedAt,
    double? SpeedMph
);
