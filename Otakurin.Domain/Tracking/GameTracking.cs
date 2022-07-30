namespace Otakurin.Domain.Tracking;

#nullable disable

public class GameTracking : MediaTracking
{
    public Guid GameId { get; set; }
    
    public string Platform { get; set; }
    
    public int HoursPlayed { get; set; }
}
