using System.Diagnostics.CodeAnalysis;
using System.Net;
using Shared.Enums;

namespace Shared.Exceptions;

[ExcludeFromCodeCoverage]
public class ExceptionWithResult<T> : ExceptionWithStatusAndErrorCodes
{
    public T Result { get; }

    public ExceptionWithResult(string message)
    : base(message)
    {
    }

    public ExceptionWithResult(string message, ErrorCodeEnum errorCode, T result)
    : base(message, HttpStatusCode.BadRequest, errorCode)
    {
        Result = result;
    }

    public ExceptionWithResult(string message, HttpStatusCode statusCode, ErrorCodeEnum errorCode, T result)
    : base(message, statusCode, errorCode)
    {
        Result = result;
    }
}