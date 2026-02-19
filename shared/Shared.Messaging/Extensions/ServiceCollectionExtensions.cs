using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Messaging.Configuration;

namespace Shared.Messaging.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMessaging<TDbContext>(
        this IServiceCollection services,
        Action<MessagingOptions> configure,
        Action<IBusRegistrationConfigurator>? configureBus = null)
        where TDbContext : DbContext
    {
        var options = new MessagingOptions();
        configure(options);

        services.AddMassTransit(x =>
        {
            configureBus?.Invoke(x);

            if (!string.IsNullOrEmpty(options.ServicePrefix))
            {
                x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter(options.ServicePrefix, false));
            }

            x.AddEntityFrameworkOutbox<TDbContext>(o =>
            {
                o.UsePostgres();
                o.UseBusOutbox();
                o.QueryDelay = TimeSpan.FromSeconds(1);
            });

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(options.Host, options.Port, options.VirtualHost, h =>
                {
                    h.Username(options.Username);
                    h.Password(options.Password);
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}