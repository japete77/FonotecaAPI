namespace NuevaLuz.Fonoteca.Middleware
{
    public class ExceptionDetails
    {
        public string Message { get; set; }
        public string InnerException { get; set; }
        public string WriteError { get; set; }
        public string HResult { get; set; }
        public string TargetSite { get; set; }
        public string StackTrace { get; set; }
        public string ConnectionId { get; set; }
        public string Source { get; set; }
    }

}
