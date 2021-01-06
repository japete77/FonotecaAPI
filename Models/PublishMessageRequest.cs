using Newtonsoft.Json;

namespace NuevaLuz.Fonoteca.Models
{
    public class PublishMessageRequest
    {
        [JsonProperty("user")]
        public string User { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("id")]
        public int? Id { get; set; }
    }
}
