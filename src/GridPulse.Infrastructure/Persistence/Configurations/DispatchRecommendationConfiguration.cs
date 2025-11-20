using GridPulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GridPulse.Infrastructure.Persistence.Configurations;

internal sealed class DispatchRecommendationConfiguration : IEntityTypeConfiguration<DispatchRecommendation>
{
    public void Configure(EntityTypeBuilder<DispatchRecommendation> builder)
    {
        builder.ToTable("dispatch_recommendations");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.CompositeScore)
               .HasPrecision(5, 4);
        builder.Property(r => r.ScoreComponents)
               .HasConversion(
                   dict => JsonColumnHelpers.Serialize(dict),
                   json => JsonColumnHelpers.DeserializeDictionary<double>(json))
               .Metadata.SetValueComparer(JsonColumnHelpers.CreateDictionaryComparer<double>());
        builder.Property(r => r.OverrideReason)
               .HasMaxLength(256);
        builder.Property(r => r.CreatedAt)
               .IsRequired();
        builder.Property(r => r.IsAutoSelected)
               .HasDefaultValue(true);
        builder.Property(r => r.IsOverride)
               .HasDefaultValue(false);

        builder.HasIndex(r => new { r.TicketId, r.CrewId, r.CreatedAt });
    }
}
