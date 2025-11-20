using GridPulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GridPulse.Infrastructure.Persistence.Configurations;

internal sealed class AssignmentEventConfiguration : IEntityTypeConfiguration<AssignmentEvent>
{
    public void Configure(EntityTypeBuilder<AssignmentEvent> builder)
    {
        builder.ToTable("assignment_events");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Actor)
               .HasMaxLength(64)
               .IsRequired();
        builder.Property(e => e.EventType)
               .HasConversion<string>()
               .HasMaxLength(64)
               .IsRequired();
        builder.Property(e => e.Details)
               .HasConversion(
                   dict => JsonColumnHelpers.Serialize(dict),
                   json => JsonColumnHelpers.DeserializeDictionary<string>(json))
               .Metadata.SetValueComparer(JsonColumnHelpers.CreateDictionaryComparer<string>());
        builder.HasIndex(e => e.TicketId);
        builder.HasIndex(e => e.CrewId);
    }
}
