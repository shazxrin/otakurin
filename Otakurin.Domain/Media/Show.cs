namespace Otakurin.Domain.Media;

#nullable disable

public enum ShowType { Movie, Series }

public class Show : Entity
{
    public string RemoteId { get; set; }
    
    public string CoverImageURL { get; set; }
    
    public string Title { get; set; }
    
    public string Summary { get; set; }

    public ShowType ShowType { get; set; }
}