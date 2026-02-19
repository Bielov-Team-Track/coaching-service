using Amazon.S3;
using Amazon.SimpleEmailV2;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Services.FileStorage;
using Shared.Services.FileStorage.Intefaces;
using Shared.Services.Interfaces;
using Shared.Services.Services;
using Shared.Services.Services.Interfaces;

namespace Shared.Services.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSharedServices(this IServiceCollection services)
    {
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IHashingService, HashingService>();
        services.AddScoped<IFileService, S3FileService>();
        services.AddAWSService<IAmazonSimpleEmailServiceV2>();
        services.AddAWSService<IAmazonS3>();

        return services;
    }
}