using Coaching.Domain.Models.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coaching.Infrastructure.Data.Configurations;

public class TrainingPlanRunItemConfiguration : IEntityTypeConfiguration<TrainingPlanRunItem>
{
    public void Configure(EntityTypeBuilder<TrainingPlanRunItem> builder)
    {
        builder.ToTable("TrainingPlanRunItems");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.RunId).IsRequired();
        builder.Property(i => i.PlanItemId).IsRequired();

        builder.Property(i => i.Kind)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(i => i.Title).HasMaxLength(PlanItem.TitleMaxLength);

        builder.HasIndex(i => new { i.RunId, i.Order });
    }
}
