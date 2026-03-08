using Coaching.Domain.Enums;
using Shared.Models;

namespace Coaching.Domain.Models.Drills;

public class DrillAttachment : BaseEntity
{
    public Guid DrillId { get; set; }

    public required string FileName { get; set; }
    public required string FileUrl { get; set; }
    public DrillAttachmentType FileType { get; set; }
    public long FileSize { get; set; } // in bytes

    public int Order { get; set; } // for display ordering

    // Navigation properties
    public virtual Drill Drill { get; set; } = null!;
}
