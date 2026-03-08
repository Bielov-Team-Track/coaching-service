using Shared.Models;

namespace Coaching.Domain.Models.Drills;

public class DrillEquipment : BaseEntity
{
    public Guid DrillId { get; set; }
    public required string Name { get; set; }
    public bool IsOptional { get; set; }
    public int Order { get; set; }

    // Navigation
    public virtual Drill? Drill { get; set; }
}
