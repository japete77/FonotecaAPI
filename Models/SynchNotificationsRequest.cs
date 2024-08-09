namespace NuevaLuz.Fonoteca.Models
{
    public class SynchNotificationsRequest
    {
        public string Session { get; set; }
        public string DeviceToken { get; set; }
        public string Platform { get; set; }
        public SNSSubscriptions Subscriptions { get; set; }
    }
}
