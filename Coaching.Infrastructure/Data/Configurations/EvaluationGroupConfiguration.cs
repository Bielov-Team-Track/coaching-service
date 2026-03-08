using Coaching.Domain.Models.Evaluation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coaching.Infrastructure.Data.Configurations;

public class EvaluationGroupConfiguration : IEntityTypeConfiguration<EvaluationGroup>
{
    public void Configure(EntityTypeBuilder<EvaluationGroup> builder)
    {
        builder.HasKey(e => e.Id);

        builder.HasOne(e => e.Session)
            .WithMany(s => s.Groups)
            .HasForeignKey(e => e.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(e => e.Name).HasMaxLength(100);
        builder.HasIndex(e => e.SessionId);
    }
}
