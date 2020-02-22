namespace NuevaLuz.Fonoteca.Models
{
    public class AudioBookDetailResult
    {
        public string Id { get; set; }
        public AuthorModel Author { get; set; }
        public string Comments { get; set; }
        public string Editorial { get; set; }
        public int LengthHours { get; set; }
        public int LengthMins { get; set; }
        public string Title { get; set; }
    }
}
