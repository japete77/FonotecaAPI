namespace NuevaLuz.Fonoteca.Models
{
    public class PublishMessageRequest
    {
        public string User { get; set; }

        public string Password { get; set; }

        public string Notification { get; set; }

        public string Title { get; set; }

        public string Message { get; set; }

        public int SubscriptionId { get; set; }

        public int? MaterialId { get; set; }
    }
}
