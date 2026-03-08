using System.Text;
using Coaching.Application.DTOs.Export;
using Coaching.Application.Interfaces.Repositories;
using Coaching.Application.Interfaces.Services;
using Coaching.Domain.Enums;
using Shared.Enums;
using Shared.Exceptions;

namespace Coaching.Application.Services;

public class ExportService(
    IPlayerEvaluationRepository evaluationRepository,
    IEvaluationSessionRepository sessionRepository,
    IClubsGrpcClient clubsGrpcClient) : IExportService
{
    public async Task<ExportResult> ExportEvaluationsAsync(ExportEvaluationRequest request, Guid requestingUserId)
    {
        if (request.Format != ExportFormat.Csv)
            throw new BadRequestException("Only CSV format is supported", ErrorCodeEnum.ValidationError);

        var session = await sessionRepository.GetByIdWithParticipantsAsync(request.SessionId);
        if (session == null)
            throw new EntityNotFoundException("Evaluation session not found");

        if (session.CoachUserId != requestingUserId)
            throw new ForbiddenException("Only the session coach can export evaluations");

        var evaluations = await evaluationRepository.GetBySessionIdAsync(request.SessionId);
        var evaluationList = evaluations.ToList();

        var csv = GenerateEvaluationsCsv(evaluationList, request.IncludeMetricDetails, request.IncludeCoachNotes);
        var bytes = Encoding.UTF8.GetBytes(csv);

        return new ExportResult
        {
            Data = bytes,
            ContentType = "text/csv",
            FileName = $"evaluations_{request.SessionId}_{DateTime.UtcNow:yyyyMMdd}.csv"
        };
    }

    public async Task<ExportResult> ExportPlayerHistoryAsync(ExportPlayerHistoryRequest request, Guid requestingUserId)
    {
        if (request.Format != ExportFormat.Csv)
            throw new BadRequestException("Only CSV format is supported", ErrorCodeEnum.ValidationError);

        // Players can only export their own history
        if (request.PlayerId != requestingUserId)
            throw new ForbiddenException("You can only export your own evaluation history");

        var evaluations = await evaluationRepository.GetByPlayerIdAsync(request.PlayerId, 1, 1000);
        var evaluationList = evaluations
            .Where(e => e.SharedWithPlayer)
            .Where(e => !request.FromDate.HasValue || e.CreatedAt >= request.FromDate.Value)
            .Where(e => !request.ToDate.HasValue || e.CreatedAt <= request.ToDate.Value)
            .ToList();

        var csv = GeneratePlayerHistoryCsv(evaluationList);
        var bytes = Encoding.UTF8.GetBytes(csv);

        return new ExportResult
        {
            Data = bytes,
            ContentType = "text/csv",
            FileName = $"player_history_{request.PlayerId}_{DateTime.UtcNow:yyyyMMdd}.csv"
        };
    }

    public async Task<ExportResult> ExportSkillMatrixAsync(Guid skillMatrixId, ExportFormat format, Guid requestingUserId)
    {
        if (format != ExportFormat.Csv)
            throw new BadRequestException("Only CSV format is supported", ErrorCodeEnum.ValidationError);

        var matrixInfo = await clubsGrpcClient.GetSkillMatrixByIdAsync(skillMatrixId);
        if (matrixInfo == null)
            throw new EntityNotFoundException("Skill matrix not found or not accessible via gRPC");

        var csv = GenerateSkillMatrixCsv(matrixInfo);
        var bytes = Encoding.UTF8.GetBytes(csv);

        return new ExportResult
        {
            Data = bytes,
            ContentType = "text/csv",
            FileName = $"skill_matrix_{skillMatrixId}_{DateTime.UtcNow:yyyyMMdd}.csv"
        };
    }

    private static string GenerateEvaluationsCsv(
        List<Domain.Models.Evaluation.PlayerEvaluation> evaluations,
        bool includeMetricDetails,
        bool includeCoachNotes)
    {
        var csv = new StringBuilder();

        // Header
        var headers = new List<string>
        {
            "Player ID",
            "Outcome",
            "Evaluated By",
            "Evaluation Date"
        };

        if (includeMetricDetails)
        {
            var skills = Enum.GetValues<VolleyballSkill>();
            foreach (var skill in skills)
            {
                headers.Add($"{skill} Score");
                headers.Add($"{skill} Level");
            }
        }

        if (includeCoachNotes)
        {
            headers.Add("Coach Notes");
        }

        csv.AppendLine(string.Join(",", headers.Select(EscapeCsvField)));

        // Rows
        foreach (var evaluation in evaluations)
        {
            var row = new List<string>
            {
                evaluation.PlayerId.ToString(),
                evaluation.Outcome?.ToString() ?? "Pending",
                evaluation.EvaluatedByUserId.ToString(),
                evaluation.CreatedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A"
            };

            if (includeMetricDetails)
            {
                var skills = Enum.GetValues<VolleyballSkill>();
                foreach (var skill in skills)
                {
                    var skillScore = evaluation.SkillScores.FirstOrDefault(s => s.Skill == skill);
                    row.Add(skillScore?.Score.ToString("F2") ?? "N/A");
                    row.Add(skillScore?.Level ?? "N/A");
                }
            }

            if (includeCoachNotes)
            {
                row.Add(evaluation.CoachNotes ?? string.Empty);
            }

            csv.AppendLine(string.Join(",", row.Select(EscapeCsvField)));
        }

        return csv.ToString();
    }

    private static string GeneratePlayerHistoryCsv(List<Domain.Models.Evaluation.PlayerEvaluation> evaluations)
    {
        var csv = new StringBuilder();

        // Header
        csv.AppendLine("Session ID,Outcome,Evaluated By,Evaluation Date,Passing,Setting,Defending,Serving,Attacking,Blocking,Game");

        // Rows
        foreach (var evaluation in evaluations)
        {
            var row = new List<string>
            {
                evaluation.Participant.EvaluationSessionId.ToString(),
                evaluation.Outcome?.ToString() ?? "Pending",
                evaluation.EvaluatedByUserId.ToString(),
                evaluation.CreatedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A"
            };

            // Add skill scores in order
            var skills = new[]
            {
                VolleyballSkill.Passing,
                VolleyballSkill.Setting,
                VolleyballSkill.Defending,
                VolleyballSkill.Serving,
                VolleyballSkill.Attacking,
                VolleyballSkill.Blocking,
                VolleyballSkill.Game
            };

            foreach (var skill in skills)
            {
                var skillScore = evaluation.SkillScores.FirstOrDefault(s => s.Skill == skill);
                row.Add(skillScore?.Score.ToString("F2") ?? "N/A");
            }

            csv.AppendLine(string.Join(",", row.Select(EscapeCsvField)));
        }

        return csv.ToString();
    }

    private static string GenerateSkillMatrixCsv(SkillMatrixInfo matrix)
    {
        var csv = new StringBuilder();

        // Header
        csv.AppendLine($"Skill Matrix ID: {matrix.MatrixId}");
        csv.AppendLine();
        csv.AppendLine("Skill,Level,Min Score,Max Score");

        // Rows
        foreach (var skill in matrix.Skills.OrderBy(s => s.Name))
        {
            foreach (var band in skill.Bands.OrderBy(b => b.Order))
            {
                var row = new List<string>
                {
                    skill.Name,
                    band.Label,
                    band.MinScore.ToString("F2"),
                    band.MaxScore.ToString("F2")
                };

                csv.AppendLine(string.Join(",", row.Select(EscapeCsvField)));
            }
        }

        return csv.ToString();
    }

    private static string EscapeCsvField(string field)
    {
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        return field;
    }
}
