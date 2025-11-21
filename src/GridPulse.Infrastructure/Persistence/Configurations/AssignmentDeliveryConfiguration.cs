using System;
using System.Collections.Generic;
using System.Linq;
using GridPulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GridPulse.Infrastructure.Persistence.Configurations;

internal sealed class AssignmentDeliveryConfiguration : IEntityTypeConfiguration<AssignmentDelivery>
{
    public void Configure(EntityTypeBuilder<AssignmentDelivery> builder)
    {
        builder.ToTable("assignment_deliveries");
        builder.HasKey(delivery => delivery.Id);

        builder.Property(delivery => delivery.TrackingId)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(delivery => delivery.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(delivery => delivery.AttemptCount)
            .HasDefaultValue(1);

        builder.Property(delivery => delivery.CreatedAt)
            .IsRequired();

        builder.Property(delivery => delivery.UpdatedAt)
            .IsRequired();

        builder.Property(delivery => delivery.Payload)
            .HasColumnType("jsonb")
            .HasConversion(
                value => System.Text.Json.JsonSerializer.Serialize(value, (System.Text.Json.JsonSerializerOptions?)null),
                json => string.IsNullOrWhiteSpace(json)
                    ? new Dictionary<string, string>()
                    : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>(),
                new ValueComparer<IDictionary<string, string>>(
                    (a, b) => a!.SequenceEqual(b!),
                    dictionary => dictionary.Aggregate(0, (hash, pair) => HashCode.Combine(hash, pair.GetHashCode())),
                    dictionary => (IDictionary<string, string>)dictionary.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)))
            .IsRequired();

        builder.Property(delivery => delivery.LastError)
            .HasMaxLength(512);

        builder.HasIndex(delivery => new { delivery.TicketId, delivery.CrewId })
            .HasDatabaseName("IX_assignment_deliveries_ticket_crew");
    }
}