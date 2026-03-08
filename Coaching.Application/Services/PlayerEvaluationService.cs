using AutoMapper;
using Coaching.Application.DTOs.Evaluation;
using Coaching.Application.Interfaces.Repositories;
using Coaching.Application.Interfaces.Services;
using Coaching.Domain.Enums;
using Coaching.Domain.Models.Evaluation;
using Shared.DataAccess.Repositories.Interfaces;
using Shared.Enums;
using Shared.Exceptions;

namespace Coaching.Application.Services;

public class PlayerEvaluationService(
    IPlayerEvaluationRepository evaluationRepository,
    IRepository<PlayerMetricScore> metricScoreRepository,
    IRepository<PlayerSkillScore> skillScoreRepository,
    IEvaluationPlanRepository planRepository,
    IEvaluationSessionRepository sessionRepository,
    IEvaluationParticipantRepository participantRepository,
    IClubsGrpcClient clubsGrpcClient,
    IScoreCalculationService scoreCalculationService,
    IMapper mapper) : IPlayerEvaluationService
{
    public async Task<PlayerEvaluationDto> CreateAsync(Guid sessionId, CreatePlayerEvaluationDto request, Guid coachUserId)
    {
        var session = await sessionRepository.GetByIdWithParticipantsAsync(sessionId);
        if (session == null)
            throw new EntityNotFoundException("Evaluation session not found");

        if (session.CoachUserId != coachUserId)
            throw new ForbiddenException("Only the session coach can create evaluations");

        // Find or create participant
        var participant = await participantRepository.GetBySessionAndPlayerAsync(sessionId, request.PlayerId);
        if (participant == null)
            throw new BadRequestException("Player is not a participant in this session. Add them first.", ErrorCodeEnum.ValidationError);

        // Check if evaluation already exists for this participant
        var existing = await evaluationRepository.GetByParticipantIdAsync(participant.Id);
        if (existing != null)
            throw new ConflictException("Player already has an evaluation in this session");

        var evaluation = mapper.Map<PlayerEvaluation>(request);
        evaluation.EvaluationParticipantId = participant.Id;
        evaluation.EvaluatedByUserId = coachUserId;
        evaluation.Outcome = EvaluationOutcome.Pending;

        evaluationRepository.Add(evaluation);
        await evaluationRepository.SaveChangesAsync();

        return await GetByIdAsync(evaluation.Id, coachUserId) ?? throw new Exception("Failed to retrieve created evaluation");
    }

    public async Task<PlayerEvaluationDto?> GetByIdAsync(Guid id, Guid requestingUserId)
    {
        var evaluation = await evaluationRepository.GetByIdWithScoresAsync(id);
        if (evaluation == null) return null;

        // Check access: coach or player
        var session = await sessionRepository.GetByIdAsync(evaluation.Participant.EvaluationSessionId);
        var isCoach = session != null && session.CoachUserId == requestingUserId;
        var isPlayer = evaluation.PlayerId == requestingUserId;

        if (!isCoach && !isPlayer)
            return null;

        // Player can only access if shared
        if (isPlayer && !isCoach && !evaluation.SharedWithPlayer)
            throw new ForbiddenException("This evaluation has not been shared with you");

        return mapper.Map<PlayerEvaluationDto>(evaluation);
    }

    public async Task<EvaluationSummaryDto> GetSessionSummaryAsync(Guid sessionId, Guid requestingUserId)
    {
        var session = await sessionRepository.GetByIdWithParticipantsAsync(sessionId);
        if (session == null)
            throw new EntityNotFoundException("Evaluation session not found");

        if (session.CoachUserId != requestingUserId)
            throw new ForbiddenException("Only the session coach can view evaluation summary");

        var evaluations = await evaluationRepository.GetBySessionIdAsync(sessionId);
        var evaluationList = evaluations.ToList();

        return new EvaluationSummaryDto
        {
            SessionId = sessionId,
            TotalPlayers = session.Participants.Count,
            EvaluatedCount = evaluationList.Count,
            PassedCount = evaluationList.Count(e => e.Outcome == EvaluationOutcome.Pass),
            FailedCount = evaluationList.Count(e => e.Outcome == EvaluationOutcome.Fail),
            PendingCount = evaluationList.Count(e => e.Outcome == EvaluationOutcome.Pending || e.Outcome == null),
            Evaluations = mapper.Map<List<PlayerEvaluationDto>>(evaluationList)
        };
    }

    public async Task<IEnumerable<PlayerEvaluationDto>> GetPlayerHistoryAsync(Guid playerId, int page = 1, int pageSize = 20)
    {
        var evaluations = await evaluationRepository.GetByPlayerIdAsync(playerId, page, pageSize);
        return mapper.Map<IEnumerable<PlayerEvaluationDto>>(evaluations);
    }

    public async Task<PlayerEvaluationDto> RecordMetricScoresAsync(Guid evaluationId, RecordMetricScoresDto request, Guid userId)
    {
        var evaluation = await evaluationRepository.GetByIdWithScoresAsync(evaluationId);
        if (evaluation == null)
            throw new EntityNotFoundException("Evaluation not found");

        var session = await sessionRepository.GetByIdAsync(evaluation.Participant.EvaluationSessionId);
        if (session == null || session.CoachUserId != userId)
            throw new ForbiddenException("Only the session coach can record scores");

        if (session.EvaluationPlanId == null)
            throw new BadRequestException("No evaluation plan is linked to this session", ErrorCodeEnum.ValidationError);

        var plan = await planRepository.GetByIdWithItemsAsync(session.EvaluationPlanId.Value);
        if (plan == null)
            throw new BadRequestException("Evaluation plan not found", ErrorCodeEnum.ValidationError);

        // Record metric scores
        foreach (var scoreDto in request.Scores)
        {
            var metric = plan.Items
                .SelectMany(i => i.Exercise.Metrics)
                .FirstOrDefault(m => m.Id == scoreDto.MetricId);

            if (metric == null) continue;

            var normalizedScore = scoreCalculationService.NormalizeMetricValue(metric, scoreDto.RawValue);

            var existingScore = evaluation.MetricScores.FirstOrDefault(s => s.MetricId == scoreDto.MetricId);
            if (existingScore != null)
            {
                existingScore.RawValue = scoreDto.RawValue;
                existingScore.NormalizedScore = normalizedScore;
                metricScoreRepository.Update(existingScore);
            }
            else
            {
                var newScore = new PlayerMetricScore
                {
                    EvaluationId = evaluationId,
                    MetricId = scoreDto.MetricId,
                    RawValue = scoreDto.RawValue,
                    NormalizedScore = normalizedScore
                };
                metricScoreRepository.Add(newScore);
            }
        }
        await metricScoreRepository.SaveChangesAsync();

        // Recalculate skill scores
        await RecalculateSkillScores(evaluationId, plan, session.ClubId);

        return await GetByIdAsync(evaluationId, userId) ?? throw new Exception("Failed to retrieve evaluation");
    }

    public async Task<PlayerEvaluationDto> UpdateOutcomeAsync(Guid evaluationId, UpdateEvaluationOutcomeDto request, Guid userId)
    {
        var evaluation = await evaluationRepository.GetByIdWithScoresAsync(evaluationId);
        if (evaluation == null)
            throw new EntityNotFoundException("Evaluation not found");

        var session = await sessionRepository.GetByIdAsync(evaluation.Participant.EvaluationSessionId);
        if (session == null || session.CoachUserId != userId)
            throw new ForbiddenException("Only the session coach can update outcomes");

        evaluation.Outcome = request.Outcome;
        if (request.CoachNotes != null) evaluation.CoachNotes = request.CoachNotes;

        evaluationRepository.Update(evaluation);
        await evaluationRepository.SaveChangesAsync();

        return await GetByIdAsync(evaluationId, userId) ?? throw new Exception("Failed to retrieve evaluation");
    }

    public async Task<PlayerEvaluationDto> ShareWithPlayerAsync(Guid evaluationId, bool share, Guid userId)
    {
        var evaluation = await evaluationRepository.GetByIdWithScoresAsync(evaluationId);
        if (evaluation == null)
            throw new EntityNotFoundException("Evaluation not found");

        var session = await sessionRepository.GetByIdAsync(evaluation.Participant.EvaluationSessionId);
        if (session == null || session.CoachUserId != userId)
            throw new ForbiddenException("Only the session coach can share evaluations");

        evaluation.SharedWithPlayer = share;
        evaluationRepository.Update(evaluation);
        await evaluationRepository.SaveChangesAsync();

        return await GetByIdAsync(evaluationId, userId) ?? throw new Exception("Failed to retrieve evaluation");
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var evaluation = await evaluationRepository.GetByIdWithScoresAsync(id);
        if (evaluation == null)
            throw new EntityNotFoundException("Evaluation not found");

        var session = await sessionRepository.GetByIdAsync(evaluation.Participant.EvaluationSessionId);
        if (session == null || session.CoachUserId != userId)
            throw new ForbiddenException("Only the session coach can delete evaluations");

        evaluation.IsDeleted = true;
        evaluationRepository.Update(evaluation);
        await evaluationRepository.SaveChangesAsync();
    }

    private async Task RecalculateSkillScores(Guid evaluationId, EvaluationPlan plan, Guid clubId)
    {
        var evaluation = await evaluationRepository.GetByIdWithScoresAsync(evaluationId);
        if (evaluation == null) return;

        // Calculate skill points
        var earnedPoints = scoreCalculationService.CalculateSkillPoints(evaluation, plan);
        var maxPoints = scoreCalculationService.CalculateMaxSkillPoints(plan);

        // Get club matrix for level lookup via gRPC from clubs-service
        ClubSkillMatrix? matrix = null;
        var matrixInfo = await clubsGrpcClient.GetDefaultSkillMatrixAsync(clubId);
        if (matrixInfo != null)
        {
            matrix = MapSkillMatrixInfoToDomain(matrixInfo);
        }

        // Delete existing skill scores
        foreach (var score in evaluation.SkillScores.ToList())
        {
            score.IsDeleted = true;
            skillScoreRepository.Update(score);
        }
        await skillScoreRepository.SaveChangesAsync();

        // Create new skill scores
        foreach (var skill in Enum.GetValues<VolleyballSkill>())
        {
            var earned = earnedPoints[skill];
            var max = maxPoints[skill];

            if (max <= 0) continue;

            var percentage = earned / max;
            var score = percentage * 10m;

            var skillScore = new PlayerSkillScore
            {
                EvaluationId = evaluationId,
                Skill = skill,
                EarnedPoints = earned,
                MaxPoints = max,
                Score = Math.Round(score, 2),
                Level = matrix != null ? scoreCalculationService.GetLevelForScore(score, skill, matrix) : null
            };

            skillScoreRepository.Add(skillScore);
        }
        await skillScoreRepository.SaveChangesAsync();
    }

    private static ClubSkillMatrix MapSkillMatrixInfoToDomain(SkillMatrixInfo info)
    {
        return new ClubSkillMatrix
        {
            Id = info.MatrixId,
            Name = "Remote Matrix",
            Skills = info.Skills.Select(s =>
            {
                if (!Enum.TryParse<VolleyballSkill>(s.SkillKey, out var skill))
                    skill = VolleyballSkill.Passing;

                return new MatrixSkill
                {
                    Id = s.Id,
                    Skill = skill,
                    Bands = s.Bands.Select(b => new SkillBand
                    {
                        Id = b.Id,
                        Order = b.Order,
                        Label = b.Label,
                        MinScore = b.MinScore,
                        MaxScore = b.MaxScore
                    }).ToList()
                };
            }).ToList()
        };
    }
}
