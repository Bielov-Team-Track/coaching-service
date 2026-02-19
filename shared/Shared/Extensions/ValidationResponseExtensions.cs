using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Shared.DTOs.Errors;

namespace Shared.Extensions;

public static class ValidationResponseExtensions
{
    /// <summary>
    /// Configures ASP.NET Core to return RFC 9457 Problem Details for model validation errors.
    /// </summary>
    public static IServiceCollection ConfigureProblemDetailsValidation(this IServiceCollection services)
    {
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var fieldErrors = context.ModelState
                    .Where(kvp => kvp.Value?.Errors.Count > 0)
                    .SelectMany(kvp => kvp.Value!.Errors.Select(error => new FieldError
                    {
                        Field = ToCamelCase(kvp.Key),
                        Code = DetermineErrorCode(error),
                        Message = error.ErrorMessage ?? "Invalid value"
                    }))
                    .ToList();

                var response = ProblemDetailsResponse.ValidationError(fieldErrors);

                return new BadRequestObjectResult(response)
                {
                    ContentTypes = { "application/problem+json" }
                };
            };
        });

        return services;
    }

    private static string ToCamelCase(string key)
    {
        if (string.IsNullOrEmpty(key)) return key;

        // Handle nested properties like "Address.City" -> "address.city"
        var parts = key.Split('.');
        return string.Join(".", parts.Select(part =>
            string.IsNullOrEmpty(part) ? part : char.ToLowerInvariant(part[0]) + part[1..]
        ));
    }

    private static string DetermineErrorCode(Microsoft.AspNetCore.Mvc.ModelBinding.ModelError error)
    {
        var message = error.ErrorMessage?.ToLowerInvariant() ?? "";

        return message switch
        {
            _ when message.Contains("required") => "REQUIRED",
            _ when message.Contains("email") => "INVALID_EMAIL",
            _ when message.Contains("length") || message.Contains("characters") => "INVALID_LENGTH",
            _ when message.Contains("range") || message.Contains("between") => "OUT_OF_RANGE",
            _ when message.Contains("format") => "INVALID_FORMAT",
            _ when message.Contains("type") || message.Contains("convert") => "INVALID_TYPE",
            _ => "INVALID_VALUE"
        };
    }
}
