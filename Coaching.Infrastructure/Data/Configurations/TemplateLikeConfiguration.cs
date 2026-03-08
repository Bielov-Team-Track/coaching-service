using Coaching.Domain.Models.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coaching.Infrastructure.Data.Configurations;

public class PlanLikeConfiguration : IEntityTypeConfiguration<PlanLike>
{
    public void Configure(EntityTypeBuilder<PlanLike> builder)
    {
        builder.ToTable("TemplateLikes");
        builder.HasKey(l => l.Id);

        builder.HasIndex(l => new { l.TemplateId, l.UserId }).IsUnique();
        builder.HasIndex(l => l.UserId);
    }
}
