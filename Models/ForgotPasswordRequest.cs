using Newtonsoft.Json;

namespace NuevaLuz.Fonoteca.Models
{
    public class ChangePasswordRequest
    {
        [JsonProperty("session")]
        public string Session { get; set; }

        [JsonProperty("newPassword")]
        public string NewPassword { get; set; }
    }
}
