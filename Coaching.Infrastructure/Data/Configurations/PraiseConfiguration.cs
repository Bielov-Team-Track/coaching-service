using Coaching.Domain.Models.Feedback;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coaching.Infrastructure.Data.Configurations;

public class PraiseConfiguration : IEntityTypeConfiguration<Praise>
{
    public void Configure(EntityTypeBuilder<Praise> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Message).IsRequired().HasMaxLength(500);
        builder.Property(p => p.BadgeType).HasConversion<int>();
        builder.HasIndex(p => p.FeedbackId).IsUnique();
    }
}
