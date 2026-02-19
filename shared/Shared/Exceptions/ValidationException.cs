using System.Net;
using Shared.DTOs.Errors;
using Shared.Enums;

namespace Shared.Exceptions;

/// <summary>
/// Exception for validation errors with field-level details.
/// </summary>
[Serializable]
public class ValidationException : ExceptionWithStatusAndErrorCodes
{
    public List<FieldError> FieldErrors { get; }

    public ValidationException(string message, List<FieldError> fieldErrors)
        : base(message, HttpStatusCode.BadRequest, ErrorCodeEnum.ValidationError)
    {
        FieldErrors = fieldErrors;
    }

    public ValidationException(string field, string code, string message)
        : base(message, HttpStatusCode.BadRequest, ErrorCodeEnum.ValidationError)
    {
        FieldErrors = new List<FieldError>
        {
            new FieldError(field, code, message)
        };
    }

    public ValidationException(List<(string Field, string Code, string Message)> errors)
        : base("One or more validation errors occurred", HttpStatusCode.BadRequest, ErrorCodeEnum.ValidationError)
    {
        FieldErrors = errors.Select(e => new FieldError(e.Field, e.Code, e.Message)).ToList();
    }
}
