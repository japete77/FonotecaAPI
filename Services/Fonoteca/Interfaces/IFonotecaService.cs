using NuevaLuz.Fonoteca.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NuevaLuz.Fonoteca.Services.Fonoteca.Interfaces
{
    public interface IFonotecaService
    {
        Task<LoginResult> Login(int user, string password);
        Task ChangePassword(string session, string password);
        Task ForgotPassword(string email);
        Task<TitleResult> GetBooksByTitle(int index, int count);
        Task<TitleResult> GetRecentBooks(int index, int count);
        Task<AuthorsResult> GetAuthors(int index, int count);
        Task<TitleResult> GetBooksByAuthor(string author, int index, int count);
        AudioBookLinkResult GetAudioBookLink(string session, string id);
        Task IncreaseTitleDownloadCounter(string session, string id);
        Task<AudioBookDetailResult> GetBookDetail(string id);
        Task CheckSession(string session);
        Task SendMessage(string notification, string title, string message, int id_suscription, int? id_material);
        Task CheckNotificationsAccess(string user, string password);
        Task<UserSubscriptions> GetUserSubscriptions(string session, bool onlyAppSubscriptions);
        Task<SubscriptionTitleResult> GetSuscriptionTitles(string session, string code);
        Task IncreaseSuscriptionTitleDownloadCounter(string session, string id, int app = 1);
        SuscriptionTitleLinkResult GetSuscriptionTitleLink(string session, string id, int app = 1);
        Task<NotificationsResult> GetUserNotifications(string session);
        Task<List<int>> GetUserNotificationsIds(string session);
    }
}
