using Shared.Enums;
using Shared.Enums.Social;

namespace Shared.Messaging.Contracts.Events.Social.Mentions;

public class EveryoneMentionedEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public Guid SourceId { get; init; }
    public MentionSourceType SourceType { get; init; }

    public ContextType ContextType { get; init; }
    public Guid ContextId { get; set; }
}
