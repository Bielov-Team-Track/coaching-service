using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;
using Shared.Enums;
using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class BaseResponseWithErrorDetailsDto<TResult, TErrorDetails>
    where TResult : class
    where TErrorDetails : class
{
    [JsonProperty("result")]
    public TResult Result { get; set; }

    [JsonProperty("errorDetails")]
    public TErrorDetails? ErrorDetails { get; set; }

    [JsonProperty("responseCode")]
    public ErrorCodeEnum ResponseCode { get; set; }

    [JsonProperty("errorMessage")]
    public string ErrorMessage { get; set; }

    public BaseResponseWithErrorDetailsDto(TResult result, TErrorDetails errorDetails = null)
    {
        Result = result;
        ErrorDetails = errorDetails;
    }

    public BaseResponseWithErrorDetailsDto()
    {
    }

    public BaseResponseWithErrorDetailsDto(ErrorCodeEnum responseCode, string errorMessage, TErrorDetails? errorDetails = null)
    {
        ResponseCode = responseCode;
        ErrorMessage = errorMessage;
        ErrorDetails = errorDetails;
    }

    public BaseResponseWithErrorDetailsDto(ErrorCodeEnum responseCode, ModelStateDictionary modelState)
    {
        var errors = modelState.Keys
            .SelectMany(key => modelState[key].Errors.ToDictionary(_ => key, x => x.ErrorMessage))
            .ToDictionary(x => x.Key, x => x.Value);

        var errorMessage = string.Join(", ", errors.Select(kv => kv.Key + " = " + kv.Value).ToArray());

        ResponseCode = responseCode;
        ErrorMessage = errorMessage;
    }

    public BaseResponseWithErrorDetailsDto(TResult result, ErrorCodeEnum responseCode, string errorMessage, TErrorDetails errorDetails = null)
    {
        Result = result;
        ErrorDetails = errorDetails;
        ResponseCode = responseCode;
        ErrorMessage = errorMessage;
    }
}