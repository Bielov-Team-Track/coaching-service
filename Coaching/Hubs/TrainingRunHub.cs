using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Coaching.Hubs;

[Authorize]
public class TrainingRunHub : Hub
{
    public async Task JoinRun(Guid eventId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(eventId));
        await Clients.Caller.SendAsync("JoinedRun", eventId);
    }

    public async Task LeaveRun(Guid eventId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(eventId));
    }

    public static string GroupName(Guid eventId) => $"run_{eventId}";

    private Guid GetUserId()
    {
        var userIdClaim = Context.User?.Claims.FirstOrDefault(c => c.Type == "userId")?.Value
            ?? Context.User?.Claims.FirstOrDefault(c => c.Type == "nameid")?.Value
            ?? Context.User?.Claims.FirstOrDefault(c => c.Type.Contains("nameidentifier"))?.Value;

        if (!Guid.TryParse(userIdClaim, out var userId))
            throw new HubException("Unauthorized");

        return userId;
    }
}
