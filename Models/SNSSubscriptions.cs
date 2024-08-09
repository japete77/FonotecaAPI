using System.Collections.Generic;

namespace NuevaLuz.Fonoteca.Models
{
    public class SNSSubscriptions
    {
        public string DeviceToken { get; set; }
        public string ApplicationEndPoint { get; set; }
        public Dictionary<string, string> Subscriptions { get; set; }
    }
}
