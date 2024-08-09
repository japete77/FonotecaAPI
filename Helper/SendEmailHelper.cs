using Config.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Security.Authentication;
using System.Threading.Tasks;

namespace NuevaLuz.Fonoteca.Helper
{
    public class SendEmailHelper
    {
        public static async Task SendEmail(ISettings settings, MimeMessage message)
        {
            using (var smtpClient = new SmtpClient())
            {
                smtpClient.SslProtocols = SslProtocols.Tls12;

                await smtpClient.ConnectAsync(settings.AwsSmtpServer, settings.AwsSmtpPort, SecureSocketOptions.StartTlsWhenAvailable);

                await smtpClient.AuthenticateAsync(settings.AwsSmtpUser, settings.AwsSmtpPassword);

                await smtpClient.SendAsync(message);

                await smtpClient.DisconnectAsync(true);
            }
        }
    }
}
