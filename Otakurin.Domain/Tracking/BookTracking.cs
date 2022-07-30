namespace Otakurin.Domain.Tracking;

#nullable disable

public class BookTracking : MediaTracking
{
    public Guid BookId { get; set; }
    
    public int ChaptersRead { get; set; }
}
