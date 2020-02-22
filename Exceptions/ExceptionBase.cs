using Microsoft.AspNetCore.Http;
using System;
using System.Runtime.Serialization;

namespace Core.Exceptions
{
    public class ExceptionBase : Exception
    {
        public int Code { get; set; }
        public int HttpCode { get; set; }
        public string UserMessage { get; set; }

        public ExceptionBase(int code, string userMessage, Exception exception = null, int httpCode = StatusCodes.Status500InternalServerError) : base(exception?.Message, exception)
        {
            Code = code;
            HttpCode = httpCode;
            UserMessage = userMessage;
        }

        public ExceptionBase(int code, SerializationInfo info, StreamingContext context) : base(info, context)
        {
            Code = code;
        }
    }
}
