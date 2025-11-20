using GridPulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GridPulse.Infrastructure.Persistence.Configurations;

internal sealed class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("tickets");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();
        builder.Property(t => t.OutageReferenceId)
               .HasMaxLength(64)
               .IsRequired();
        builder.Property(t => t.Title)
               .HasMaxLength(140)
               .IsRequired();
        builder.Property(t => t.Description)
               .HasMaxLength(2000);
        builder.Property(t => t.AutomationSource)
               .HasMaxLength(64)
               .HasDefaultValue("ingestion");
        builder.Property(t => t.Priority)
               .HasConversion<string>()
               .HasMaxLength(32);
        builder.Property(t => t.Status)
               .HasConversion<string>()
               .HasMaxLength(32);
        builder.Property(t => t.AuditVersion)
               .IsConcurrencyToken();
        builder.Property(t => t.AffectedAssets)
               .HasConversion(
                   assets => JsonColumnHelpers.Serialize(assets),
                   json => JsonColumnHelpers.DeserializeList<string>(json))
               .Metadata.SetValueComparer(JsonColumnHelpers.CreateCollectionComparer<string>());

        builder.HasMany(t => t.Events)
               .WithOne(e => e.Ticket)
               .HasForeignKey(e => e.TicketId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.Recommendations)
               .WithOne(r => r.Ticket)
               .HasForeignKey(r => r.TicketId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.Priority);
        builder.HasIndex(t => t.AssignedCrewId);
    }
}
