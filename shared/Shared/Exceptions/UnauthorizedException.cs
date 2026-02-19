using Shared.Enums;
using System.Net;

namespace Shared.Exceptions
{
    public class UnauthorizedException : ExceptionWithStatusAndErrorCodes
    {
        public UnauthorizedException(string message, ErrorCodeEnum errorCode)
    : base(message, HttpStatusCode.Unauthorized, errorCode)
        {
        }

        public UnauthorizedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
