using Shared.Enums;

namespace Shared.Exceptions
{
    public class ForbiddenException : ExceptionWithStatusAndErrorCodes
    {
        public ForbiddenException(string message)
            : base(message, System.Net.HttpStatusCode.Forbidden, ErrorCodeEnum.Forbidden)
        {
        }
        public ForbiddenException(string message, string additionalInformation)
            : base(message, System.Net.HttpStatusCode.Forbidden, ErrorCodeEnum.Forbidden, additionalInformation)
        {
        }
    }
}
