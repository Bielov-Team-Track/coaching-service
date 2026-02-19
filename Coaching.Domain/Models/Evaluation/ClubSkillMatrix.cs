using Coaching.Domain.Enums;
using Shared.Models;

namespace Coaching.Domain.Models.Evaluation;

/// <summary>
/// In-memory representation of a skill matrix fetched from clubs-service via gRPC.
/// Not persisted in coaching-service database.
/// </summary>
public class ClubSkillMatrix
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public List<MatrixSkill> Skills { get; set; } = new();
}

public class MatrixSkill
{
    public Guid Id { get; set; }
    public VolleyballSkill Skill { get; set; }

    public List<SkillBand> Bands { get; set; } = new();
}

public class SkillBand
{
    public Guid Id { get; set; }
    public int Order { get; set; }
    public decimal MinScore { get; set; }
    public decimal MaxScore { get; set; }
    public string Label { get; set; } = string.Empty;
}
