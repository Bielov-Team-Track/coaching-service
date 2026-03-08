using Coaching.Application.DTOs.Evaluation;
using Coaching.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Coaching.Hubs;

[Authorize]
public class EvaluationHub : Hub
{
    private readonly IEvaluationScoringService _scoringService;

    public EvaluationHub(IEvaluationScoringService scoringService)
    {
        _scoringService = scoringService;
    }

    public async Task JoinSession(Guid sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"session_{sessionId}");
        await Clients.Caller.SendAsync("JoinedSession", sessionId);
    }

    public async Task LeaveSession(Guid sessionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"session_{sessionId}");
    }

    public async Task SubmitScores(Guid sessionId, SubmitExerciseScoresDto dto)
    {
        var userId = GetUserId();
        var result = await _scoringService.SubmitExerciseScoresAsync(sessionId, dto, userId);

        await Clients.Group($"session_{sessionId}")
            .SendAsync("ScoresSubmitted", result);
    }

    private Guid GetUserId()
    {
        var claim = Context.User?.Claims.FirstOrDefault(c => c.Type == "userId");
        return claim != null ? Guid.Parse(claim.Value) : throw new HubException("User not authenticated");
    }
}
