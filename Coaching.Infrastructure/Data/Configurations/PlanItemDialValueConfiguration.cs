using Coaching.Domain.Models.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coaching.Infrastructure.Data.Configurations;

public class PlanItemDialValueConfiguration : IEntityTypeConfiguration<PlanItemDialValue>
{
    public void Configure(EntityTypeBuilder<PlanItemDialValue> builder)
    {
        builder.ToTable("PlanItemDialValues");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.DialName)
            .IsRequired()
            .HasMaxLength(PlanItemDialValue.DialNameMaxLength);

        builder.Property(v => v.Value)
            .IsRequired()
            .HasMaxLength(PlanItemDialValue.ValueMaxLength);

        // Every read of a plan's values is by plan, in one query.
        builder.HasIndex(v => v.PlanId);

        // ItemId and StationItemId are deliberately plain columns, not foreign keys: a plan
        // save deletes and recreates every item, and a key would take these rows with it.
        // The uniqueness still has to hold per use, hence one filtered index for each side.
        builder.HasIndex(v => new { v.ItemId, v.DialName })
            .IsUnique()
            .HasFilter("\"ItemId\" IS NOT NULL");

        builder.HasIndex(v => new { v.StationItemId, v.DialName })
            .IsUnique()
            .HasFilter("\"StationItemId\" IS NOT NULL");

        // The plan is what these rows belong to, so deleting it takes them.
        builder.HasOne(v => v.Plan)
            .WithMany(p => p.DialValues)
            .HasForeignKey(v => v.PlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
