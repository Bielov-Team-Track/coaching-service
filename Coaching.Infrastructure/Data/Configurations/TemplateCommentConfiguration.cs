using Coaching.Domain.Models.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coaching.Infrastructure.Data.Configurations;

public class PlanCommentConfiguration : IEntityTypeConfiguration<PlanComment>
{
    public void Configure(EntityTypeBuilder<PlanComment> builder)
    {
        builder.ToTable("TemplateComments");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Content)
            .IsRequired()
            .HasMaxLength(2000);

        builder.HasIndex(c => c.TemplateId);
        builder.HasIndex(c => c.UserId);
        builder.HasIndex(c => c.ParentCommentId);

        builder.HasOne(c => c.ParentComment)
            .WithMany(c => c.Replies)
            .HasForeignKey(c => c.ParentCommentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
