namespace NuevaLuz.Fonoteca.Models
{
    public class LoginResult
    {
        public string Message { get; set; }
        public string Session { get; set; }
        public bool Success { get; set; }
        public bool MustBeChanged { get; set; }
    }
}
