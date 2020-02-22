using Microsoft.AspNetCore.Http;
using System;

namespace Core.Exceptions
{
    public class DatabaseAccessException : ExceptionBase
    {
        public DatabaseAccessException(int code, string message, Exception exception = null, int statusCode = StatusCodes.Status503ServiceUnavailable) : base(code, message, exception, statusCode)
        {
        }
    }
}
