using NuevaLuz.Fonoteca.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NuevaLuz.Fonoteca.Services.Fonoteca.Interfaces
{
    public interface IFonotecaService
    {
        Task<string> Login(int user, string password);
        Task ChangePassword(string session, string password);
        Task<TitleResult> GetBooksByTitle(int index, int count);
        Task<AuthorsResult> GetAuthors(int index, int count);
        Task<TitleResult> GetBooksByAuthor(string author, int index, int count);
        Task<AudioBookLinkResult> GetAudioBookLink(string session, string id);
        Task<AudioBookDetailResult> GetBookDetail(string id);
        Task CheckSession(string session);
    }
}
