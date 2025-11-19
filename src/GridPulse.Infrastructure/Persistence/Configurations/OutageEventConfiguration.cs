using GridPulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GridPulse.Infrastructure.Persistence.Configurations;

internal sealed class OutageEventConfiguration : IEntityTypeConfiguration<OutageEvent>
{
    public void Configure(EntityTypeBuilder<OutageEvent> builder)
    {
        builder.ToTable("outage_events");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Type)
               .HasConversion<string>()
               .HasMaxLength(64);
        builder.Property(e => e.Message).HasMaxLength(512);
        builder.Property(e => e.CreatedBy).HasMaxLength(128);
    }
}
