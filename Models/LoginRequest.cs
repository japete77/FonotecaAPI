using Newtonsoft.Json;

namespace NuevaLuz.Fonoteca.Models
{
    public class LoginRequest
    {
        [JsonProperty("user")]
        public int User { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }
    }
}
