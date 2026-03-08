using Coaching.Domain.Enums;

namespace Coaching.Application.DTOs.Evaluation;

public class EvaluationParticipantDto
{
    public Guid Id { get; set; }
    public Guid EvaluationSessionId { get; set; }
    public Guid PlayerId { get; set; }
    public ParticipantSource Source { get; set; }
    public PlayerEvaluationDto? Evaluation { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public record AddParticipantsDto
{
    public List<Guid> PlayerIds { get; set; } = new();
    public ParticipantSource Source { get; set; } = ParticipantSource.Manual;
}

public record RemoveParticipantDto
{
    public Guid PlayerId { get; set; }
}
