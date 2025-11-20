using GridPulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GridPulse.Infrastructure.Persistence.Configurations;

internal sealed class CrewLocationSnapshotConfiguration : IEntityTypeConfiguration<CrewLocationSnapshot>
{
    public void Configure(EntityTypeBuilder<CrewLocationSnapshot> builder)
    {
        builder.ToTable("crew_location_snapshots");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.Latitude)
               .HasPrecision(9, 6);
        builder.Property(s => s.Longitude)
               .HasPrecision(9, 6);
        builder.Property(s => s.SignalAgeSeconds)
               .HasDefaultValue(0);
        builder.Property(s => s.IsStale)
               .HasDefaultValue(false);
        builder.Property(s => s.CapturedAt)
               .IsRequired();

        builder.HasIndex(s => new { s.CrewId, s.CapturedAt });
    }
}
