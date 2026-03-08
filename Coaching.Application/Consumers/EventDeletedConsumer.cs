using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Coaching.Application.Interfaces.Repositories;
using Coaching.Domain.Enums;
using Shared.Messaging.Contracts.Events.Events;

namespace Coaching.Application.Consumers;

public class EventDeletedConsumer : IConsumer<EventDeletedEvent>
{
    private readonly ITrainingPlanRepository _planRepository;
    private readonly ILogger<EventDeletedConsumer> _logger;

    public EventDeletedConsumer(ITrainingPlanRepository planRepository, ILogger<EventDeletedConsumer> logger)
    {
        _planRepository = planRepository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<EventDeletedEvent> context)
    {
        var eventId = context.Message.TargetEventId;

        var plan = await _planRepository.Query()
            .FirstOrDefaultAsync(p => p.EventId == eventId && p.PlanType == PlanType.Instance);

        if (plan != null)
        {
            _planRepository.Delete(plan);
            await _planRepository.SaveChangesAsync();
            _logger.LogInformation("Deleted orphaned training plan {PlanId} for deleted event {EventId}", plan.Id, eventId);
        }
    }
}
