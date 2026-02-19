using Coaching.Application.Interfaces.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Shared.Contracts.Grpc;

namespace Coaching.Infrastructure.Services;

/// <summary>
/// gRPC client for events-service authorization checks with in-memory caching.
/// Participant status is cached for 5 minutes to reduce cross-service calls.
/// </summary>
public class EventsGrpcClient : IEventsGrpcClient
{
    private readonly EventsInternalService.EventsInternalServiceClient _grpcClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<EventsGrpcClient> _logger;

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private const string ParticipantCacheKeyPrefix = "event_participant_";

    public EventsGrpcClient(
        EventsInternalService.EventsInternalServiceClient grpcClient,
        IMemoryCache cache,
        ILogger<EventsGrpcClient> logger)
    {
        _grpcClient = grpcClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<bool> IsEventAdminAsync(Guid eventId, Guid userId)
    {
        try
        {
            var response = await _grpcClient.IsEventAdminAsync(new IsEventAdminRequest
            {
                EventId = eventId.ToString(),
                UserId = userId.ToString()
            });
            return response.IsAdmin;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check event admin status via gRPC for event {EventId}, user {UserId}",
                eventId, userId);
            throw;
        }
    }

    public async Task<(bool IsParticipant, bool EventExists)> IsEventParticipantAsync(Guid eventId, Guid userId)
    {
        var cacheKey = $"{ParticipantCacheKeyPrefix}{eventId}_{userId}";

        if (_cache.TryGetValue(cacheKey, out (bool IsParticipant, bool EventExists) cached))
            return cached;

        try
        {
            var response = await _grpcClient.IsEventParticipantAsync(new IsEventParticipantRequest
            {
                EventId = eventId.ToString(),
                UserId = userId.ToString()
            });

            var result = (response.IsParticipant, response.EventExists);
            _cache.Set(cacheKey, result, CacheDuration);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check event participant status via gRPC for event {EventId}, user {UserId}",
                eventId, userId);
            throw;
        }
    }

    public async Task<EventContext?> GetEventContextAsync(Guid eventId)
    {
        var cacheKey = $"event_context_{eventId}";

        if (_cache.TryGetValue(cacheKey, out EventContext? cached))
            return cached;

        try
        {
            var response = await _grpcClient.GetEventContextAsync(new GetEventContextRequest
            {
                EventId = eventId.ToString()
            });

            if (!response.Found)
                return null;

            Guid? contextId = Guid.TryParse(response.ContextId, out var parsed) ? parsed : null;
            var result = new EventContext(response.EventType, response.ContextType, contextId);

            _cache.Set(cacheKey, result, CacheDuration);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get event context via gRPC for event {EventId}", eventId);
            throw;
        }
    }
}
