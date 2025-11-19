using GridPulse.Domain.Entities;
using GridPulse.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GridPulse.Infrastructure.Persistence.Configurations;

internal sealed class OutageConfiguration : IEntityTypeConfiguration<Outage>
{
    public void Configure(EntityTypeBuilder<Outage> builder)
    {
        builder.ToTable("outages");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedNever();
        builder.Property(o => o.ServiceLocationId).IsRequired();
        builder.Property(o => o.ServiceAddress).HasMaxLength(256);
        builder.Property(o => o.Status)
               .HasConversion<string>()
               .HasMaxLength(64)
               .HasDefaultValue(OutageStatus.Reported);
        builder.Property(o => o.Cause).HasMaxLength(256);
        builder.Property(o => o.LastUpdatedAt).IsRequired();
        builder.Property(o => o.ReportedAt).IsRequired();

        builder.HasMany(o => o.Events)
               .WithOne()
               .HasForeignKey(e => e.OutageId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
