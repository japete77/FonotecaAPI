namespace NuevaLuz.Fonoteca.Middleware
{
    public class ExceptionDescription
    {
        public int Code { get; set; }
        public string UserMessage { get; set; }
        public ExceptionDetails Details { get; set; }
    }
}
