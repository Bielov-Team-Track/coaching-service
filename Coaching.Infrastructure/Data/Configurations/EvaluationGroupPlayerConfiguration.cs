using Coaching.Domain.Models.Evaluation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coaching.Infrastructure.Data.Configurations;

public class EvaluationGroupPlayerConfiguration : IEntityTypeConfiguration<EvaluationGroupPlayer>
{
    public void Configure(EntityTypeBuilder<EvaluationGroupPlayer> builder)
    {
        builder.HasKey(e => e.Id);

        builder.HasOne(e => e.Group)
            .WithMany(g => g.Players)
            .HasForeignKey(e => e.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.GroupId, e.PlayerId }).IsUnique();
    }
}
