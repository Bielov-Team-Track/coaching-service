using Shared.Enums;
using System.Net;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace Shared.Exceptions
{
    public class ExceptionWithStatusAndErrorCodes : Exception
    {
        public HttpStatusCode StatusCode { get; set; }

        public ErrorCodeEnum ErrorCode { get; set; }

        /// <summary>
        /// Temporary field to highlight if custom error is already logged in
        /// or requires to be logged on global error handler level.
        /// This field should be removed when all necessary occurrences of custom errors have necessary logs
        /// then, global error handler can rely on type of error to identify if it needs to be logged or not
        /// rather than logging all errors, causing duplications of errors or checking IsLogged field.
        /// </summary>
        [JsonIgnore]
        [XmlIgnore]
        public bool IsLogged { get; set; }

        [JsonIgnore]
        [XmlIgnore]
        public string? AdditionalInformation { get; set; }

        public object? ErrorDetails { get; set; }

        public ExceptionWithStatusAndErrorCodes(string message, HttpStatusCode statusCode, ErrorCodeEnum errorCode)
            : base(message)
        {
            StatusCode = statusCode;
            ErrorCode = errorCode;
        }
        public ExceptionWithStatusAndErrorCodes(string message, HttpStatusCode statusCode, ErrorCodeEnum errorCode, string additionalInformation)
            : base(message)
        {
            StatusCode = statusCode;
            ErrorCode = errorCode;
            AdditionalInformation = additionalInformation;
        }

        public ExceptionWithStatusAndErrorCodes(string message, Exception innerException, HttpStatusCode statusCode, ErrorCodeEnum errorCode)
            : base(message, innerException)
        {
            StatusCode = statusCode;
            ErrorCode = errorCode;
        }

        public ExceptionWithStatusAndErrorCodes(string message)
            : base(message)
        {
        }
        public ExceptionWithStatusAndErrorCodes(string message, string additionalInformation)
            : base(message)
        {
            AdditionalInformation = additionalInformation;
        }

        public ExceptionWithStatusAndErrorCodes()
            : base()
        {
        }

        public ExceptionWithStatusAndErrorCodes(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected ExceptionWithStatusAndErrorCodes(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            StatusCode = (HttpStatusCode)info.GetValue(nameof(StatusCode), typeof(HttpStatusCode));
            ErrorCode = (ErrorCodeEnum)(info.GetValue(nameof(ErrorCode), typeof(ErrorCodeEnum)));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            info.AddValue(nameof(StatusCode), StatusCode, typeof(HttpStatusCode));
            info.AddValue(nameof(ErrorCode), ErrorCode, typeof(ErrorCodeEnum));
        }
    }
}
