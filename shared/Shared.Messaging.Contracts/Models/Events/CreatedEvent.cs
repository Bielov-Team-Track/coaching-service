namespace Shared.Messaging.Contracts.Models.Events
{
    public class CreatedEvent
    {
        public Guid EventId { get; set; }
        public required string Name { get; set; }
        public string? Type { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }
}
