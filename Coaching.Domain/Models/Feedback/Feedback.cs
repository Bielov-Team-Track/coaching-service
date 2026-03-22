using Coaching.Domain.Models.Evaluation;
using Shared.Models;

namespace Coaching.Domain.Models.Feedback;

public class Feedback : BaseEntity
{
    public Guid RecipientUserId { get; set; }
    public Guid CoachUserId { get; set; }
    public Guid? EventId { get; set; }           // Cross-service reference, no FK
    public Guid? ClubId { get; set; }            // Club context for standalone feedback
    public Guid? EvaluationId { get; set; }      // Link to PlayerEvaluation (future use)
    public string? Content { get; set; }          // HTML rich text from Tiptap (sanitized)
    public string? ContentPlainText { get; set; } // Stripped text for search/preview
    public bool SharedWithPlayer { get; set; }

    // Phase A backward compat: Comment column kept in DB, mapped by EF.
    // Code writes to both Content and Comment to keep them in sync.
    // The Comment column will be dropped in Phase B (next release).
    public string? Comment { get; set; }

    public virtual PlayerEvaluation? Evaluation { get; set; }
    public virtual ICollection<ImprovementPoint> ImprovementPoints { get; set; } = new List<ImprovementPoint>();
    public virtual Praise? Praise { get; set; }
}
