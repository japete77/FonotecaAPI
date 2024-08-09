using NuevaLuz.Fonoteca.Models;
using System.Threading.Tasks;

namespace NuevaLuz.Fonoteca.Services.Notifications.Interfaces
{
    public interface INotificationsService
    {
        Task DeleteEndpoint(string endpoint);
        Task Unsubscribe(string endpoint);
        Task<string> CreateEndpoint(string deviceToken, string platform);
        Task<SNSSubscriptions> SynchSubscriptions(string session, string deviceToken, string platform, SNSSubscriptions notificationsSubscriptions);
    }
}
