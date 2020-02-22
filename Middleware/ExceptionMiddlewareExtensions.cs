using Microsoft.AspNetCore.Builder;

namespace NuevaLuz.Fonoteca.Middleware
{
    public static class ExceptionMiddlewareExtensions
    {
        public static IApplicationBuilder UseFonotecaExceptionHandler(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ExceptionMiddleware>();
        }
    }
}
