using Config.Interfaces;
using Core.Exceptions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuevaLuz.Fonoteca.Middleware
{
    /// <summary>
    /// Middleware to handle exceptions.
    /// It separates exceptions based on their type and returns different status codes and answers based on it, instead of 500 Internal Server Error code in all cases.
    /// In addition, it writes them in the log.
    /// </summary>
    /// <remarks>
    /// There is another way to do this - an exception filter.
    /// However, a middleware is a preferred way to achieve this according to the official documentation.
    /// To learn more see https://docs.microsoft.com/en-us/aspnet/core/mvc/controllers/filters?view=aspnetcore-2.1#exception-filters
    /// 
    /// See also: https://github.com/drwatson1/AspNet-Core-REST-Service/wiki#unhandled-exceptions-handling
    /// </remarks>
    public class ExceptionMiddleware
    {
        RequestDelegate _next { get; }
        ILogger _logger { get; }
        IWebHostEnvironment _environment { get; }

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IWebHostEnvironment environment)
        {
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var body = context.Response.Body;
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                // If context.Response.HasStarted == true, then we can't write to the response stream anymore. So we have to restore the body.
                // If we don't do that we get an exception.
                context.Response.Body = body;
                await HandleExceptionAsync(context, ex);
            }
        }

        async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            // default exception details
            int statusCode = StatusCodes.Status500InternalServerError;
            int code = ExceptionCodes.UNHANDLED_EXCEPTION;
            string userMessage = null;
            Exception innerException = ex;

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;

            // We can decide what the status code should return
            if (ex is ExceptionBase baseException)
            {
                code = baseException.Code;
                statusCode = baseException.HttpCode;
                userMessage = baseException.UserMessage;
                innerException = new Exception(ex.Message, ex.InnerException);
            }
            else if (ex is AuthenticationException)
            {
                statusCode = StatusCodes.Status401Unauthorized;
                userMessage = "Invalid user name or password";
            }
            else if (ex is KeyNotFoundException)
            {
                statusCode = StatusCodes.Status404NotFound;
            }

            context.Response.StatusCode = statusCode;

            var outputException = new ErrorResponse(
                code,
                userMessage,
                new ExceptionDetails
                {
                    ConnectionId = GetExceptionFieldValue(innerException, "ConnectionId"),
                    InnerException = GetExceptionFieldValue(innerException, "InnerException"),
                    Message = GetExceptionFieldValue(innerException, "Message"),
                    Source = GetExceptionFieldValue(innerException, "Source"),
                    StackTrace = GetExceptionFieldValue(innerException, "StackTrace"),
                    TargetSite = GetExceptionFieldValue(innerException, "TargetSite"),
                    WriteError = GetExceptionFieldValue(innerException, "WriteError"),
                    HResult = GetExceptionFieldValue(innerException, "HResult")
                },
                _environment.IsDevelopment());

            var exception = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(outputException));

            await context.Response.Body.WriteAsync(exception);

            _logger.LogError(ex, $"Exception Code: {code}, {userMessage}");
        }

        private string GetExceptionFieldValue(Exception exception, string propertyName)
        {
            var properties = exception.GetType().GetProperties();

            var fieldValue = properties
                             .Where(w => w.Name == propertyName)
                             .Select(property => property.GetValue(exception, null))
                             .Select(x => x != null ? x.ToString() : String.Empty)
                             .FirstOrDefault();

            return fieldValue;
        }
    }
}
