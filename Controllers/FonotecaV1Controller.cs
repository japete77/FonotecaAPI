using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Belsize.Controllers
{
    [Route("/api/v1/fonoteca/")]
    [ApiController]
    [Produces("application/json")]
    public class FonotecaV1Controller : ControllerBase
    {
        /// <summary>
        /// Log in to the application
        /// </summary>
        [Route("login")]
        [HttpPost]
        //[ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public void Login()
        {

        }
    }
}
