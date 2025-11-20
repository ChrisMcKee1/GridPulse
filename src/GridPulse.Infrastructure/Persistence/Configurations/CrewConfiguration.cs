using System;
using GridPulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GridPulse.Infrastructure.Persistence.Configurations;

internal sealed class CrewConfiguration : IEntityTypeConfiguration<Crew>
{
    public void Configure(EntityTypeBuilder<Crew> builder)
    {
        builder.ToTable("crews");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.DisplayName)
               .HasMaxLength(120)
               .IsRequired();
        builder.Property(c => c.Region)
               .HasMaxLength(64)
               .IsRequired();
        builder.Property(c => c.DeviceEndpoint)
               .HasMaxLength(256);
        builder.Property(c => c.Status)
               .HasConversion<string>()
               .HasMaxLength(32);
        builder.Property(c => c.Skills)
               .HasConversion(
                   skills => JsonColumnHelpers.Serialize(skills),
                   json => JsonColumnHelpers.DeserializeList<CrewSkill>(json))
               .Metadata.SetValueComparer(JsonColumnHelpers.CreateCollectionComparer<CrewSkill>());
        builder.Property(c => c.PreferredShiftEnd)
               .HasConversion(
                   span => (int)span.TotalMinutes,
                   minutes => TimeSpan.FromMinutes(minutes))
               .HasColumnName("preferred_shift_end_minutes");

        builder.HasMany(c => c.LocationHistory)
               .WithOne(s => s.Crew)
               .HasForeignKey(s => s.CrewId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Events)
               .WithOne(e => e.Crew)
               .HasForeignKey(e => e.CrewId)
               .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(c => c.Recommendations)
               .WithOne(r => r.Crew)
               .HasForeignKey(r => r.CrewId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.Region);
        builder.HasIndex(c => c.Status);
    }
}
