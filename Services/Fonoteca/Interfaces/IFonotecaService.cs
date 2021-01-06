using NuevaLuz.Fonoteca.Models;
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
        Task SendMessage(string title, string message, string type, int? id);
        Task CheckNotificationsAccess(string user, string password);
        Task<UserSubscriptions> GetUserSubscriptions(string session);
        Task<SubscriptionTitleResult> GetSuscriptionTitles(string session, string code);
        SuscriptionTitleLinkResult GetSuscriptionTitleLink(string session, string id);
    }
}
