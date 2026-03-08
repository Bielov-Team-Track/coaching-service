using Coaching.Domain.Models.Evaluation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coaching.Infrastructure.Data.Configurations;

public class MetricSkillWeightConfiguration : IEntityTypeConfiguration<MetricSkillWeight>
{
    public void Configure(EntityTypeBuilder<MetricSkillWeight> builder)
    {
        builder.HasKey(w => w.Id);

        builder.Property(w => w.Skill)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(w => w.Percentage)
            .HasPrecision(5, 2);

        builder.HasIndex(w => new { w.MetricId, w.Skill })
            .IsUnique();
    }
}
