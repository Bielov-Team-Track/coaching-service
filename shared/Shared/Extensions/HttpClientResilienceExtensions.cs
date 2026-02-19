using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using System.Net;

namespace Shared.Extensions;

/// <summary>
/// Extension methods for adding resilient HTTP client configurations.
/// </summary>
public static class HttpClientResilienceExtensions
{
    /// <summary>
    /// Adds a resilient HTTP client with retry, circuit breaker, and timeout policies.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="name">The name of the HTTP client.</param>
    /// <param name="baseAddress">The base address for the HTTP client.</param>
    /// <param name="configureResilience">Optional action to configure resilience options.</param>
    /// <returns>The IHttpClientBuilder for further configuration.</returns>
    public static IHttpClientBuilder AddResilientHttpClient(
        this IServiceCollection services,
        string name,
        string baseAddress,
        Action<HttpStandardResilienceOptions>? configureResilience = null)
    {
        var builder = services.AddHttpClient(name, client =>
        {
            client.BaseAddress = new Uri(baseAddress);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        builder.AddStandardResilienceHandler(options =>
        {
            // Configure retry policy
            options.Retry.MaxRetryAttempts = 3;
            options.Retry.Delay = TimeSpan.FromMilliseconds(500);
            options.Retry.UseJitter = true;
            options.Retry.ShouldHandle = args => ValueTask.FromResult(
                args.Outcome.Exception is not null ||
                args.Outcome.Result?.StatusCode is HttpStatusCode.RequestTimeout or
                    HttpStatusCode.ServiceUnavailable or
                    HttpStatusCode.TooManyRequests or
                    HttpStatusCode.GatewayTimeout or
                    HttpStatusCode.BadGateway);

            // Configure circuit breaker
            options.CircuitBreaker.FailureRatio = 0.5;
            options.CircuitBreaker.MinimumThroughput = 10;
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
            options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);

            // Configure timeout
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);

            // Apply custom configuration if provided
            configureResilience?.Invoke(options);
        });

        return builder;
    }

    /// <summary>
    /// Adds a resilient HTTP client for internal service-to-service communication.
    /// Uses shorter timeouts and more aggressive retry policies suitable for internal calls.
    /// </summary>
    public static IHttpClientBuilder AddInternalServiceClient(
        this IServiceCollection services,
        string name,
        string baseAddress)
    {
        var builder = services.AddHttpClient(name, client =>
        {
            client.BaseAddress = new Uri(baseAddress);
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        builder.AddStandardResilienceHandler(options =>
        {
            // Internal services should be fast - use shorter timeouts
            options.Retry.MaxRetryAttempts = 2;
            options.Retry.Delay = TimeSpan.FromMilliseconds(200);
            options.Retry.UseJitter = true;

            // Circuit breaker for internal services
            options.CircuitBreaker.FailureRatio = 0.3;
            options.CircuitBreaker.MinimumThroughput = 5;
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(20);
            options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);

            // Shorter timeouts for internal calls
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(15);
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(5);
        });

        return builder;
    }
}
