using Coaching.Domain.Enums;

namespace Coaching.Application.DTOs.Templates;

public class RunDto
{
    public Guid Id { get; set; }
    public Guid PlanId { get; set; }
    public Guid EventId { get; set; }
    public Guid StartedByUserId { get; set; }
    public RunStatus Status { get; set; }

    public Guid? CurrentItemId { get; set; }

    // Virtual start of the current item's timer; set while Running.
    public DateTime? CurrentItemStartedAt { get; set; }
    public int CurrentItemPausedElapsedSeconds { get; set; }

    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Server "now" so each client computes a clock offset.
    public DateTime ServerTime { get; set; }

    // True when the requesting user is the plan creator (may control the run).
    public bool CanControl { get; set; }

    public List<RunItemDto> Items { get; set; } = new();
}

public class RunItemDto
{
    public Guid Id { get; set; }
    public Guid PlanItemId { get; set; }
    public Guid DrillId { get; set; }
    public int Order { get; set; }
    public int PlannedDurationSeconds { get; set; }
    public int ActualElapsedSeconds { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

// Body for POST .../run/advance — guards against double-tap / concurrent advance.
public record AdvanceRunDto(Guid FromItemId);
