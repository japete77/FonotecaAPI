using System;

namespace NuevaLuz.Fonoteca.Models
{
    public class SubscriptionTitle
    {
        public int Id { get; set; }
        public DateTime PublishingDate { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
    }
}
