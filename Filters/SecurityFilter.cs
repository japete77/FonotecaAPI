using Core.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using System;

namespace NuevaLuz.Fonoteca.Filters
{
    public class SecurityFilter : Attribute, IAuthorizationFilter
    {
        IHttpContextAccessor _httpContext { get; }

        public SecurityFilter(IHttpContextAccessor httpContext)
        {
            _httpContext = httpContext ?? throw new ArgumentNullException(nameof(httpContext));
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var unathorizedException = new ExceptionBase(ExceptionCodes.IDENTITY_NOT_AUTHORIZED, "User is not authorized", null, StatusCodes.Status403Forbidden);

            // Check if token is a valid one...

            if (false)
            {
                throw unathorizedException;
            }
        }
    }
}
