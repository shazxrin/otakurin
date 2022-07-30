namespace Otakurin.Domain.Media;

#nullable disable

public class Book : Entity
{
    public string RemoteId { get; set; }
    
    public string CoverImageURL { get; set; }
    
    public string Title { get; set; }
    
    public string Summary { get; set; }
    
    public string AuthorsString { get; set; }
}
