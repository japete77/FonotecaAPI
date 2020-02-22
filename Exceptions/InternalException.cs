using Microsoft.AspNetCore.Http;
using System;

namespace Core.Exceptions
{
    public class InternalException : ExceptionBase
    {
        public InternalException(int code, string message, Exception exception = null, int statusCode = StatusCodes.Status500InternalServerError) : base(code, message, exception, statusCode)
        {
        }
    }
}
