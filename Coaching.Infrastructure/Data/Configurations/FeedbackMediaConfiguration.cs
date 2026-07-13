using Coaching.Domain.Models.Feedback;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coaching.Infrastructure.Data.Configurations;

public class FeedbackMediaConfiguration : IEntityTypeConfiguration<FeedbackMedia>
{
    public void Configure(EntityTypeBuilder<FeedbackMedia> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Url).IsRequired().HasMaxLength(500);
        builder.Property(m => m.Title).HasMaxLength(200);
        builder.Property(m => m.Type).HasConversion<int>().IsRequired();
        builder.HasIndex(m => m.FeedbackId);
    }
}
