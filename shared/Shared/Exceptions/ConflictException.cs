using Shared.Enums;

namespace Shared.Exceptions
{
    public class ConflictException : ExceptionWithStatusAndErrorCodes
    {
        public ConflictException(string message)
            : base(message, System.Net.HttpStatusCode.Conflict, ErrorCodeEnum.ValidationError)
        {
        }

        public ConflictException(string message, ErrorCodeEnum errorCode)
            : base(message, System.Net.HttpStatusCode.Conflict, errorCode)
        {
        }
    }
}
