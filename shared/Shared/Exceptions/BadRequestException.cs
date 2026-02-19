using Shared.Enums;
using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace Shared.Exceptions
{
    [Serializable]
    [ExcludeFromCodeCoverage]
    public class BadRequestException : ExceptionWithStatusAndErrorCodes
    {
        public BadRequestException(string message, ErrorCodeEnum errorCode)
            : base(message, HttpStatusCode.BadRequest, errorCode)
        {
        }

        public BadRequestException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
