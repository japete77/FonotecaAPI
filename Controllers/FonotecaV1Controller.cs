using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NuevaLuz.Fonoteca.Models;
using NuevaLuz.Fonoteca.Services.Fonoteca.Interfaces;
using System;
using System.Threading.Tasks;

namespace Belsize.Controllers
{
    [Route("/api/v1/fonoteca/")]
    [ApiController]
    [Produces("application/json")]
    public class FonotecaV1Controller : ControllerBase
    {
        IFonotecaService _fonotecaService { get; }

        public FonotecaV1Controller(IFonotecaService fonotecaService)
        {
            _fonotecaService = fonotecaService;
        }

        /// <summary>
        /// Log in to the application
        /// </summary>
        [Route("login")]
        [HttpPost]
        public async Task<LoginResult> Login(LoginRequest request)
        {
            LoginResult result = new LoginResult();

            try
            {
                result.Session = await _fonotecaService.Login(request.User, request.Password);
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
            }

            return result;
        }

        [Route("change-password")]
        [HttpPost]
        public async Task ChangePassword(ChangePasswordRequest request)
        {
            await _fonotecaService.CheckSession(request.Session);

            await _fonotecaService.ChangePassword(request.Session, request.NewPassword);
        }

        [Route("titles")]
        [HttpGet]
        public async Task<TitleResult> GetTitles(string session, int index, int count)
        {
            // Check security
            await _fonotecaService.CheckSession(session);

            return await _fonotecaService.GetBooksByTitle(index, count);
        }

        [Route("titles/author/{author}")]
        [HttpGet]
        public async Task<TitleResult> GetAuthors(string session, string author, int index, int count)
        {
            // Check security
            await _fonotecaService.CheckSession(session);

            return await _fonotecaService.GetBooksByAuthor(author, index, count);
        }

        [Route("authors")]
        [HttpGet]
        public async Task<AuthorsResult> GetAuthors(string session, int index, int count)
        {
            // Check security
            await _fonotecaService.CheckSession(session);

            return await _fonotecaService.GetAuthors(index, count);
        }

        [Route("title/{id}")]
        [HttpGet]
        public async Task<AudioBookDetailResult> GetDetails(string session, string id)
        {
            // Check security
            await _fonotecaService.CheckSession(session);

            return await _fonotecaService.GetBookDetail(id);
        }

        [Route("title/{id}/link")]
        [HttpGet]
        public async Task<AudioBookLinkResult> GetTitleLink(string session, string id)
        {
            // Check security
            await _fonotecaService.CheckSession(session);

            return await _fonotecaService.GetAudioBookLink(session, id);
        }
    }
}
