using Newtonsoft.Json;

namespace NuevaLuz.Fonoteca.Models
{
    public class ForgotPasswordRequest
    {
        [JsonProperty("email")]
        public string Email { get; set; }
    }
}
