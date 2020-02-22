using System.Collections.Generic;

namespace NuevaLuz.Fonoteca.Models
{
    public class AuthorsResult
    {
        public List<AuthorModel> Authors { get; set; }
        public int Total { get; set; }
    }
}
