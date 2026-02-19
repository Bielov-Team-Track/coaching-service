using Shared.Enums;

namespace Shared.Exceptions
{
    public class EntityNotFoundException : ExceptionWithStatusAndErrorCodes
    {
        public EntityNotFoundException(string message) : base(message, System.Net.HttpStatusCode.NotFound, ErrorCodeEnum.EntityNotFound)
        {
        }
    }
}
