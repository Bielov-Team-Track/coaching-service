using Microsoft.Extensions.DependencyInjection;
using Shared.DataAccess.Providers;
using Shared.DataAccess.Providers.Interfaces;

namespace Shared.DataAccess.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSharedDataAccess(this IServiceCollection services)
        {

            services.AddScoped<IJwtPayloadProvider, JwtPayloadProvider>();

            return services;
        }
    }
}
