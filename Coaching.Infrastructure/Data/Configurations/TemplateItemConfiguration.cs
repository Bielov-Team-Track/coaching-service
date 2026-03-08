using Coaching.Domain.Models.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coaching.Infrastructure.Data.Configurations;

public class PlanItemConfiguration : IEntityTypeConfiguration<PlanItem>
{
    public void Configure(EntityTypeBuilder<PlanItem> builder)
    {
        builder.ToTable("TemplateItems");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Notes)
            .HasMaxLength(500);

        builder.Property(i => i.DrillId)
            .IsRequired();

        builder.HasIndex(i => new { i.TemplateId, i.Order });
        builder.HasIndex(i => i.DrillId);

        // Drill is local in coaching-service - restore FK relationship
        builder.HasOne(i => i.Drill)
            .WithMany()
            .HasForeignKey(i => i.DrillId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
