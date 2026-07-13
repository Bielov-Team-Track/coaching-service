using Coaching.Application.DTOs.Templates;

namespace Coaching.Application.Interfaces.Services;

/// <summary>
/// Pushes a run state update to every device watching the run.
/// Implemented in the web layer over IHubContext&lt;TrainingRunHub&gt;.
/// </summary>
public interface IRunBroadcaster
{
    Task BroadcastRunUpdatedAsync(Guid eventId, RunDto run);
}
