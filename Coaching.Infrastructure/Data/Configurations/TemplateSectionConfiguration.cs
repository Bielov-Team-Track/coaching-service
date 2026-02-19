using Coaching.Domain.Models.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coaching.Infrastructure.Data.Configurations;

public class PlanSectionConfiguration : IEntityTypeConfiguration<PlanSection>
{
    public void Configure(EntityTypeBuilder<PlanSection> builder)
    {
        builder.ToTable("TemplateSections");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(s => new { s.TemplateId, s.Order });

        builder.HasMany(s => s.Items)
            .WithOne(i => i.Section)
            .HasForeignKey(i => i.SectionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
