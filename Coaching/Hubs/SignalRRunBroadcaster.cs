using Coaching.Application.DTOs.Templates;
using Coaching.Application.Interfaces.Services;
using Microsoft.AspNetCore.SignalR;

namespace Coaching.Hubs;

public class SignalRRunBroadcaster : IRunBroadcaster
{
    private readonly IHubContext<TrainingRunHub> _hubContext;

    public SignalRRunBroadcaster(IHubContext<TrainingRunHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task BroadcastRunUpdatedAsync(Guid eventId, RunDto run) =>
        _hubContext.Clients.Group(TrainingRunHub.GroupName(eventId)).SendAsync("RunUpdated", run);
}
