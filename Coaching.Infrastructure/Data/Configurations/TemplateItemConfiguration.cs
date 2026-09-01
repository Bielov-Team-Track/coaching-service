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

        builder.Property(i => i.Kind)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(i => i.Notes)
            .HasMaxLength(PlanItem.NotesMaxLength);

        builder.Property(i => i.Title)
            .HasMaxLength(PlanItem.TitleMaxLength);

        builder.HasIndex(i => new { i.TemplateId, i.Order });
        builder.HasIndex(i => i.DrillId);

        // Only a Drill row points at a drill, so the FK is optional. Restrict still applies:
        // a drill in use by a plan cannot be deleted out from under it.
        builder.HasOne(i => i.Drill)
            .WithMany()
            .HasForeignKey(i => i.DrillId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
