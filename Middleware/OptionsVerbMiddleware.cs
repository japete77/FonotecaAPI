using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace NuevaLuz.Fonoteca.Middleware
{
    /// <summary>
    /// OPTIONS HTTP-method handler
    /// </summary>
    /// <remarks>
    /// See:     https://github.com/drwatson1/AspNet-Core-REST-Service/wiki#cross-origin-resource-sharing-cors-and-preflight-requests
    /// </remarks>
    public class OptionsVerbMiddleware
    {
        RequestDelegate _next { get; }

        public OptionsVerbMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Method == "OPTIONS")
            {
                context.Response.StatusCode = StatusCodes.Status200OK;
                context.Response.Headers.Add("Access-Control-Allow-Methods", "*");
                return Task.CompletedTask;
            }
            return _next.Invoke(context);
        }
    }
}
