using Coaching.Domain.Models.Feedback;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coaching.Infrastructure.Data.Configurations;

public class PlayerBadgeConfiguration : IEntityTypeConfiguration<PlayerBadge>
{
    public void Configure(EntityTypeBuilder<PlayerBadge> builder)
    {
        builder.HasKey(pb => pb.Id);
        builder.Property(pb => pb.UserId).IsRequired();
        builder.Property(pb => pb.BadgeType).IsRequired().HasConversion<int>();
        builder.Property(pb => pb.Message).IsRequired().HasMaxLength(500);
        builder.Property(pb => pb.AwardedByUserId).IsRequired();

        builder.HasIndex(pb => pb.UserId);
        builder.HasIndex(pb => pb.EventId);
        builder.HasIndex(pb => pb.PraiseId);

        builder.HasOne(pb => pb.Praise)
            .WithMany()
            .HasForeignKey(pb => pb.PraiseId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
