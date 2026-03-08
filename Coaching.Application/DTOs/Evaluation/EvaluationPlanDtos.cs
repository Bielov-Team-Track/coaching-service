namespace Coaching.Application.DTOs.Evaluation;

public class EvaluationPlanDto
{
    public Guid Id { get; set; }
    public Guid? ClubId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string? Name { get; set; }
    public string? Notes { get; set; }
    public List<EvaluationPlanItemDto> Items { get; set; } = new();
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class EvaluationPlanItemDto
{
    public Guid Id { get; set; }
    public Guid ExerciseId { get; set; }
    public int Order { get; set; }
    public EvaluationExerciseDto Exercise { get; set; } = null!;
}

public record CreateEvaluationPlanDto
{
    public Guid? ClubId { get; set; }
    public string? Name { get; set; }
    public string? Notes { get; set; }
    public List<Guid>? ExerciseIds { get; set; }
}

public record UpdateEvaluationPlanDto
{
    public string? Name { get; set; }
    public string? Notes { get; set; }
}

public record AddPlanItemDto
{
    public Guid ExerciseId { get; set; }
    public int? Order { get; set; }
}

public record ReorderPlanItemsDto
{
    public List<Guid> ItemIds { get; set; } = new();
}
