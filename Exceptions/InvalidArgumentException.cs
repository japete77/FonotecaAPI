using Microsoft.AspNetCore.Http;
using System;

namespace Core.Exceptions
{
    public class InvalidArgumentException : ExceptionBase
    {
        public InvalidArgumentException(int code, string message, Exception exception = null, int httpCode = StatusCodes.Status400BadRequest) : base(code, message, exception, httpCode)
        {
        }
    }
}
