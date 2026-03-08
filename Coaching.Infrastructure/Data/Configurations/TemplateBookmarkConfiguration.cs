using Coaching.Domain.Models.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coaching.Infrastructure.Data.Configurations;

public class PlanBookmarkConfiguration : IEntityTypeConfiguration<PlanBookmark>
{
    public void Configure(EntityTypeBuilder<PlanBookmark> builder)
    {
        builder.ToTable("TemplateBookmarks");
        builder.HasKey(b => b.Id);

        builder.HasIndex(b => new { b.TemplateId, b.UserId }).IsUnique();
        builder.HasIndex(b => b.UserId);
    }
}
