using Coaching.Domain.Enums;
using Coaching.Domain.Models.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coaching.Infrastructure.Data.Configurations;

public class TrainingPlanConfiguration : IEntityTypeConfiguration<TrainingPlan>
{
    public void Configure(EntityTypeBuilder<TrainingPlan> builder)
    {
        builder.ToTable("TrainingPlanTemplates");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Description)
            .HasMaxLength(2000);

        builder.Property(t => t.Visibility)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(t => t.PlanType)
            .HasConversion<int>()
            .HasDefaultValue(PlanType.Template);

        builder.Property(t => t.EventId);
        builder.Property(t => t.SourceTemplateId);

        // Indexes
        builder.HasIndex(t => t.CreatedByUserId);
        builder.HasIndex(t => t.PlanType);
        builder.HasIndex(t => t.EventId);
        builder.HasIndex(t => t.ClubId);
        builder.HasIndex(t => t.Visibility);
        builder.HasIndex(t => t.LikeCount);
        builder.HasIndex(t => t.UsageCount);

        // Relationships
        builder.HasOne(t => t.Creator)
            .WithMany()
            .HasForeignKey(t => t.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.Sections)
            .WithOne(s => s.Plan)
            .HasForeignKey(s => s.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.Items)
            .WithOne(i => i.Plan)
            .HasForeignKey(i => i.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.Likes)
            .WithOne(l => l.Plan)
            .HasForeignKey(l => l.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.Bookmarks)
            .WithOne(b => b.Plan)
            .HasForeignKey(b => b.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.Comments)
            .WithOne(c => c.Plan)
            .HasForeignKey(c => c.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
