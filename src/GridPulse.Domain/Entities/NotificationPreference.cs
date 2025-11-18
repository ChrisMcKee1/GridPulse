namespace GridPulse.Domain.Entities;

public sealed class NotificationPreference
{
    public Guid CustomerId { get; init; }
    public NotificationChannel Channel { get; init; } = NotificationChannel.Email;
    public string? PhoneNumber { get; init; }
    public bool EmailEnabled { get; init; } = true;
    public bool SmsEnabled { get; init; }
}
