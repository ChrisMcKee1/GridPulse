namespace GridPulse.Domain.Entities;

public sealed class Customer
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public NotificationChannel PreferredNotificationChannel { get; init; } = NotificationChannel.Email;
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public IReadOnlyCollection<ServiceLocation> ServiceLocations { get; init; } = Array.Empty<ServiceLocation>();
}
